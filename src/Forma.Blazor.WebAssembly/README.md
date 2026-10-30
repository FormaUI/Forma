# Forma.Blazor.WebAssembly

The WebAssembly half of Forma's component library: the registration a WebAssembly client makes, which
registers what the components need in the browser and the culture a WebAssembly runtime keeps.

## Install

```pwsh
dotnet add package formaui-net.Blazor.WebAssembly
```

It brings `formaui-net.Blazor` and the Framework packages the components build on.

## Register

In the client's `Program.cs`:

```csharp
using Forma.Blazor;

builder.Services.AddFormaUIWebAssembly();
```

`AddFormaUIWebAssembly()` registers what `AddFormaUI()` registers for every render mode, each only where
nothing is registered for it yet, and the culture's WebAssembly half,
[`Forma.Framework.Culture.Blazor.WebAssembly`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Culture.Blazor.WebAssembly/README.md):
the runtime registers its culture state with the page's culture coordinator as it starts, before it
renders, so it follows every switch on the page, and each pair it applies becomes the process's default
cultures too, so an event or a timer after a switch runs in the new pair.

`UIWebAssemblyOptions.InputRouting` sets the application's input policy, and
`UIWebAssemblyOptions.CultureArrangement` how the page treats the client's culture: `Coordinated`, the
default, follows the page's coordinator, and `Independent` keeps the client's culture its own, switched
alone, which the server declares too:

```csharp
builder.Services.AddFormaUIWebAssembly(options =>
{
    options.InputRouting = new InputRouterOptions { ... };
    options.CultureArrangement = CultureArrangement.Independent;
});
```

## A standalone app

A standalone WebAssembly app has no server to publish the page's culture, so its runtime starts the page's
coordinator from the cultures it starts in, which the app sets before it runs, and from
`UIWebAssemblyOptions.CultureSwitchPolicy`, how a switch persists: in the state alone by default, or in a
cookie. Its document's language and direction follow each switch. A standalone app switches without culture
addresses, since nothing gives its document a base in each culture.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
