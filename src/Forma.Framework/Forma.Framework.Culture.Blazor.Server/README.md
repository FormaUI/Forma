# Forma.Framework.Culture.Blazor.Server

What the culture state needs from an Interactive Server host: a circuit handler that sets
`CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` from the circuit's culture state before
every inbound activity, each UI event and each call from script into .NET, and starts the document
culture synchronizer of
[`Forma.Framework.Culture.Browser.Interop`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Browser.Interop/README.md)
as each connection comes up. Code that reads the ambient culture in an event after a switch reads the new
cultures, the page's `lang` and `dir` follow the state's UI culture until the circuit closes, and a switch
in one circuit leaves every other circuit's in place.

The package comes with `formaui-net.Blazor`, and `AddFormaUI()` registers it through
`AddFormaCultureCircuitIntegration()`, which also registers the culture state and the document
culture synchronizer. A host without `AddFormaUI()` makes the same call:

```csharp
builder.Services.AddFormaCultureCircuitIntegration();
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
