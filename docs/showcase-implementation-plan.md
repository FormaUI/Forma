# Forma Showcase Implementation Plan

The steps that carry out [the Showcase design](showcase-design.md), in order, each landing with its tests and
its gate. Three conditions hold before the review bar is built:

- **The pointer emulation is proven** by a prototype, since the rest of the design rests on it (step 1).
- **A preview keeps nothing:** a view is shown to leave the window's culture as it was before any control
  opens one (step 2).
- **A view keeps its page:** every step from step 4 on keeps the page a view shows through each change it
  adds, and its gate shows that it does.

The first six steps need nothing the theme system adds, so they land as soon as they're built. The last three
wait on the [theme implementation plan](theme-implementation-plan.md)'s steps they name.

## Steps

1. **The pointer emulation, proven.** A prototype of the emulation, drawing Forma's own stylesheets in a frame
   on a page of its own, before the review bar exists. It shows, in Chromium and WebKit:
   - every pointer and hover condition in Forma's stylesheets rewritten, the compound ones and each nesting
     among them, with every other feature left to the browser;
   - no stylesheet applying as written, at the first paint, for a component's stylesheet loaded later and for
     one an enhanced navigation brings;
   - `matchMedia` answering live and telling its listeners of each change;
   - Touch forced on a desk, and Fine forced in a browser with a touch screen;
   - a change of pointer in place.

   Each result is recorded under this step. A result that falls short changes the design's
   [pointer in a view](showcase-design.md#the-pointer-in-a-view) before step 5 starts.
   - Gate: script tests of the rewriting; a browser check of each result above.
2. **The preview.** The Showcase serves a page alone, without the window's parts, under its preview path base,
   `/preview`, in the culture its address names.
   - Gate: a browser check that an English and a Hebrew preview loaded beside the window, and one changed in
     place to another culture, leave the window's culture-route cookie as it was, and that no preview response
     sets a cookie the window reads.
3. **The review bar.** The bar above every component's page, in the Showcase's layout, holding the Device,
   Orientation, Pointer, Scale and Compare controls, Reset and the review link, every choice kept in the
   page's address; Window, the reviewer's own, draws the page as today.
   - Gate: bUnit tests of the bar and its address; a browser check that a page opened at an address with
     choices opens in them.
4. **The device view.** Desktop, Tablet, Phone and a custom size draw the page in a preview in an `<iframe>`,
   in either orientation, at Fit or 100% with the scale shown; a change of size, orientation or scale keeps
   the view's document; Reset starts it again; the review link resolves Window to a custom size.
   - Gate: a browser check that a view's viewport is its device's, that a phone rule answering the viewport
     applies in a phone view, as the explorer flowing in the page under `40rem` does, that pressing and typing
     reach a scaled view, and that a dialog left open stays open through a change of size, orientation and
     scale and closes on Reset.
5. **The pointer emulation.** The prototype made the Showcase's script, which every view loads first, under
   Fine or Touch, with `matchMedia`, the touch points and a change of pointer in place.
   - Gate: script tests of the rewriting, a compound condition and each nesting among them; a browser check in
     Chromium and WebKit that a touch view draws a target at 44px and drops a hover rule, that a fine view in
     a browser with a touch screen draws a fine pointer's targets, and that a dialog left open stays open
     through a change of pointer.
6. **Direction and side by side.** Compare draws devices or directions side by side, each pane labeled, a
   device comparison under one pointer, a direction comparison's pair fixed, a comparison from Window at the
   window's size, and a pane expanded and restored, each view's frame kept where it stands. Every view
   follows the window's culture in place.
   - Gate: a browser check that a comparison draws one view per value, each in its own value alone; that a
     change of the window's culture changes every view in place, keeping a dialog left open, and leaves a
     direction comparison's pair as it was; and that starting and ending a comparison keeps the current
     view's document.
7. **Themes and schemes in a view.** The Theme and Scheme controls; a preview given its theme from its
   address; the page in Window drawn in a theme scope on a canvas of its own; a change of theme or scheme in
   place; the sample host theme; Compare across themes and schemes; and the review link resolving System.
   - Waits on the theme plan's steps 5 and 6, Forma's themes and `FormaThemeScope`, and steps 8 and 9, a choice
     given to a request and the coordinator accepting a page's switches without keeping them.
   - Gate: a browser check that two views side by side draw two themes; that a view in Dark, loaded and
     changed in place, leaves the window's choice cookie as it was; and that a dialog left open stays open
     through a change of theme.
8. **The window's theme picker.** The sidebar's picker beside the culture switcher, keeping the reviewer's
   choice as a host's reader's is kept.
   - Waits on the theme plan's step 11, `FormaThemePicker`.
   - Gate: the window switching in place, statically rendered and interactive.
9. **The Themes page.** Each registered theme's palette, its base tokens and the tokens derived from them, in
   each of its schemes, with its contrast report.
   - Waits on the theme plan's steps 3 and 5, the contrast check and Forma's themes.
   - Gate: the page's own checks, and the four gates the checking standard sets.
