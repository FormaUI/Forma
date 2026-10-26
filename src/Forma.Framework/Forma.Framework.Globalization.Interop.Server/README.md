# Forma.Framework.Globalization.Interop.Server

What the culture state needs from an Interactive Server host: a circuit handler that sets
`CultureInfo.CurrentCulture` and `CultureInfo.CurrentUICulture` from the circuit's culture state before
every inbound activity, each UI event and each call from script into .NET, and starts the document
language of
[`Forma.Framework.Globalization.Interop`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Globalization.Interop/README.md)
as each connection comes up. Code that reads the ambient culture in an event after a switch reads the new
cultures, the page's `lang` and `dir` follow the state's UI culture until the circuit closes, and a switch
in one circuit leaves every other circuit's in place.

The package comes with `formaui-net.Blazor`, and `AddFormaUI()` registers it through
`AddFormaGlobalizationCircuitIntegration()`, which also registers the culture state and the document
language. A host without `AddFormaUI()` makes the same call:

```csharp
builder.Services.AddFormaGlobalizationCircuitIntegration();
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
