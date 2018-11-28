# Simple helpers for acquiring auth tokens
These identity extensions can be used to simplify management of Dynamics 365 tokens when using ASP.NET Core with Azure AD.

Copy the code to your project, then when you have acquired an Azure AD token and want to exchange it for a Dynamics 365 token:

```csharp
var dynamicsOrg = "myorg"; //this is the first component of your dynamics URI, ie: https://myorg.crm.dynamics.com/
var accessToken = await HttpContext.GetTokenAsync("access_token");
var upn = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn").Value;
var oid = User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

var dynamicsCrmToken = await HttpContext.AcquireDynamicsTokenAsync(accessToken, upn, oid);
```

To check if you already have tokens, you can do this:

```csharp
if (User.HasValidDynamicsTokens())
{
  //do something
}
else
{
  //retrieve them, as shown above
}
```

If you get a token through some other mechanism, such as client side, you can do this:

```csharp
await HttpContext.UpdatePrincipal(accessToken, DateTimeOffset.FromUnixTimeSeconds(long.Parse(expiry)));
```
