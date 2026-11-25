# Forma.Blazor.Server

The server half of Forma's component library: what a host that renders on the server needs beside the
components, the document head `FormaPageHead` and the registration that adds the interactive server
circuit's integrations.

## Install

```pwsh
dotnet add package formaui-net.Blazor.Server --prerelease
```

It brings `formaui-net.Blazor` and the Framework packages the components build on.

## Register

```csharp
using Forma.Blazor;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.AddFormaServerIntegration();
```

`AddFormaServerIntegration()` registers what `AddFormaUIServices()` registers for every render mode, and beside it the
circuit's integrations: the culture applied before every event and registered with the page's culture
coordinator, whose switch reaches every circuit on the page and the document, and the browser's
observations measured again when a circuit reconnects. Each is registered only where nothing is
registered for it yet. `ServerIntegrationOptions.InputRouting` sets the application's input policy, and
`ServerIntegrationOptions.CultureSwitchPolicy` how a switch persists, in the states alone by default, or also in
request localization's culture cookie:

```csharp
builder.AddFormaServerIntegration(options =>
{
    options.InputRouting = new InputRouterOptions { ... };
    options.CultureSwitchPolicy = new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie };
});
```

## Limits

The hub's and the circuit's limits are the host's, and Forma runs on their defaults:

- **SignalR's `MaximumReceiveMessageSize`**, 32 KB by default, needs no raising for Forma. A value Forma sends
  inline measures at most 16 KB as the hub carries it, the rest of the message left to the call around it, and a
  larger value, and every file a reader uploads, travels as a stream Blazor sends in pieces under the limit. A host
  keeps the default, or more where its own code needs it, which lets every client send messages that large.
- **`CircuitOptions`**, and the hub's other options, need nothing changed: Forma brings a reconnecting circuit's
  text fields and observations back in step itself.
- **Blazor's own `InputFile`**, which `FormaFilePicker` draws, describes every file a reader chooses in one message,
  each file's name, size and type, so a selection of very many files, or of files with very long names, can pass
  `MaximumReceiveMessageSize`, which closes the circuit's connection, whatever the picker's `MaxFiles`, which holds
  once the list has arrived. A host expecting such selections raises the limit.
- **A Host header no address can be built from**, such as an empty one or one with a port past 65535, which Kestrel
  lets through, makes the framework throw as it builds the page's address, answering the request with a 500. A host
  refuses such a request itself, ahead of its pipeline, with a 400, as the Showcase's
  [`HostAddressMiddleware`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Showcase/HostAddressMiddleware.cs)
  does.

Forma holds what the browser sends to limits of its own: `JSTransferOptions.MaximumLength`, 16 MiB, for a value
with no maximum of its own, `FileUploaderOptions.MaxFileSize`, 10 MiB, for an uploaded file, and a text field's
`MaxLength`. The [library's README](https://github.com/FormaUI/Forma/blob/main/README.md#limits) sets each out.

## WebAssembly components

A server that renders WebAssembly components, through `AddInteractiveWebAssemblyComponents()`, chooses how
its pages treat a client's culture with `ServerIntegrationOptions.CultureArrangement`, and the client declares the
same on `AddFormaWebAssemblyIntegration`. Under `Coordinated`, the default, every runtime on the page follows the
page's coordinator, and a prerendered client starts in the pair the server rendered it in: the
registration turns on `Components:UseCultureFromServer`, the framework's handoff of the server's culture to
the client, wherever the host's configuration names no value, and the host fails to start where the
configuration sets it to `false`. Under `Independent` the client's culture is its own, and the setting is
the host's:

```csharp
builder.AddFormaServerIntegration(options => options.CultureArrangement = CultureArrangement.Independent);
```

## The document head

`FormaPageHead`, in `Forma.Blazor.Components.Page`, renders the head a host's `App.razor` needs: the
character set, viewport, title, base address and icon, the culture marker and switch policy the page's
culture coordinator starts from, Forma's stylesheets in their cascade order, and the framework's
`ResourcePreloader`, `ImportMap` and `HeadOutlet`.

The document has one title: the one a page sets with `PageTitle`, or `Title` where the page sets none. The
`HeadOutlet` renders in `PageRenderMode`, the render mode the host gives `Routes`, so a page's title follows the
page wherever it renders.

A coordinated prerendered page needs `FormaPageHead`: without the marker the coordinator can't start, and no
circuit registers with it. Its base address is the application's root, or `BaseHref` where the host gives one;
where culture routing is registered, it writes the base routing gives the page's culture and publishes the address
policy, and refuses a `BaseHref`. The [library's README](https://github.com/FormaUI/Forma/blob/main/README.md)
shows it in place.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
