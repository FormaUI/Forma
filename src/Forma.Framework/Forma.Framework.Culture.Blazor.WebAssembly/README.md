# Forma.Framework.Culture.Blazor.WebAssembly

What the culture state needs from a WebAssembly client: the runtime's state registered with the page's
culture coordinator of
[`Forma.Framework.Culture.Browser.Interop`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Browser.Interop/README.md)
as the runtime starts, before it renders, and each pair the state applies set as the process's default
cultures too, so an event, a timer or any work the runtime starts after a switch runs in the new pair. The
runtime's `ICultureSwitch` requests through the coordinator, so a switch reaches every runtime on the page
and the document. A runtime is one user scope in one process, so its culture services are one per runtime.

A standalone app's runtime starts the page's coordinator itself, from the cultures it starts in and the
switch's policy; a page a server rendered starts from the marker `FormaPageHead` publishes.

## Use it

The package comes with `formaui-net.Blazor.WebAssembly`, and `AddFormaUIWebAssembly()` registers it
through `AddFormaCultureWebAssembly()`. A client whose host declared its culture independent registers the
local switch instead, which changes the runtime alone:

```csharp
builder.Services.AddFormaCultureWebAssembly(CultureArrangement.Independent);
builder.Services.AddFormaUI();
```

A client that registers its own culture state registers it once per runtime too, as a singleton.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
