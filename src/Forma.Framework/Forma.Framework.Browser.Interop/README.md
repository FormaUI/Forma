# Forma.Framework.Browser.Interop

The browser capabilities Forma's components reach through JavaScript, each a script with a typed C#
wrapper beside it: behaviors that attach to an element for a lifetime, such as roving focus or a drop
zone; operations that make a single call, such as writing to the clipboard; and the browser side of
components such as `FormaDialog` and `FormaWindow`.

The assembly ships inside `FormaUI.NET-Blazor`. Its wrappers are built on
`Forma.Framework.JavaScript.Interop` and sit in the `Behaviors`, `Operations` and `Components`
namespaces under `Forma.Framework.Browser.Interop`; their scripts are served from
`_content/forma/browser/scripts/`.

## Using a wrapper

A component builds a wrapper from its `Context.JS`, attaches a behavior from `OnAfterRenderAsync`,
and disposes it from `DisposeAsyncCore`. A toolbar that moves focus among its buttons with the arrow
keys, as one Tab stop:

```csharp
using Forma.Framework.Browser.Interop.Behaviors;

public sealed partial class FormaToolbar : FormaElementBase
{
    private Focus _focus = default!;

    protected override void OnInitialized()
        => _focus = new Focus(Context.JS);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await _focus.AttachAsync(RootElement, roving: FocusOrientation.Horizontal);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        await _focus.DisposeAsync();
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
| `Focus` | Decides where Tab and the arrow keys go inside a surface: wrapping among its controls, visiting named regions first, or roving as one Tab stop. |
| `FocusHandOff` | Keeps the focus inside a surface when the element holding it goes away, handing it to the element its owner marks. |
| `PanGesture` | Drags a scroll container's contents with the pointer. |
| `ReorderInteraction` | Moves an item to a new position in its list by dragging or by keyboard, telling its owner through `HandleReorderAsync`. |
| `Scroll` | Tells its owner how far a surface can still scroll and when a sentinel comes into view, and moves a container one step or to its center. |
| `SentinelObserver<TOwner>` | Watches a sentinel element through `Scroll` while it is wanted. |
| `Wheel` | Answers a wheel turned over a surface by moving a track, or by telling its owner the direction through `HandleWheelAsync`. |

`DragGesture.js`, the press, move and release of a drag, is composed by other scripts and has no
wrapper.

## Operations

| Wrapper | What it does |
| --- | --- |
| `Asset` | Loads a module, script, stylesheet or SVG sprite once per page. |
| `CheckBox` | Sets whether a checkbox shows as indeterminate. |
| `Clipboard` | Places text on the clipboard. |
| `Download` | Hands the reader a file to save. |
| `Embedding` | Loads a third-party embed script and has it process the markup rendered since. |
| `Playback` | Mutes a media element, resuming it if it is paused. |
| `Pointer` | Captures a pointer to one element for the rest of its gesture, and releases it. |
| `Print` | Prints the page, or a piece of markup dressed in the page's stylesheets. |
| `Share` | Offers a title and URL to the platform's share surface, and reports whether it has one. |
| `TextEntry` | Reads and clears a text entry's value, and reads or writes its selected range. |

## Components

| Wrapper | What it does |
| --- | --- |
| `Dialog` | Opens and closes a native dialog as a modal. |
| `Frame` | Moves a frame by its drag region, resizes it at its edges, steps it with the arrow keys and holds it within its bounds. |
| `Virtualize` | Tells a virtualized list's owner where the list stands in what scrolls it and how much room its drawn items take, and scrolls it to an offset or an item. |
| `Window` | Shows a window in the top layer, raises it in front of the others, and returns focus to where it was inside it. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
