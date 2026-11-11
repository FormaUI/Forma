# Forma.Framework.Culture.Blazor.Server

What the culture state needs from an Interactive Server host: a circuit handler that sets
`CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` from the circuit's culture state before
every inbound activity, each UI event and each call from script into .NET, and registers the circuit's
state with the page's culture coordinator of
[`Forma.Framework.Culture.Browser.Interop`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Culture.Browser.Interop/README.md)
as each connection comes up, before the circuit renders. The circuit's `ICultureSwitch` requests through
the coordinator, so a switch reaches every circuit on the page and the document, and code that reads the
ambient culture in an event after a switch reads the new cultures. While the connection is down a switch
fails at once with `InvalidOperationException`, as it does in a static render.

The package comes with `formaui-net.Blazor.Server`, and `AddFormaServerIntegration()` registers it through
`AddFormaCultureServerIntegration()`, which also registers the culture state, its registration with the coordinator,
the coordinated switch and the switch's policy. A host without `AddFormaServerIntegration()` makes the same call
beside `AddFormaUIServices()`, and names the policy where a switch writes the culture cookie:

```csharp
builder.Services.AddFormaCultureServerIntegration(new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie });
builder.Services.AddFormaUIServices();
```

## The request's cultures

An application that registers an `ICultureProvider` and chooses each request's cultures through ASP.NET
Core's request localization, which takes them from lists fixed at startup, adds `UseFormaRequestCulture()`
after it. The step runs the request in the provider's cultures for the names request localization chose,
setting `CultureInfo.CurrentCulture`, `CultureInfo.CurrentUICulture` and the request culture feature, so
code reading them in a prerender reads the application's patterns, as a circuit the request starts does.
Without a provider, it leaves the request in its own cultures; culture routing applies the provider
itself.

```csharp
using Forma.Framework.Culture.Blazor.Server;

app.UseRequestLocalization();
app.UseFormaRequestCulture();
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
