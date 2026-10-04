# Forma Virtualization Implementation Plan

The plan for building the virtualization that [the virtualization design](virtualization-design.md) states and moving the data view's views onto it, and the record of what has landed.

## Status

The virtualizer and the tree have landed; the focus handed on in one move is next.

## Steps

Each step is a commit of its own, taken in order.

1. **The virtualizer.** Done. `FormaVirtualize` as the design states it, verified in a browser as well as in the component and script tests, since component tests can see neither layout nor scrolling.
2. **The tree.** Done. `FormaDataTree` draws its visible nodes through the virtualizer and pins the node holding its tab stop and the node holding its focus, in either mode, a row or a checkbox, with the nodes either side of a focused checkbox, and when the node holding the focus goes away it hands the focus on as the data view design's tree section sets out. Gate, in a browser: Tab and Shift+Tab reach the tree after its tab stop scrolled away, and land on that node; scrolling with the focus inside the tree never leaves the focus on the page; the arrows, Home, End and type-ahead carry on from a node scrolled out of view and bring the one they reach into view; the selection does not change on any of it.
3. **The focus handed on in one move.** Open. A view whose focused item goes away marks the item that takes the focus in the same render, and a behavior attached once to the view hands the focus to it in the same browser task as the removal, so the browser reports one focus change rather than the page and then the item; the tree takes it first. Gate: under NVDA, a node taken away while it holds the focus and an ancestor closed over the focus each say the node that takes it, and nothing of the page before it.
4. **The grid.** The grid draws its rows through the virtualizer, with the anchoring set it uses today, the initial item, the edge the viewport holds to and the comparer, which the virtualizer gains in this step. Gate: the grid's virtualized scenarios behave as they do today, and a focused row or box scrolled away keeps the focus.
5. **The list.** The list moves after checking how its own focus behaves under the framework's virtualization. Gate: its virtualized scenarios behave as they do today, and a focused marker or box scrolled away keeps the focus.
6. **The cards.** The cards move after the same check, rows of cards measured as the virtualizer's items. Gate: as the list's, with the cards packing as they do today.

Each of the last three is a commit of its own and passes its own gate. The data view's rows and the selection behavior carry two remedies for what the framework's virtualization does, holding back the rows found under a scroll that has not landed and taking the focus back onto an item a render drew afresh; each is kept, or retired where the virtualizer makes it unneeded, as the views move. The selection behavior moves the focus among the markers drawn, by where they stand, so a view whose markers carry the focus pins the items either side of the focused one as well, which keeps the item an arrow reaches drawn beside it however far the view has scrolled.
