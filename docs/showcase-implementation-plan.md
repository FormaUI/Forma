# Forma Showcase Implementation Plan

The steps that carry out [the Showcase design](showcase-design.md), in order, each landing with its tests and
its gate. The first four need nothing the theme system adds, so they land as soon as they're built; the last
three wait on the [theme implementation plan](theme-implementation-plan.md)'s steps they name.

## Steps

1. **The review bar.** The bar above every component's page, in the Showcase's layout, holding the Device,
   Orientation, Pointer and Compare controls, every choice kept in the page's address; Window, the reviewer's
   own, draws the page as today.
   - Gate: bUnit tests of the bar and its address; a browser check that a page opened at an address with
     choices opens in them.
2. **The device view.** Desktop, Tablet and Phone draw the page in an `<iframe>` at the presets' viewports, in
   either orientation, holding the page alone, each a document with a circuit of its own, and a view larger
   than the room it has scaled down as a whole.
   - Gate: a browser check that a view's viewport is its device's, that a phone rule answering the viewport
     applies in a phone view, as the explorer flowing in the page under `40rem` does, and that pressing and
     typing reach a scaled view.
3. **Touch emulation.** The script a touch view loads first, rewriting every pointer and hover condition in
   the document's stylesheets, a nested rule's and a later stylesheet's among them, answering `matchMedia` and
   reporting `navigator.maxTouchPoints` as the device does.
   - Gate: script tests of the rewriting, a compound condition and each nesting among them; a browser check in
     Chromium and WebKit that a touch view draws a target at 44px and drops a hover rule.
4. **Direction and side by side.** Compare draws devices or directions side by side, each view scaled to share
   the stage's width, and a view's culture follows the window's.
   - Gate: a browser check that a comparison draws one view per value, each in its own value alone.
5. **Themes and schemes in a view.** The Theme and Scheme controls, a device view's layout drawing the page in
   a theme scope named by its address on a canvas of its own, the sample host theme, and Compare across themes
   and schemes.
   - Waits on the theme plan's steps 5 and 6, Forma's themes and `FormaThemeScope`.
   - Gate: a browser check that two views side by side draw two themes, and that a view in Dark leaves the
     window's choice as it was.
6. **The window's theme picker.** The sidebar's picker beside the culture switcher, keeping the reviewer's
   choice as a host's reader's is kept.
   - Waits on the theme plan's step 11, `FormaThemePicker`.
   - Gate: the window switching in place, statically rendered and interactive.
7. **The Themes page.** Each registered theme's palette, its base tokens and the tokens derived from them, in
   each of its schemes, with its contrast report.
   - Waits on the theme plan's steps 3 and 5, the contrast check and Forma's themes.
   - Gate: the page's own checks, and the four gates the checking standard sets.
