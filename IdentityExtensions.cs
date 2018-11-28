using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;

namespace DynamicsExtensions
{
    public static class IdentityExtensions
    {
        /// <summary>
        /// Returns true if the expected tokens exist on the ClaimsPrincipal AND the access token has not expired
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static bool HasValidDynamicsTokens(this ClaimsPrincipal user)
        {
            var upn = user.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn");
            var accessToken = user.FindFirstValue(DynamicsClaims.AccessToken);
            var accessTokenExpiry = user.FindFirstValue(DynamicsClaims.AccessTokenExpiresOn);

            var validPrincipal = !string.IsNullOrWhiteSpace(upn) && !string.IsNullOrWhiteSpace(accessToken) && !string.IsNullOrWhiteSpace(accessTokenExpiry);

            if (!validPrincipal)
            {
                return false;
            }

            var expiresOn = DateTimeOffset.FromUnixTimeSeconds(long.Parse(accessTokenExpiry));

            if (DateTimeOffset.UtcNow >= expiresOn.AddMinutes(5)) //if it has expired or will expire within 5 mins, need to auth
            {
                return false;
            }

            return true;
        }

        public static async Task<AuthenticationResult> AcquireDynamicsTokenAsync(this HttpContext httpContext, string dynamicsOrganisation, string accessToken, string upn, string oid)
        {
            var authContext = new AuthenticationContext(AzureAdOptions.Settings.Authority);

            var credential = new ClientCredential(AzureAdOptions.Settings.ClientId, AzureAdOptions.Settings.ClientSecret);

            var dynamicsCrmToken = await authContext.
                AcquireTokenAsync($"https://{dynamicsOrganisation}.crm.dynamics.com/",
                credential,
                new UserAssertion(accessToken));

            await httpContext.UpdatePrincipal(dynamicsCrmToken.AccessToken, dynamicsCrmToken.ExpiresOn);

            return dynamicsCrmToken;
        }

        public static async Task UpdatePrincipal(this HttpContext httpContext, string dynamicsAccessToken, DateTimeOffset dynamicsAccessTokenExpiry)
        {
            var claimsIdentity = (ClaimsIdentity)httpContext.User.Identity;
            var clonedIdentity = claimsIdentity.Clone();
            clonedIdentity.AddClaim(new Claim(DynamicsClaims.AccessToken, dynamicsAccessToken));
            clonedIdentity.AddClaim(new Claim(DynamicsClaims.AccessTokenExpiresOn, dynamicsAccessTokenExpiry.ToUnixTimeSeconds().ToString()));

            var principal = new ClaimsPrincipal(clonedIdentity);

            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            await httpContext.SignInAsync(
                    scheme: CookieAuthenticationDefaults.AuthenticationScheme,
                    principal: principal, 
                    properties: new AuthenticationProperties {
                        IsPersistent = true,
                        AllowRefresh = true,
                        IssuedUtc = DateTime.UtcNow
                    });
        }
    }

    public static class DynamicsClaims
    {
        public const string AccessToken = "d365_access_token";
        public const string AccessTokenExpiresOn = "d365_access_token_expires_on";
    }
}
