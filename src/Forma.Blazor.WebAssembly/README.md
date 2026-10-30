# Forma.Blazor.WebAssembly

The WebAssembly half of Forma's component library: the registration a WebAssembly client makes, which
registers what the components need in the browser.

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
nothing is registered for it yet. `UIWebAssemblyOptions.InputRouting` sets the application's input
policy:

```csharp
builder.Services.AddFormaUIWebAssembly(options => options.InputRouting = new InputRouterOptions { ... });
```

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
