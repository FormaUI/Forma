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
circuit's integrations: the culture applied before every event and registered with the page's culture
coordinator, whose switch reaches every circuit on the page and the document, and the browser's
observations measured again when a circuit reconnects. Each is registered only where nothing is
registered for it yet. `UIServerOptions.InputRouting` sets the application's input policy, and
`UIServerOptions.CultureSwitchPolicy` how a switch persists, in the states alone by default, or also in
request localization's culture cookie:

```csharp
builder.AddFormaUIServer(options =>
{
    options.InputRouting = new InputRouterOptions { ... };
    options.CultureSwitchPolicy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie };
});
```

## The document head

`FormaPageHead`, in `Forma.Blazor.Components.Page`, renders the head a host's `App.razor` needs: the
character set, viewport, title, base address and icon, the culture marker and switch policy the page's
culture coordinator starts from, Forma's stylesheets in their cascade order, and the framework's
`ResourcePreloader`, `ImportMap` and `HeadOutlet`. A coordinated prerendered page needs it: without the
marker the coordinator can't start, and no circuit registers with it. The
[library's README](https://github.com/FormaUI/Forma/blob/main/README.md) shows it in place.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
