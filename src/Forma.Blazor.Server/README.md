# Forma.Blazor.Server

The server half of Forma's component library: what a host that renders on the server needs beside the
components, the document head `FormaPageHead` and the registration that adds the interactive server
circuit's integrations.

## Install

```pwsh
dotnet add package formaui-net.Blazor.Server
```

It brings `formaui-net.Blazor` and the Framework packages the components build on.

## Register

```csharp
using Forma.Blazor;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddFormaUIServer();
```

`AddFormaUIServer()` registers what `AddFormaUI()` registers for every render mode, and beside it the
circuit's integrations: the culture applied before every event and kept on the document, and the
browser's observations measured again when a circuit reconnects. Each is registered only where nothing
is registered for it yet. `UIServerOptions.InputRouting` sets the application's input policy:

```csharp
builder.AddFormaUIServer(options => options.InputRouting = new InputRouterOptions { ... });
```

## The document head

`FormaPageHead`, in `Forma.Blazor.Components.Page`, renders the head a host's `App.razor` needs: the
character set, viewport, title, base address and icon, Forma's stylesheets in their cascade order, and
the framework's `ResourcePreloader`, `ImportMap` and `HeadOutlet`. The
[library's README](https://github.com/FormaUI/Forma/blob/main/README.md) shows it in place.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
