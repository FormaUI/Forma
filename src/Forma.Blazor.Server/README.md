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

## WebAssembly components

A server that renders WebAssembly components, through `AddInteractiveWebAssemblyComponents()`, chooses how
its pages treat a client's culture with `UIServerOptions.CultureArrangement`, and the client declares the
same on `AddFormaUIWebAssembly`. Under `Coordinated`, the default, every runtime on the page follows the
page's coordinator, and a prerendered client starts in the pair the server rendered it in: the
registration turns on `Components:UseCultureFromServer`, the framework's handoff of the server's culture to
the client, wherever the host's configuration names no value, and the host fails to start where the
configuration sets it to `false`. Under `Independent` the client's culture is its own, and the setting is
the host's:

```csharp
builder.AddFormaUIServer(options => options.CultureArrangement = CultureArrangement.Independent);
```

## The document head

`FormaPageHead`, in `Forma.Blazor.Components.Page`, renders the head a host's `App.razor` needs: the
character set, viewport, title, base address and icon, the culture marker and switch policy the page's
culture coordinator starts from, Forma's stylesheets in their cascade order, and the framework's
`ResourcePreloader`, `ImportMap` and `HeadOutlet`. A coordinated prerendered page needs it: without the
marker the coordinator can't start, and no circuit registers with it. Its base address is the application's
root, or `BaseHref` where the host gives one; where culture routing is registered, it writes the base routing
gives the page's culture and publishes the address policy, and refuses a `BaseHref`. The
[library's README](https://github.com/FormaUI/Forma/blob/main/README.md) shows it in place.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
