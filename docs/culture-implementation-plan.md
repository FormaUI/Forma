# Forma Culture Implementation Plan

The plan for building what [the culture design](culture-design.md) states, and the record of what has
landed.

## Status

Steps 1 to 5 have landed. Step 6 is next.

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

- ✅ `FormaPropertyColumn`'s title, taken once, and from `DisplayAttribute.Name` as written, where
  `GetName()` resolves a resource-backed display name.
- ✅ `FormaEmojiPicker`'s nine default group titles, English text rendered as they are.
- ✅ The data view's view-switch names, which each view registers once as it initializes.
- ✅ The data view's group names, built when the view loads, by the default namer in the culture of that
  load.
- ✅ `FormaInputInspector`'s tools, built once as it initializes.
- ✅ `FormaComposerShortcutsDialog`'s rows, built as it opens and kept while it is open.
- ✅ `FormaCalendar`'s shown months, whose boundaries are kept from the calendar they were reckoned in.
- ✅ `FormaDataTree`'s input profile, whose open and close arrows are bound by the direction it read once.
- ✅ The anchored popover's script, which keeps the direction it read when the popover opened.
- ✅ The file viewer's office conversions, whose localized text is written into the converted document once
  per source.

Text an event produces, an announcement, a notice, a refusal, a validation message or a dialog's message,
stays out of step 1, as the design's exception for it sets out.

## Steps

Each step is built, reviewed and committed on its own.

1. **Output that follows the culture.** ✅ Every item the survey lists produces its output when it renders,
   and caches nothing that depends on the culture unless the cache is keyed by it; each fix is correct
   under any switching model. The last four items take these approaches:
   - **The view names.** A view's registration produces its name when asked, through `GetName()`, in place
     of the string it held; each view resolves the name from its own locale section, and the view switch
     asks for it as it renders.
   - **The group names.** `DataViewGroup` holds no name; the grouping makes its groups, each holding the
     grouping's name callback, and a group's `DisplayName` names the group each time it is read, so a heading
     reads the name as it renders. The grouping's `GetName` is gone, since a group needn't be handed its own
     key, and the host's `Func<TValue, string>` callback is unchanged. The callback may run once per heading
     shown on each render, and suits repeated synchronous calls.
   - **The popover's direction.** Every reposition reads the anchor's computed direction, so a local `dir`
     or a style override is respected, and a change of direction schedules a reposition once it reaches the
     document, even where the anchor's size and position stay the same, which a resize observer would not
     report.
   - **The office conversions.** The file viewer's resolution is keyed by the source, the UI culture and,
     where the converted output depends on it, the formatting culture, each captured as the conversion
     starts and given to it; a conversion that completes after another request began is discarded, so an
     English conversion finishing after a switch to Hebrew never replaces the Hebrew one.

   ✅ Departure until step 4: a heading's call and a view's name run in the ambient culture as the heading or
   the switch renders; once the state exists, they run in the bounded scope the design sets out, in the
   view's cultures.

   Gate: each item shown under one culture and again under another, by the test its behavior takes: a
   component test for rendered names and formats, a script test and a browser check for the popover's
   direction and its reposition, a conversion test for the converted document's labels, and an
   asynchronous test for a conversion that completes after a switch.
2. **What the platform does today.** ✅ Taken alongside step 1's last items, since its probes depend on none
   of them; each records its configuration and what it observed, and the results choose how culture
   addresses follow the state:
   - whether the cultures an event handler assigns reach the circuit's next event, `CurrentCulture` and
     `CurrentUICulture` each recorded;
   - whether a navigation under global interactivity, with an interactive `Routes`, finds a page declaring
     `/about` at `/he/about`; the Showcase's islands can't establish it;
   - whether an enhanced navigation updates `<html lang dir>`, and whether it replaces a dynamic change to
     them;
   - whether location changes reach the interactive runtimes holding the page's islands, an island that
     survives the navigation apart from one created by it.
3. **The working example.** ✅ A small working example of explicit culture reads and bounded scopes, through
   real Razor components on both component bases, the input base among them:
   - the first render in the intended cultures;
   - an immediate switch, whose scheduled output is in the new cultures;
   - a delayed operation begun in the old culture, whose output is in the state's;
   - two isolated scopes, each in its own cultures;
   - a gated row rendering again when the revision changes;
   - strings, formats and direction following the state;
   - a host's ambient-culture callback running in the intended cultures;
   - a bounded scope restoring both of the caller's cultures, on success and when the operation throws;
   - enhanced and interactive navigation, Back and Forward among them, as the design requires.

   Gate: each case behaves as the design states, or the design changes before step 4.

   Each case behaved as the design states, on step 2's probe host, with islands and with an interactive
   `Routes`, driven in Chrome, through a probe state and probe bases standing in for step 4's:
   - the first render was English, and a switch to Hebrew rendered the text, its direction, the input's
     value and the gated row in Hebrew at once; the row rendered again once on the revision, and not when
     its parent rendered again with the same data;
   - a delayed operation begun in English finished after the switch with its own flow still `en/en`, and
     its output, read from the state, was Hebrew;
   - a callback reading the ambient culture, run in a bounded scope from that English flow, produced
     Hebrew and left the caller's `en/en` in place, on success and when it threw;
   - a second circuit of the same host stayed English throughout;
   - the state followed the address through enhanced navigation to `/he/example` and back to `/example`,
     and through Back and Forward.
4. **The culture state and the component bases.** ✅ The scoped state, `DefaultCultureState` its default; both
   component bases injecting it as `CultureState` and subscribing to it; components reading its cultures and
   its direction for their output; the localization layer looking strings up in a given UI culture, and the
   bases' strings in the state's; ambient-culture APIs and callbacks run in the bounded scope; every render
   gate counting the revision; work a component resolves or loads in a culture, the file viewer's resolution
   among it, started again on a change, which sets no parameter; and the cascaded culture retired. Its parts
   land one commit at a time:
   - ✅ The routing assembly becomes `Forma.Framework.Globalization.AspNetCore`, its namespace, README and
     tests with it, so the name `Forma.Framework.Globalization` passes to the platform-neutral state, as C1
     sets out.
   - ✅ The state: `ICultureState`, announcing its changes through `INotifyStateChanged`, and
     `DefaultCultureState`, registered once per user scope by the feature's registrar, which `AddFormaUI`
     calls, in a platform-neutral `Forma.Framework.Globalization`.
   - ✅ The bounded scope: `AmbientCulture.Run` runs a synchronous operation in the cultures it is given and
     restores the caller's in a `finally`, and the state's `Run` runs one in the state's cultures; an
     operation that returns a task is refused, since the scope spans no asynchronous work.
   - ✅ The strings: a locale's `WithCulture` returns one bound to the cultures given, choosing its strings,
     fallback and plural forms by the UI culture and formatting the values in their holes by the culture, a
     value that takes no culture of its own among them; an unbound locale keeps reading the current cultures.
     Both bases inject the state as `CultureState`, moved forward from the bases' part since their strings
     need it, and their `L` is one locale that binds again on each revision of the state.
   - ✅ Following the state: both bases subscribe to it with their first parameters, render again on each
     change unless disposed, and unsubscribe as they are disposed; the data view's four item gates count
     the state's revision beside their own, so an item whose data is unchanged draws again in a new
     culture; and the cascaded culture is retired.
   - ✅ The output: every component formats, parses, compares and lays out by the state's cultures and
     direction; the data view's group names and column titles, and a cell's value that takes no culture
     of its own, run in the bounded scope; and the pagination's summary and the file viewer's resolution
     start again after a change, which sets no parameter.
   - ✅ The Framework's measurements and orders take their culture: the resource formatter, a resource's
     size, the explorer's grouping and the catalog view's kind order are each given the culture they
     write or compare in, the catalog view ordering again when its UI culture changes, and every
     component gives them the state's.
   - ✅ The ban: a component project bans the members that read or set the ambient culture, `CultureInfo`'s
     current cultures, the current format infos and the current-culture comparisons, each banned symbol
     naming the culture state member to use, through the banned-API analyzer at error.

   ✅ Departure until step 5: a resource provider converts in the ambient culture as the file viewer calls it.
   The resolution a change starts runs in the change's render, whose flow carries the new cultures, while
   one a parameter starts runs in its event's, the connection's culture until the circuit handler applies
   the state before every inbound activity.

   Gate: a change renders an input, a component on the base and a gated grid row again, each in the new
   cultures, and a bounded scope restores the caller's cultures.
5. **The server.** ✅ The circuit handler applies the culture before every inbound activity. Gate: a test of
   the handler around an inbound activity, and an event after a switch rendering in the new culture.

   The handler sets both cultures from the state in each activity's own flow, leaving the dispatching flow's in
   place, and the feature's server registrar, which `AddFormaUI` calls, registers it beside the state.

   On step 2's probe host, with the handler registered and a switch made through the state, driven in Chrome
   with an interactive `Routes` and again with islands: the switching event read `he/he`, the next event and
   the one after it read `he/he` where step 2 recorded `en/en`, and a second circuit of the same host read
   `en/en`.
6. **The document.** The ASP.NET Core integration keeps `<html lang dir>` in step, again after each
   enhanced navigation. Gate: a script test of the operation, and a browser check of both attributes
   after a switch and after an enhanced navigation.
7. **Culture addresses.** The switching entry point, reconciliation when the integration starts,
   interactive routing, Back and Forward, and the cookie, as step 2's results choose. Gate: a browser check
   of each navigation, and of a new tab opening in the culture last chosen.
8. **The edit in progress.** An uncommitted edit keeps its parsing culture until it commits or is
   cancelled, as the design's editing section sets out: a failed parse keeps its text and its parsing
   culture, a committed or cancelled edit takes the current culture, a committed value formatted again
   keeps its value and stays unmodified, focus alone is no edit, and the field's labels, accessible names
   and direction follow the new UI culture throughout. Gate: a component test of each rule, and a browser
   check of an edit across a switch.
9. **The Showcase.** The Showcase switches language in place, keeping the user's work, with the new load
   still supported; the README, the globalization READMEs and the localization and authoring rules follow.
   Gate, in a browser: switching between English and Hebrew in place lays the page out right to left and
   keeps it there on the next click, an edit in progress keeps its text and its parsing culture, and a
   popover already open takes the new direction.
10. **WebAssembly and Hybrid,** each when Forma supports its render mode, Hybrid after its ownership is
    chosen.

## To verify

Step 2 settled each, on a probe host of its own: Forma's culture routing with `CulturePrefix.ExceptDefault`,
English as the default and Hebrew, under .NET 11 release candidate 1 in the Development environment, run once
with an interactive `Routes` and once statically with islands and enhanced navigation, each driven in Chrome.

- ✅ A culture an event handler assigns doesn't reach the circuit's next event: the event that set both cultures
  to Hebrew read `he/he`, and the next event read `en/en`.
- ✅ Interactive route matching, under global interactivity, doesn't find a page at a culture-prefixed address,
  so culture addresses don't work under global interactivity today. A navigation within the application to
  `/he/other` reached the router's not-found content; a full load of `/he/other` rendered the document in
  Hebrew, `lang=he dir=rtl`, through the middleware, and the interactive router, once the circuit started,
  routed the browser's address, `/he/other`, found no page and replaced the one rendered. The Showcase, whose
  routes are static with islands, doesn't meet it. Step 7 makes route matching understand the culture segment.
- ✅ An enhanced navigation sets the `<html>` element's attributes from the server's response, undoing a
  dynamic change to them: a document set to `lang=xx dir=rtl` returned to `lang=en dir=ltr` on a navigation
  within the same culture, and took `lang=he dir=rtl` on a navigation to `/he/about`.
- ✅ Location changes reach an interactive island that an enhanced navigation keeps, the layout's and a page's
  island of one type at one place alike, Back among them; an island the navigation creates sees
  the location changes after it, and never the one that created it, so it reads the current address as it
  starts.
