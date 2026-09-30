# Forma Virtualization Design

`FormaVirtualize` is Forma's virtualization: a component that shows a long collection renders its items through it. It renders the items near the viewport and stands spacers for the rest, keeping rendered any item its owner asks it to keep wherever that item lies. It lives in `Forma.Blazor`'s Virtualization category with the types its owners name.

## Purpose

A component over a large collection draws only what the reader can see, so its items come and go as the reader scrolls. An item holding the focus has to stay: when its element is removed, the browser drops the focus to the page, a roving tab stop that scrolled away leaves the component with none, and a focused checkbox inside an item goes with its item. A component that keeps its focus, its tab stop and its focused control needs the element that holds them to stay on the page. `FormaVirtualize` renders the window and keeps the items its owner names, so the element that holds the focus is the same element from the moment it is drawn until the owner lets it go.

## What it is responsible for

The virtualizer owns the geometry and the rendered set, and nothing about what the items mean:

- the visible range, the overscan beyond it, and the spacers that stand for the rest;
- the items its owner names as **pinned**, kept rendered outside the range by their stable keys;
- disjoint rendered ranges, the window and each pinned item, without rendering the items between them;
- scrolling an item into range, and telling its caller when that item's element is on the page;
- letting go of what an item that leaves the rendered set held.

What to pin is the owner's decision; selection, the current item, expansion and keyboard movement stay with the owner. An owner that moves the focus among its items pins the item holding the focus and the one holding its tab stop.

## Guarantees

- **Continuity.** A pinned item's element stays connected, and keeps the focus it holds, while the list scrolls it out of range and back and while the spacers and the ranges around it change, in a list of `div`s and in the rows of a table, a checkbox inside the item included.
- **Bounded rendering.** An item is rendered once however it is reached, in the window, pinned, or both. A pinned key that names no item renders nothing, and the virtualizer keeps nothing of an item once no range and no pin holds it.
- **Scroll completion.** A scroll to an item completes when the item is in view, which is more than its element existing: a pinned item's element may stand far outside the viewport. It completes false when no item stands at the position asked for, as when the set ends before it, and a newer scroll or the caller's token cancels it.

## Supported scope

- **One element per item.** Each item draws one element, which is what the virtualizer measures, keeps and scrolls to.
- **Sizing.** The virtualizer measures the room each drawn item takes, from its top to the next item's, so a gap or a margin between items counts, and sizes every spacer by their average, starting from the owner's estimate, so items of one height, or near enough, scroll smoothly; items that differ widely scroll true to their average.
- **Sources.** The items come from a list the owner holds, every item with a stable key, or from a provider that answers a window of positions with its items and the size of the whole set. A pinned key is found in the list, or among the items the provider has answered; after the owner asks for the items to be read again, a pinned item outside the window is read again at its position and kept only while it is still there.

## Rendering

Every rendered item and every spacer is a direct child of one parent, in the items' order, each item keyed by its stable key. A spacer stands in normal layout for the run of items it replaces, so the same rendering serves rows of a `<div>` list and rows of a `<tbody>`: the spacer is the element the owner names, a `div` or a `tr`. A pinned item outside the window stands between two spacers that split the run around it. An item that crosses between the window and its pinned place keeps its place among its siblings and so keeps its element, which is what keeps the focus on it.

## Sizes and measurement

An item's size starts as the estimate the owner gives and becomes the average room the drawn items take once some are, so a page zoomed or a density changed keeps the spacers true. One behavior per instance watches the scroll container, or the window where the list scrolls with the page, and reports where the list stands in it; it measures the drawn items as they resize. On a server-rendered circuit every report is a round trip, so the reports are throttled and the overscan covers the time one takes.

## Sources

The items come from a list the owner holds, or from a provider asked for one window at a time, with a placeholder drawn for an item not yet answered and a superseded request canceled. The owner can ask for the rendered set to be read again when its items changed.

## Scrolling

`ScrollToIndexAsync` brings an item into view, by the least scroll that shows it or aligned to the start, and completes once the item is in view, so the caller can focus it without waiting on a render of its own.

## Anchoring

An owner can open the list on an item, and hold the viewport in place when items arrive at either edge, telling a prepend from an append by a comparer it gives. The anchoring set is the initial item, the edge the viewport holds to, and the comparer.
