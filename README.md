## Description

This projects shows how to handle short-lived OIDC access tokens.
It aligns the lifetime of the ASP.NET session cookie with the one of the OIDC access token.

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

The lifetime of that new authentication cookie is based on the existing one, as you can see [here](https://github.com/aspnet/Security/blob/3a5df89f1c06868cc6dd67997ea492c227a977fc/src/Microsoft.AspNetCore.Authentication.Cookies/CookieAuthenticationHandler.cs#L58).

### What if refreshing the token doesn't work?

Then we'll try again on the next requests, until it succeeds or the authentication cookie expires and we'll ask the user to log in again.