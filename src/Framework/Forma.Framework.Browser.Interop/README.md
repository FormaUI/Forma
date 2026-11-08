# Forma.Framework.Browser.Interop

The browser capabilities Forma's components reach through JavaScript. The DOM watcher and engine give
an owner typed objects for what it observes of its elements and what it does to them, with one
lifetime and one notification path for all of them. Beside them, scripts with typed C# wrappers:
behaviors that attach to an element for a lifetime, such as a drop zone or an anchored popover; operations
that make a single call, such as writing to the clipboard; and the browser side of components such as
`FormaDialog` and `FormaWindow`.

The package comes with `formaui-net.Blazor`. It is built on `Forma.Framework.JavaScript.Interop`.
The watcher and engine sit at the root of the `Forma.Framework.Browser.Interop` namespace with the
registrations they make, and each capability area has a namespace beneath it, such as `Scrolling`.
The wrappers sit in `Behaviors`, `Operations` and `Components`. The scripts are served from
`_content/forma/browser/scripts/`.

## The DOM watcher and engine

A component reaches its watcher and engine as `Context.DomWatcher` and `Context.DomEngine`; its
context makes them on first use and releases everything they hold when the component is disposed, in
one call to the browser. The engine acts through the watcher it borrows, so an element got from the
engine both observes and acts, and one got from the watcher only observes.

An observation hands each state the browser measures to its handler, the first one included, and
an interaction keeps running in the browser. Each is created once and brought in step with the
component after every render; a request equal to the one the browser holds makes no call, and
overlapping calls join one settle. A freeze pane that reports how far its content can move and
steps it with its arrows:

```csharp
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;

private DomObservation<ScrollWatchOptions, ScrollState>? _scrollState;
private DomInteraction<ScrollInteractionOptions>? _scrolling;

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var scroll = Context.DomEngine.GetElement(_viewport).Scroll;
        var watching = scroll.WatchAsync(HandleScrollStateAsync, WatchOptions);
        var attaching = scroll.AttachAsync(InteractionOptions, WantsInteraction);

        _scrollState = await watching;
        _scrolling = await attaching;
    }

    if (_scrollState is { } scrollState && _scrolling is { } scrolling)
    {
        await Task.WhenAll(
            scrollState.SyncAsync(_viewport, WatchOptions),
            scrolling.SyncAsync(_viewport, InteractionOptions, WantsInteraction));
    }
}

// The state arrives off the renderer, so the render is marshalled back onto it.
private Task HandleScrollStateAsync(ScrollState state)
{
    _state = state;

    return InvokeAsync(StateHasChanged);
}
```

A one-off action is a method on the element: `Scroll.StepAsync` moves it a step and reports whether
it started moving.

| Capability | Watcher | Engine |
| --- | --- | --- |
| `Focus` | `WatchAsync`: whether the reader is within the element, and which of its items holds the focus, named by the attribute its items carry | `AttachAsync`: where the focus goes inside the element, from Tab wrapping, regions Tab visits first, one roving Tab stop and the element taking the focus when the one holding it goes away, each selectable alone |
| `Intersection` | `WatchAsync`: whether the element is in view of its root, or of the viewport, grown by a margin | |
| `Scroll` | `WatchAsync`: whether the content can still move each way, how far its frozen parts reach, and its visible extent where asked for | `AttachAsync`: a wheel route moving the content along its inline axis, and buttons stepping it, both run in the browser; `StepAsync`: one step to the next stop |
| `Text` | `WatchSelectionAsync`: the range a text field's selection spans, as the reader moves it | |

A text field's edits are a `DomTextEdits`, made over the component's engine as it initializes, from the
text it first shows: .NET applies the edits the field sends, in revision order, as each is typed or as
the field commits, and writes a text of its own through the field. The field renders `GetShownText`,
which keeps the text it was last rendered or written with while the text its component shows is the
one .NET holds, so a render never writes back over typing, and syncs the edits with that text after each
render, which attaches them and writes a text of .NET's own into the field:

```razor
<input @ref="RootElement" value="@ShownText" />
```

```csharp
private DomTextEdits _edits = default!;

private string? ShownText
    => _edits.GetShownText(CurrentValueAsString);

protected override void OnInitialized()
    => _edits = new DomTextEdits(Context.DomEngine, CurrentValueAsString, HandleTextChangedAsync);

protected override Task OnAfterRenderAsync(bool firstRender)
    => _edits.SyncAsync(RootElement, CurrentValueAsString, new TextEditOptions { SendsOnInput = true });
```

`TextEditOptions` also names the most the field accepts, and where it shows a running count of its
characters, which the browser keeps as the reader types. An element whose script owns its text, as a rich text
editor's canvas, follows the same edits: the script provides the text they read and write through
`TextSources.provide`, and commits it at its own pace. A form's submission waits in the browser while an
edit of a field in it is still on its way, and is made again, by the same submitter, once the edit has
reached .NET. The field takes no Blazor event, since Blazor
sends the field's whole text with every event a .NET handler takes on it or on an element the event
reaches, and neither does an element around a text field: a surface follows the focus within it
through `Focus.WatchAsync`, and a field's selection through `Text.WatchSelectionAsync`.

A host registers the bridge the watchers and engines of one circuit share their imports through with
`AddFormaBrowserServices`, which `AddFormaUI` calls. On Interactive Server, `AddFormaUIServer` also
registers `Forma.Framework.Browser.Blazor.Server`, which refreshes a circuit's observations, and has
each text field whose edits could have been lost send its whole text, once its connection is restored.
A watcher made with no bridge imports through one of its own, and its observations recover through
`RefreshAsync`.

## Using a wrapper

A component builds a wrapper from its `Context.JS`, attaches a behavior from `OnAfterRenderAsync`,
and disposes it from `DisposeAsyncCore`. A text box that marks a value the browser filled in:

```csharp
using Forma.Framework.Browser.Interop.Behaviors;

public sealed partial class FormaTextBox : FormaInputBase<string?>
{
    private AutofillTracker _autofill = default!;

    protected override void OnInitialized()
        => _autofill = new AutofillTracker(Context.JS);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _autofill.AttachAsync(RootElement);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _autofill.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
```

An operation is called whenever it is needed:

```csharp
using Forma.Framework.Browser.Interop.Operations;

await using var clipboard = new Clipboard(Context.JS);
await clipboard.WriteAsync(link);
```

A wrapper's calls report whether they ran, so a component carries on when its circuit has gone.

## Behaviors

| Wrapper | What it does |
| --- | --- |
| `AnchoredPopover` | Keeps a popover positioned against its anchor while it is open. |
| `AutofillTracker` | Marks a field the browser filled in without the user typing. |
| `DropZone` | A drag-and-drop target, telling its owner through `HandleDropAsync` which of its targets took the drop. |
| `FileDropZone` | Hands dropped files to the file input inside an element. |
| `FilePreview` | Renders thumbnails of a file input's selected files into a container's images. |
| `PanGesture` | Drags a scroll container's contents with the pointer. |
| `ReorderInteraction` | Moves an item to a new position in its list by dragging or by keyboard, telling its owner through `HandleReorderAsync`. |
| `Scroll` | Moves a container to its center. |
| `Wheel` | Answers a wheel turned over a surface by telling its owner the direction through `HandleWheelAsync`. |

`DragGesture.js`, the press, move and release of a drag, is composed by other scripts and has no
wrapper.

## Operations

| Wrapper | What it does |
| --- | --- |
| `Asset` | Loads a module, stylesheet or SVG sprite once per page. |
| `CheckBox` | Sets whether a checkbox shows as indeterminate. |
| `Clipboard` | Places text on the clipboard. |
| `DocumentOrder` | Reads the order elements stand in within the document. |
| `Download` | Hands the reader a file to save. |
| `Embedding` | Loads a third-party embed script and has it process the markup rendered since. |
| `Playback` | Mutes a media element, resuming it if it is paused. |
| `Print` | Prints the page, or a piece of markup dressed in the page's stylesheets. |
| `Share` | Offers a title and URL to the platform's share surface, and reports whether it has one. |
| `TextEntry` | Reads or writes a text entry's selected range. |

`INativeOperations` is a host's own way to save a file and share an address, for a web view that does
neither. `Download`, `Print` and `Share` take it where the host registers one, and take the browser's path
where the host hasn't registered one or declines.

`Pointer.js`, a pointer captured to one element for the rest of its gesture, is composed by
`DragGesture.js` alone, without a wrapper.

## Components

| Wrapper | What it does |
| --- | --- |
| `Dialog` | Opens and closes a native dialog as a modal. |
| `Frame` | Moves a frame by its drag region, resizes it at its edges, steps it with the arrow keys and holds it within its bounds. |
| `Splitter` | Drags a splitter's bar along its axis, telling its owner the travel once a frame and the drag's end. |
| `Virtualize` | Tells a virtualized list's owner where the list stands in what scrolls it and how much room its drawn items take, and scrolls it to an offset or an item. |
| `Window` | Shows a window in the top layer, raises it in front of the others, returns focus to where it was inside it, and tells its owner of each press inside it. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
