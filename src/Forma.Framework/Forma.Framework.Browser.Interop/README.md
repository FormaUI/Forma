# Forma.Framework.Browser.Interop

The behaviors and operations any component may attach or call, and the browser side of the
components that belong to no feature, each script beside the C# wrapper that reaches it. It is not
a package; it ships inside `Forma.Blazor`, and a host never references it. It is built on
`Forma.Framework.JavaScript.Interop`, whose bases every wrapper derives from and whose scripts its own
scripts import.

The assembly serves many concepts, so it sorts by kind: a script lives under
`wwwroot/scripts/behaviors/`, `operations/` or `components/`, and its wrapper in the matching
`Behaviors/`, `Operations/` or `Components/` folder and namespace. Its static assets are served from
`_content/forma/browser`, so a wrapper imports its module as
`./_content/forma/browser/scripts/behaviors/Focus.js`.

## Behaviors

A behavior attaches to an element for a lifetime and is torn down by disposing the handle. Two
have no wrapper because only other scripts attach them.

| Script | Wrapper | What it does |
| --- | --- | --- |
| `AnchoredPopover.js` | `AnchoredPopover` | Keeps a popover positioned against its anchor while it is open, returning focus to its trigger on a close that asks for it. |
| `AutofillTracker.js` | `AutofillTracker` | Marks a field the browser filled in without the user typing. |
| `Breakpoint.js` | `Breakpoint` | Tells the owner, through `HandleBreakpointAsync`, which layout tier a stylesheet has put the target in. |
| `DragGesture.js` | — | The press-move-release of a drag, with pointer capture and edge scrolling, reported to the script that composes it; a drag under way is an active registration on the input router, which cancels it on Escape, and the press is a route on it where the script names an action. |
| `DropZone.js` | `DropZone` | A drag-and-drop target whose drop is a route on the input router, marked while a drag is over it, telling the owner through `HandleDropAsync` which of its targets took the drop. |
| `FileDropZone.js` | `FileDropZone` | Hands dropped files to the file input inside an element. |
| `FilePreview.js` | `FilePreview` | Thumbnails rendered from the browser's own `File` objects into a container's images. |
| `Focus.js` | `Focus` | Decides where Tab and the arrow keys go inside a surface: wrapping among its controls, visiting named regions first, or roving as one Tab stop. |
| `HeightReservation.js` | `HeightReservation` | Holds a container at the tallest height its content has needed. |
| `ImplicitSubmitGuard.js` | `ImplicitSubmitGuard` | Stops Enter from submitting the form around a control. |
| `PanGesture.js` | `PanGesture` | Drags a scroll container's contents with the mouse while a class marks it eligible. |
| `ReorderInteraction.js` | `ReorderInteraction` | Drags or keys an item into a new position in its list, telling the owner through `HandleReorderAsync`. |
| `Scroll.js` | `Scroll` | Tells the owner how far a surface can still scroll through `HandleOverflowAsync` and when a sentinel comes into view through `HandleReachedAsync`; exports `toCenter` and `step` beside the attachment. |
| `SelectionFollower.js` | `SelectionFollower` | Keeps an element positioned beside the text selection inside its owner, holding focus while it shows and standing on the input router while focused so Escape returns focus to the owner. |
| `SelectionInteraction.js` | `SelectionInteraction` | Click and keyboard selection across a list of keyed items, told to the owner through `HandleSelectAsync` and `HandleRangeSelectAsync`. |
| `Wheel.js` | `Wheel` | Answers a wheel turned over a surface: a track moves by it, or the owner is told the direction through `HandleWheelAsync`. |

The options a behavior's `AttachAsync` takes and the reports its owner receives sit beside the
wrappers: `FocusOrientation`, `PopoverAlignment`, `ScrollAxis`, `ScrollBehaviors`,
`ScrollBoundary`, `ScrollBoundaryReport`, `ScrollSentinel`, `ScrollStops`, `ScrollWheel` and
`WheelModifier`. `SentinelObserver<TOwner>` is a C# helper over `Scroll` that attaches or detaches a
sentinel watch to match whether it is currently wanted.

## Operations

An operation is a stateless call against an element or a value; its wrapper holds a `JSModule`
and calls through `TryInvoke*`, so a dropped circuit leaves the call absent rather than throwing.

| Script | Wrapper | What it does |
| --- | --- | --- |
| `Asset.js` | `Asset` | Loads a module, script, stylesheet or SVG sprite once per page. |
| `CheckBox.js` | `CheckBox` | Sets whether a checkbox presents as neither checked nor unchecked. |
| `Clipboard.js` | `Clipboard` | Places text on the clipboard. |
| `Download.js` | `Download` | Hands the reader a file to save. |
| `Embedding.js` | `Embedding` | Loads a third-party embed script and has it process markup rendered since it last ran. |
| `Playback.js` | `Playback` | Mutes a rendered media element, resuming it if paused. |
| `Pointer.js` | `Pointer` | Routes a pointer to one element for the rest of its gesture, and releases it. |
| `Print.js` | `Print` | Prints the page as it stands, or markup by itself dressed in the page's stylesheets. |
| `Share.js` | `Share` | Whether the platform offers a share surface, and offering a subject to it. |
| `TextEntry.js` | `TextEntry` | Reads and clears a text entry's value, and reads or writes its selected range. |

## Components

A component script owns the whole browser side of one component, attachment and operations
together, and is named for the component with the `Forma` prefix dropped.

| Script | Wrapper | What it does |
| --- | --- | --- |
| `Dialog.js` | `Dialog` | Opens and closes a native dialog as a modal, closing on a scrim click when asked. |
| `Frame.js` | `Frame` | A box moved by its drag region, resized at its edges, stepped by the arrow keys and held within its bounds, reporting where it came to rest through `ReportPlacementAsync`. |
| `Window.js` | `Window` | Shown in the top layer and raised in front of the other windows; modal, it keeps Tab inside and stands on a scrim; either way it takes focus back to where it was. |

## Reaching a module from a component

A component constructs a wrapper from its `Context.JS`, `new Focus(Context.JS)`, attaches it from
`OnAfterRenderAsync` with the element it targets, and disposes it from its `DisposeAsyncCore`.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
