# Forma Showcase Design

The Showcase is where a Forma component is reviewed: its page renders the component in every state it can
enter. This design states how a page is reviewed beyond the reader's own window: in each theme and color
scheme, in a desktop's, a tablet's and a phone's viewport with the pointer each one has, in either writing
direction, and side by side. How a page is written, and what it must render, are the Showcase's README's
and the repository's component authoring guide's.

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
| Pointer | Fine or touch; a tablet and a phone start on touch, a desktop on fine |
| Theme | Each theme the Showcase registers, Forma's two and a sample host theme |
| Scheme | Light, Dark or System |
| Compare | Nothing, or one axis drawn side by side: devices, themes, schemes or directions |

The choices are part of the address, so a link to a page in a mode opens in that mode, and a reviewer can
hand a colleague exactly what they saw.

## Devices

Each device mode draws the page in a **device view**: an `<iframe>` element at the device's viewport in CSS
pixels, holding the page alone, without the Showcase's navigation, sidebar or review bar. A device view is a
real viewport, so every rule that answers the viewport's size answers the device's, and every rule that
answers a box answers the boxes inside it.

| Device | Viewport, portrait | Pointer it starts on |
| --- | --- | --- |
| Desktop | 1440 × 900 | Fine |
| Tablet | 820 × 1180 | Touch |
| Phone | 390 × 844 | Touch |

Landscape swaps the two sides. A view larger than the room the page has is scaled down as a whole to fit,
its own viewport unchanged, so a tablet's 1180 pixels stand inside a laptop's window, and a reviewer still
presses and types in it as in any page.

A view's page is a document of its own: it starts its own circuit, so it renders and behaves as a reader's
page does, and it keeps no state the reviewer's window has.

## Touch on a desk

A browser has no way to make a page believe it has a coarse pointer, so a device view draws touch itself.
Its document loads the Showcase's **touch emulation** first, before the page's stylesheets and scripts, and
under a touch pointer it:

- rewrites every pointer and hover condition in the document's stylesheets to the device's: each of
  `pointer`, `any-pointer`, `hover` and `any-hover` in a `@media` condition is replaced by a condition of
  the same truth, so a rule for a coarse pointer applies and a rule for hover doesn't, in a compound
  condition as in a plain one, and in a rule nested in `@container`, `@supports` or `@layer`;
- does the same for a stylesheet added later, as a component's own loads;
- answers `matchMedia` for those features as the device would, so a script that branches on the pointer
  branches as on the device;
- reports the touch points a phone reports through `navigator.maxTouchPoints`.

The view draws exactly what a phone draws, then, from the same stylesheets. What it can't make is a finger:
the reviewer's presses still arrive as a mouse's, so a gesture only a finger makes, a swipe to show a row's
actions or a long press to start selecting, is reached in a view as a keyboard or a pointer reaches it, and
as a finger reaches it on a touch screen or under the browser's own device emulation, which the touch
emulation leaves to the browser.

## Themes

The Showcase registers Forma's themes and a sample host theme, written as a host writes one against the
[theme design](theme-design.md)'s contract, so a reviewer sees a host's brand as well as Forma's own.

- **The reviewer's window** follows the theme picker in the sidebar, beside the culture switcher, which keeps
  the reviewer's choice as any host's reader's choice is kept.
- **A device view** takes its theme and scheme from its address and keeps nothing, so a view in Dark never
  changes the window's choice, and two views side by side stand in two themes at once.
- **The Themes page** shows each registered theme's palette, its base tokens and the tokens derived from them,
  in each of its schemes, with its contrast report, so a theme's values are reviewed as a component is.

## Direction

A page's direction comes from its culture, so a view in Hebrew draws right to left. The culture switcher
changes the window's culture and every view's with it, as the views' addresses carry the culture, and comparing
directions draws an English view beside a Hebrew one.

## Side by side

Compare draws one axis side by side, the page in each of its values, every other choice held:

- **Devices:** a desktop, a tablet and a phone view of the page.
- **Themes:** one view per registered theme, on the chosen device.
- **Schemes:** a light and a dark view.
- **Directions:** a left-to-right and a right-to-left view.

Each view in a comparison is scaled to share the stage's width, and each is a page of its own, so the views
never interfere. One axis at a time keeps a comparison to a handful of views; reviewing a combination, a
phone in the host's dark theme in Hebrew, is a single view with those choices.

## Where the parts live

The device view and the touch emulation are the Showcase's own, a component and a script with no dependency
on the rest of the Showcase, so they can move into a package of their own if a host wants to review its own
pages the same way. The theme picker and the theme scope they use are the theme system's, which any host
has.

## Decisions for the maintainer

- **The devices' sizes.** Proposed: the table above, a common laptop, the most common tablet in portrait and
  the most common phone, each in CSS pixels. A host's own sizes are a later addition.
- **A host's reuse.** Proposed: the device view stays in the Showcase until a host asks for it; its parts are
  written so the move is a move, not a rewrite.
- **Compare across two axes.** Proposed: one axis at a time, as above, since every view is a circuit and a
  page of its own, and two axes multiply them past what a stage shows legibly.
