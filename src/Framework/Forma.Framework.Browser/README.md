# Forma.Framework.Browser

The abstractions of Forma's browser layer: the observations a component holds of an element and the
states they report, the options of what the browser does to an element, the registrations an owner's
session holds, and `INativeOperations`, a host's own way to save a file and share an address where its
web view can't. `Forma.Framework.Browser.Interop` implements them over the browser's scripts.

The package comes with `formaui-net.Blazor`. Every type is in the `Forma.Framework.Browser` namespace.

## Observing an element

An element's observations come from the component's watcher, as
`Context.DomWatcher.GetElement(element)`, an `IDomElementObservation`. Each one watches a state and
hands what the browser measures to a handler, and is brought in step with the component after every
render:

```csharp
using Forma.Framework.Browser;

private IDomObservation<ScrollWatchOptions, ScrollState>? _scrollState;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        _scrollState = await Context.DomWatcher.GetElement(_viewport).Scroll.WatchAsync(HandleScrollStateAsync, WatchOptions);
    }

    if (_scrollState is { } scrollState)
    {
        await scrollState.SyncAsync(_viewport, WatchOptions);
    }
}
```

## Saving and sharing natively

A host whose web view neither saves a file nor shares an address registers an `INativeOperations`.
`CanSave` and `CanShare` tell whether it does each, and `TrySaveAsync` and `TryShareAsync` answer
whether it took the file or the address. Forma's downloads, printing and sharing take it where one is
registered that does the work, and go to the browser's own path otherwise. `Forma.Framework.Browser.Maui`
registers one for MAUI's web views on Android, iOS and Mac Catalyst.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
