[![Build status](https://dev.azure.com/mderriey/github-pipelines/_apis/build/status/aspnet-core-token-renewal)](https://dev.azure.com/mderriey/github-pipelines/_build/latest?definitionId=18)
[![Dependabot Status](https://api.dependabot.com/badges/status?host=github&repo=mderriey/aspnet-core-token-renewal)](https://dependabot.com)

## Description

This projects shows how to handle short-lived OIDC access tokens.
It aligns the lifetime of the ASP.NET session cookie with the one of the OIDC access token.

_Note: This code has been upgraded to work with ASP.NET Core 2.0; if you're after the original version based on ASP.NET Core 1.x, you can check the `asp-net-core-1` branch_

### How does it work?

When the OIDC middleware redeems the authorisation code for an access token and a refresh token, we add these tokens to the `ClaimsIdentity` as ClaimsIdentityso they end up in the encrypted authentication cookie.
We can imagine that on subsequent requests, these tokens would be used to make call to protected APIs.

The OIDC middleware has a configuration property, `UseTokenLifetime`, which allows to align the lifetime of the cookie with the one of the identity token.
In our case, we want to align it with the lifetime of the access token, so we have to make a couple of changes.

First, we edit the `AuthenticationProperties` so that a fixed-lifetime cookie is issued, as opposed to one which would live for the time of the session - that is, until the browser is closed.
We also modify the expiration time to make it consistent with the access token.

The OIDC middleware then instructs the cookie middleware to sign in the identity with these properties, which results in an authentication cookie being created.

### What happens when the cookie and the access token are about to expire?

The cookie middleware fires an event every time an authentication cookie has been succesfully validated, that is for every authenticated request.
This gives us a chance to inspect the remaining lifetime of the cookie - and the access token since they share the same one.

If we find we have to renew the access token - in our case if we're more than halfway through the lifetime of the cookie - we use the refresh token to get a new set of tokens.
It's then a matter of removing the old tokens from the claims, add the new ones, and instruct the cookie middleware to issue a new cookie.

That last part is much easier in ASP.NET Core than it was with OWIN. I never got this to work with OWIN, to be honest.
The context that is passed to the `ValidateIdentity` event contains a new property, `ShouldRenew`, which we can set to `true` for the middleware to automatically issue a new token.

The lifetime of that new authentication cookie is based on the existing one, as you can see [here](https://github.com/aspnet/Security/blob/e98a0d243a7a5d8076ab85c3438739118cdd53ff/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationHandler.cs#L88-L102).

### What if refreshing the token doesn't work?

Then we'll try again on the next requests, until it succeeds or the authentication cookie expires and we'll ask the user to log in again.

### What should I expect, then?

The `About` page needs the user to be authenticated, so the OIDC middle ware will redirect you to IdentityServer login page.
You can login with the user `bob` and the password `secret`.

We've configured in IdentityServer that this client, `mvc`, has access tokens valid for a minute.
When logging in, you can then expect to see an initial authentication cookie which lifetime will be of 1 minute.

Subsequent requests in the following 30 seconds won't try to renew tokens or issue a new authentication cookie.
Requests within 30 to 60 seconds will try renewing the tokens, and if successful, issue a new authentication cookie valid for another 1 minute.

To avoid having valid refresh tokens in the wild, their lifetime is also aligned with the access token, so they become invalid when:

 - it's used to get a new set of tokens, or
 - the session ends because the cookie expires since the cookie, the access token and the refresh token share the same lifetime

### Anything else?

In this case, it's possible that a user closes their browser, navigates back to the application and is still logged in.
Since the authentication cookie is flagged to be access by HTTP only, there's no way we can kill it with JS when the user navigates away.
Keeping short-lived cookies might be enough.

If the cookie **has** to be killed when the user closes their browser, then we could issue an authentication cookie valid for the time of the session.
The expiration logic would have to be done against the access token and not the cookie.
It would still be renewed to accomodate new tokens when they're renewed.
