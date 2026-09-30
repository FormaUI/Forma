# Forma Virtualization Implementation Plan

The plan for building the virtualization that [the virtualization design](virtualization-design.md) states and moving the data view's views onto it, and the record of what has landed.

## Status

The virtualizer has landed; the tree is next.

## Steps

Each step is a commit of its own, taken in order.

1. **The virtualizer.** Done. `FormaVirtualize` as the design states it, verified in a browser as well as in the component and script tests, since component tests can see neither layout nor scrolling.
2. **The tree.** `FormaDataTree` draws its visible nodes through the virtualizer and pins the node holding its tab stop and the node holding its focus, in either mode, a row or a checkbox. Gate, in a browser: Tab and Shift+Tab reach the tree after its tab stop scrolled away, and land on that node; scrolling with the focus inside the tree never leaves the focus on the page; the arrows, Home, End and type-ahead carry on from a node scrolled out of view and bring the one they reach into view; the selection does not change on any of it.
3. **The grid.** The grid draws its rows through the virtualizer, with the anchoring set it uses today, the initial item, the edge the viewport holds to and the comparer, which the virtualizer gains in this step. Gate: the grid's virtualized scenarios behave as they do today, and a focused row or box scrolled away keeps the focus.
4. **The list.** The list moves after checking how its own focus behaves under the framework's virtualization. Gate: its virtualized scenarios behave as they do today, and a focused marker or box scrolled away keeps the focus.
5. **The cards.** The cards move after the same check, rows of cards measured as the virtualizer's items. Gate: as the list's, with the cards packing as they do today.

Each of the last three is a commit of its own and passes its own gate. The data view's rows and the selection behavior carry two remedies for what the framework's virtualization does, holding back the rows found under a scroll that has not landed and taking the focus back onto an item a render drew afresh; each is kept, or retired where the virtualizer makes it unneeded, as the views move.

## Open decisions

- **Where a tree's tab stop returns.** Outside a sidebar, the tree's tab stop follows its current node rather than the node the reader focused, so Tab back into a plain tree lands on the current node or the first one rather than where the reader left it, with or without virtualization. Moving the tree onto the virtualizer keeps that as it is; whether the tab stop should follow the focus is a decision of its own.
- **Leaving a node that went away.** A node that collapses under the focus gives it to its parent, which is the tree pattern's own rule. A node removed under the focus gives it to a neighbor, and which one, the next sibling, then the previous, then the parent, is a policy to choose rather than a rule to follow. It stays open through the virtualizer's own step and is settled before the tree's step is complete.
