# Forma Culture Implementation Plan

The plan for building what [the culture design](culture-design.md) states, and the record of what has
landed.

## Status

Nothing has landed. Step 1 is next.

## Where Forma stood

The survey the steps rest on, taken before step 1.

- **The culture is decided per request.** `CultureRoutingMiddleware` reads the culture from the first
  segment of the path, or from the `forma-culture` cookie for a framework request such as `/_blazor`,
  sets both cultures and the `IRequestCultureFeature`, and removes the segment before routing. It sees
  requests only, so a navigation the router handles in the browser reaches the router with the segment
  as written.
- **A circuit runs in the culture of its connection.** The circuit's host sets no culture of its own,
  so every event runs in the culture of the request that opened the connection, and a culture an event
  handler assigns is asynchronous-local.
- **Components read the culture in force as they render,** and the string layer is culture-independent:
  a locale looks the culture up on every call, and its file cache is keyed by culture.
- **The document's language and direction** come from the request, through the host's static `App`
  component.
- **A host switches language with a new load,** as the Showcase does with full-load language links that
  keep the reader's place.
- **A cascaded culture** on the component base makes a component on that base render again when the
  host cascades a new culture from its root, and does nothing else the design requires.

What breaks when the culture changes in place:

| What fails | Why | What the reader sees |
| --- | --- | --- |
| The change does not last on the server | The culture is asynchronous-local, and the next event runs in the connection's culture again | The new language once, and the old one again on the next click |
| Nothing renders again | A component renders again when its parameters change, and a culture change is no parameter | The date box keeps its old format |
| Some components refuse to render even when asked | The data view's rows, cards, list items and tree nodes gate their renders, and the culture is not among what they compare | Grid cells and their accessible names stay in the old language |
| Some text is computed once | It is built when the component initializes or kept in a field | The data view's view-switch names and group names, the input inspector's tools, the composer's shortcut rows |
| Direction is read once | It is kept from the first render, or from when a popover opened | The data tree's arrow keys stay reversed after switching between English and Hebrew, and an open popover keeps its old side |
| The document stays in the old language | `<html lang dir>` is static, and nothing in a circuit updates it | A page switched to Hebrew in place is laid out left to right |
| The address and the culture disagree | The culture changed and the address did not | `/he/about` shows English |

The output step 1 brings under the design's rule, that nothing depending on the culture outlives a render
unless it is keyed by the culture:

- `FormaPropertyColumn`'s title, taken once, and from `DisplayAttribute.Name` as written, where
  `GetName()` resolves a resource-backed display name.
- `FormaEmojiPicker`'s nine default group titles, English text rendered as they are.
- The data view's view-switch names, which each view registers once as it initializes.
- The data view's group names, built when the view loads, by the default namer in the culture of that
  load.
- `FormaInputInspector`'s tools, built once as it initializes.
- `FormaComposerShortcutsDialog`'s rows, built as it opens and kept while it is open.
- `FormaCalendar`'s shown months, whose boundaries are kept from the calendar they were reckoned in.
- `FormaDataTree`'s input profile, whose open and close arrows are bound by the direction it read once.
- The anchored popover's script, which keeps the direction it read when the popover opened.
- The file viewer's office conversions, whose localized text is written into the converted document once
  per source.

Text an event produces, an announcement, a notice, a refusal, a validation message or a dialog's message,
stays out of step 1, as the design's exception for it sets out.

## Steps

Each step is built, reviewed and committed on its own.

1. **Output that follows the culture.** Every item the survey lists renders from the culture in force,
   and caches nothing that depends on the culture unless the cache is keyed by it; each fix is correct
   under any switching model. Gate: a component test for each item, rendering it under one culture and
   again under another, shows the second culture's output.
2. **What the platform does today.** Establish whether interactive route matching, under global
   interactivity, finds a page at a culture-prefixed address, and settle the [items to
   verify](#to-verify), before choosing how culture addresses follow the state.
3. **The working example.** A small working example that proves the runtime behavior the design rests on:
   - an immediate switch, whose scheduled renders are already in the new culture;
   - a delayed completion, a task begun in the old culture whose render shows the new one;
   - two isolated scopes in different cultures;
   - navigation, enhanced and interactive, Back and Forward among them;
   - culture application reaching real Razor rendering through both component bases, the input base's
     path among them;
   - a render in the new culture leaving the culture of the older work unchanged afterward, when the
     render throws too.

   Gate: each case behaves as the design states, or the design changes before step 4.
4. **The culture state and the component bases.** The state, both component bases subscribing to it and
   applying it for each render as step 3 proved, and every render gate counting its revision; the
   cascaded culture retired. Gate: a change renders an input, a component on the base and a gated grid row
   again, each in the new culture.
5. **The server.** The circuit handler applies the culture before every inbound activity. Gate: a test of
   the handler around an inbound activity, and an event after a switch rendering in the new culture.
6. **The document.** The ASP.NET Core integration keeps `<html lang dir>` in step, again after each
   enhanced navigation. Gate: a script test of the operation, and a browser check of both attributes
   after a switch and after an enhanced navigation.
7. **Culture addresses.** The switching entry point, reconciliation when the integration starts,
   interactive routing, Back and Forward, and the cookie; and `Forma.Framework.Browser.Interop.Server`
   renamed `Forma.Framework.Browser.Interop.AspNetCore`. Gate: a browser check of each navigation, and of
   a new tab opening in the culture last chosen.
8. **The Showcase.** The Showcase switches language in place, keeping the user's work, with the new load
   still supported; the README, the globalization READMEs and the localization and authoring rules follow.
   Gate, in a browser: switching between English and Hebrew in place lays the page out right to left and
   keeps it there on the next click, an edit in progress keeps its text and its parsing culture, and a
   popover already open takes the new direction.
9. **WebAssembly and Hybrid,** each when Forma supports its render mode, Hybrid after its ownership is
   chosen.

## To verify

- That a culture an event handler assigns doesn't reach the circuit's next event, which the circuit's
  host, setting no culture of its own, implies.
- Whether interactive route matching, under global interactivity, finds a page at a culture-prefixed
  address.
- Whether an enhanced navigation updates the `<html>` element's own attributes, or undoes a dynamic change
  to them.
- Whether location changes reach every interactive island on an enhanced navigation.

## Open

- **Where a component base wraps a render** (C7). The design requires a boundary that covers a Razor
  component's `BuildRenderTree` and a component's first render, and saves and restores both cultures; step
  3 finds it, and the design adopts the temporary application across the components only once the working
  example proves it.
