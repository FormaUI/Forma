# Forma Showcase Design

The Showcase is where a Forma component is reviewed: its page renders the component in every state it can
enter. This design states how a page is reviewed beyond the reader's own window: in each theme and color
scheme, in a desktop's, a tablet's and a phone's viewport with the pointer each one has, in either writing
direction, and side by side. How a page is written, and what it must render, are the Showcase's README's
and the repository's component authoring guide's. [The implementation plan](showcase-implementation-plan.md)
holds the steps that carry it out.

## Purpose

A component draws differently by where it stands, and a reviewer's own window shows it in one place alone:
the window's width, one theme, under the desk's pointer. What a reader on a phone sees isn't reachable from
there:

- Forma's phone rules respond to the viewport, such as the explorer flowing in the page under `40rem`, so a
  narrow box inside a wide window never shows them; container rules, such as the data view's `36rem`, respond
  to the box, and show either way.
- Forma draws much differently under a coarse pointer: 44px targets, a switch at its row's end, no resize grip
  on a text area, strips a finger swipes rather than arrows. A desk's mouse never draws them.
- A theme other than the light one, and a host's own theme, can't be seen at all.

So a reviewer can approve a component that is broken where most readers will meet it. The review modes put
each of those in front of the reviewer, on the desk, from the component's own page.

## The review bar

Every component's page carries a **review bar** above it, in the Showcase's layout, so a mode applies to
whichever page is open and stays as the reviewer moves between pages. It holds:

| Control | Choices |
| --- | --- |
| Device | Window, the reviewer's own, as today; Desktop; Tablet; Phone |
| Orientation | Portrait or landscape, for a tablet and a phone |
| Pointer | Fine or Touch, for a device; a device chosen alone starts on its own, and Window draws under the reviewer's |
| Theme | Each theme the Showcase registers, Forma's two and a sample host theme |
| Scheme | Light, Dark or System |
| Compare | Nothing, or one axis drawn side by side: devices, themes, schemes or directions |
| Scale | Fit or 100%, beside the scale each view is drawn at |
| Reset | Starts every view again from the address |
| Review link | Copies a link that opens what the reviewer sees |

Every choice is part of the page's address, so a link to a page in a mode opens in that mode.

### The window and the page under review

The bar's controls draw the page under review, and the sidebar's draw the window around it:

- **The sidebar's** theme picker and culture switcher change the Showcase's own interface, and keep the
  reviewer's choice as a host keeps its reader's.
- **The bar's** Theme and Scheme draw the page under review in every device mode, and keep nothing beyond the
  address. They start as the window's choice. In Window, the page stands on a canvas of its own inside a
  [theme scope](theme-design.md#a-part-in-a-theme-of-its-own); in a view, the view's whole document is drawn
  in them.
- **The culture** of the page under review is the window's, since a document holds one culture: the page in
  Window stands in the window's own document, and every view follows the window's culture, except in a
  [comparison of directions](#direction).

### A link that reproduces the review

A link reproduces the choices, and two of them depend on the device that opens it. System draws the scheme
the colleague's device prefers, and Window draws at the size of the colleague's window, under their pointer.
The bar's **review link** resolves both, so a colleague sees what the reviewer saw:

- System becomes the scheme it drew, Light or Dark.
- Window becomes a view at the size the page had in the window, under the pointer the reviewer's device has.
  The Device control shows that view as a custom size, with its dimensions.

An address copied from the browser's own bar keeps the choices as they are.

## Devices

Each device mode draws the page in a **device view**: an `<iframe>` element at the device's viewport in CSS
pixels, holding the page alone, without the Showcase's navigation, sidebar or review bar. A device view is a
real viewport, so every rule that answers the viewport's size answers the device's, and every rule that
answers a box answers the boxes inside it.

| Device | Viewport | Pointer it starts on |
| --- | --- | --- |
| Desktop | 1440 × 900 | Fine |
| Tablet | 820 × 1180 | Touch |
| Phone | 390 × 844 | Touch |

The three are the Showcase's presets, each representative of its class in CSS pixels: a laptop's window, a
tablet's screen and a phone's. A tablet and a phone are listed in portrait, and landscape swaps the two
sides. A view can also stand at a custom size, as a review link from Window names.

### Scale

A view is drawn at one of two scales:

- **Fit** scales each view down as a whole to the room it has, and never up, so a tablet's 1180 pixels stand
  inside a laptop's window. It shows a page whole, and a comparison's views together.
- **100%** draws each view at its own size, and the stage scrolls to reach the rest. It shows the page as a
  reader's screen does, for working in it.

The bar shows the scale each view is drawn at. A 1440-pixel desktop fitted to a 400-pixel column stands at
28%, where a 44-pixel target is 12 pixels wide, so a reviewer judging a target's size works at 100%. A
scaled view still takes presses and typing as any page does, and its viewport is its device's at either
scale.

## A view keeps nothing

A view's document is a **preview**: a page drawn in the culture, theme, scheme and pointer its address names,
from its first render, which keeps none of them. Each view starts a circuit of its own, so it renders and
behaves as a reader's page does, and its page's state is its own.

Documents of one origin still share the browser's cookies and storage, so a preview is kept apart from them
on purpose:

- **The culture.** The Showcase serves previews under a path base of their own, `/preview`. A culture-route
  cookie belongs to the application at its path base, as [culture addresses](culture-design.md#culture-addresses)
  set out, so a preview landing in Hebrew writes the preview's cookie, never the window's. Nothing a preview
  shows depends on that cookie, since every preview's address decides its culture.
- **The theme.** The Showcase gives each preview request the choice its address names. A
  [request given its choice](theme-design.md#a-choice-given-to-a-request) writes no cookie and no store, and
  its page accepts each switch without keeping it.
- **Storage.** Nothing a view runs keeps anything in the browser's storage. The reading place the Showcase
  keeps in session storage is the sidebar's, which a view doesn't draw.

So no view changes the window's culture or theme, however it loads or changes, and two views side by side
stand in two cultures or two themes at once.

## Changing a view in place

A view keeps the page it shows as the reviewer changes its choices, so a dialog left open, a form left
invalid or a list scrolled to its end stays as the reviewer left it. The window never loads a view's frame
again to change it:

- **Size, orientation and scale** change the frame, and the document inside it lays out again at its new
  viewport.
- **The pointer** changes inside the view's document, as [a change of pointer](#a-change-of-pointer) sets out.
- **Theme, scheme and culture** navigate the view's document in place to its new address, as a link inside it
  would. The navigation keeps an interactive page, as a culture switch keeps it, and the document's
  coordinators take the new choice from its landing.

A view starts again, as a new document loaded from its address, only:

- when the reviewer presses Reset, which starts every view again;
- when a view first appears: on going from Window to a device, since the page in the window is another
  document, and for each view a comparison adds;
- when the reviewer goes to another page.

A view's frame stays where it stands in the stage as the stage's layout changes around it, since a frame
moved to another parent loads its document again. Starting a comparison keeps the current view as the pane
of its own value and starts views for the others, and ending one keeps the pane of the value the bar returns
to.

## The pointer in a view

A device view draws under the pointer the bar names, Fine or Touch, whatever pointer the reviewer's device
has. So a desk's mouse previews touch, and a tablet running the Showcase previews a fine pointer. Window
draws under the reviewer's own pointer.

### What it previews

A browser has no way to make a page believe it has another pointer, so a view's document loads the
Showcase's **pointer emulation** first, before any stylesheet or script. It previews Forma's
pointer-dependent styles and script branches: every rule whose condition names the pointer or its hover
capability, and every script asking `matchMedia` about them. Under Touch, a view draws 44px targets, a switch
at its row's end and strips a finger swipes, and drops every rule kept behind `(hover: hover)`.

It doesn't make a finger:

- The reviewer's presses still arrive as a mouse's, with a mouse's `pointerType`.
- The `:hover` state still follows the mouse, since a hover capability and the hover state are two things to
  CSS, so a rule matching `:hover` outside a hover condition still matches in a touch view.
- A gesture only a finger makes, a swipe to show a row's actions or a long press to start selecting, is
  reached in a view as a keyboard or a pointer reaches it. A finger reaches it on a touch screen or under the
  browser's own device emulation, which the pointer emulation leaves to the browser.

### What it rewrites

- **The stylesheets it reads** are the document's `<link>` and `<style>` stylesheets from the page's origin,
  and every stylesheet they import, as the Showcase's scoped bundle imports each assembly's. The browser keeps
  a stylesheet from another origin's rules from scripts, so the emulation leaves one as written and names it
  in the browser's console as a warning. Forma's stylesheets are all of the page's origin.
- **The conditions it replaces** are each `pointer`, `any-pointer`, `hover` and `any-hover` feature in a
  media condition, an `@media` rule's, an `@import`'s or a `<link>`'s. Each is replaced by a condition that is
  always true or always false, as the view's pointer makes it, and a rule nested in `@container`, `@supports`,
  `@layer` or a style rule is rewritten as one at the top is.
- **Every other feature** stays as written, for the browser to evaluate. Under Touch,
  `(max-width: 40rem), (pointer: coarse) and (max-height: 40rem)` still answers the viewport's height, as on a
  phone.

A media condition outside a stylesheet, such as a `<picture>` source's, is left as written, and Forma writes
none.

### When a stylesheet applies

A stylesheet never applies as written in a view:

- the document's first paint waits until every stylesheet in its head has loaded and been rewritten;
- a stylesheet added later, a component's own as it first renders or one an enhanced navigation brings,
  matches nothing until it has loaded and been rewritten.

### What scripts see

- **`matchMedia`** answers a query naming the pointer from the same rewriting, live. A list's `matches` is
  the rewritten query's, which the browser still evaluates for every other feature, and its `change`
  listeners are told whenever its answer changes, whether the viewport or the view's pointer changed it.
- **`navigator.maxTouchPoints`** reports none under Fine and a phone's under Touch.

### A change of pointer

A change of pointer happens in place. The emulation keeps every condition as written, rewrites each again
for the new pointer, and tells every list `matchMedia` handed out whose answer changed. The touch points
change with it.

## Themes

The Showcase registers Forma's themes and a sample host theme, written as a host writes one against the
[theme design](theme-design.md)'s contract, so a reviewer sees a host's brand as well as Forma's own.
[The window and the page under review](#the-window-and-the-page-under-review) sets out which controls draw
which.

**The Themes page** shows each registered theme's palette, its base tokens and the tokens derived from them,
in each of its schemes, with its contrast report, so a theme's values are reviewed as a component is.

## Direction

A page's direction comes from its culture, so a view in Hebrew draws right to left. Every view follows the
window's culture: the culture switcher changes the window's culture and every view's with it, in place.

Comparing directions draws a view in English beside one in Hebrew. The two differ in language and in formats
as well as in direction, so each pane is labeled with its culture and its direction. The pair stays English
and Hebrew whatever the window's culture, so the comparison holds while the reviewer changes the window's
language.

## Side by side

Compare draws one axis side by side, the page in each of its values, every other choice held:

- **Devices:** a desktop, a tablet and a phone view, all under the pointer the bar names, as a comparison holds
  every other choice; a device starts on its own pointer only when it's chosen alone. A tablet and a phone
  stand in the orientation the bar names.
- **Themes:** one view per registered theme.
- **Schemes:** a light and a dark view. System isn't a third, since it draws one of the two.
- **Directions:** English and Hebrew, as [direction](#direction) sets out.

Each pane is labeled with its value. Each view in a comparison is a page of its own, so the views never
interfere, and Fit scales each to share the stage's width. A pane can be expanded to take the whole stage,
keeping its document, and the other panes keep theirs, at their own sizes, out of view until it's restored.

A comparison from Window holds the size too. Window has no viewport of its own to divide, so each view is
drawn at the size the page had in the window as the comparison started, under the reviewer's own pointer,
and the bar shows that size. Dividing the stage among the views never changes what each page is laid out
at.

A comparison draws one axis at a time:

- what differs between its views is that axis alone, so the comparison shows what the axis changes;
- it stays to a handful of views, each a circuit and a page of its own, where two axes would multiply them
  past what a stage shows legibly.

Reviewing a combination, a phone in the host's dark theme in Hebrew, is a single view with those choices.

## Where the parts live

The device view, the preview and the pointer emulation are the Showcase's own: a component, a path base and a
script that depend on nothing else in the Showcase. What they rest on is the library's, which any host has:
the theme picker, the theme scope and a request given its theme choice are the theme system's, and a
culture-route cookie kept at its path base is the culture system's.
