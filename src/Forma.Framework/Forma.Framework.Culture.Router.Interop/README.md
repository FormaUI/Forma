# Forma.Framework.Culture.Router.Interop

The culture cookie written from the browser, for a culture changed in place where no request carries
the change to the server. Culture routing uses it: under `CultureSwitchMode.InPlace`, a culture route page
that changes the culture state writes the `forma-culture-route` cookie through it, so the next request
whose address names no culture, such as a circuit's own connection, takes the culture last chosen.

The package comes with
[`formaui-net.Framework.Culture.Router.AspNetCore`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Router.AspNetCore/README.md),
whose switcher constructs it, so it has no services to register. Its script is served from
`_content/forma/culture-router/scripts/`.

## Writing the cookie

`CultureCookie` takes an `IJSEnvironment`, the JS runtime with its owner's logger and identity, and
writes a cookie holding a culture's token at a path for a lifetime:

```csharp
await using var cultureCookie = new CultureCookie(new JSEnvironment(js, logger, Guid.CreateVersion7()));

var isWritten = await cultureCookie.WriteAsync("forma-culture-route", "he", "/", TimeSpan.FromDays(365), isSecure: false);
```

The cookie is written `SameSite=Lax`, and `Secure` where `isSecure` is set. `WriteAsync` reports
whether the browser wrote it; a circuit that dropped or a script that threw reports `false`, logged at
`Debug` under the owner's logger.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
