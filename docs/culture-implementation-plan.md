# Forma Culture Implementation Plan

The plan for building what [the culture design](culture-design.md) states, and the record of what has
landed.

## Status

Steps 1 to 9 have landed. Step 10 waits until Forma supports a WebAssembly or Hybrid host, and the plan
stands until it lands.

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
   - ✅ The routing assembly becomes `Forma.Framework.Culture.Router.AspNetCore`, its namespace, README and
     tests with it, so the name `Forma.Framework.Culture` passes to the platform-neutral state, as C1
     sets out.
   - ✅ The state: `ICultureState`, announcing its changes through `INotifyStateChanged`, and
     `DefaultCultureState`, registered once per user scope by the feature's registrar, which `AddFormaUI`
     calls, in a platform-neutral `Forma.Framework.Culture`.
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
6. **The document.** ✅ The document culture synchronizer keeps `<html lang dir>` on the state, again after each
   enhanced page update, as the design's document section sets out. Gate: a script test of the operation,
   and a browser check of both attributes after a switch and after an enhanced navigation.

   `Forma.Framework.Culture.Browser.Interop` ships the script, its initializer and the service, and every
   Framework assembly became a package first, so this package could build on the JavaScript interop
   floor. The culture's circuit handler, moved with its registrar out of
   `Forma.Framework.Culture.Router.AspNetCore` into `Forma.Framework.Culture.Blazor.Server`, the
   render mode's half, starts the document culture synchronizer as each connection comes up, and the routing package
   holds routing alone again.

   On step 2's probe host, driven in Chrome with islands and again under global interactivity:
   - a switch to Hebrew set `he/rtl`, which held through an enhanced navigation within the culture and
     through a streaming page's update;
   - five rapid switches ending in English ended on `en/ltr`;
   - a switch landing while the browser was offline left the document `en/ltr` until the connection
     returned, and `he/rtl` after;
   - after a pause and a resume, the client reported the old circuit closed and a new one opened, the
     resumed state started from the request's culture, and the document followed it and the next switch;
   - with islands, a page without them kept `he/rtl` while the circuit lived and showed its served `en/ltr`
     once the client closed the circuit, about two seconds later under the default inactivity timeout,
     and the next islands page's circuit registered and switched;
   - a host installing `Forma.Framework.Culture.Blazor.Server` alone received the five assemblies
     it needs and the page listed the interop package's initializer.

   Departure until step 7: navigation to another culture's address does not update the culture state.
   While the existing registration remains live, the document continues to follow that state: a
   navigation to `/he/about` from an English state, with islands, left the document `en/ltr` over a page
   the server rendered in Hebrew.
7. ✅ **Culture addresses.** The switching entry point, reconciliation when the integration starts,
   interactive routing, Back and Forward, and the cookie, as step 2's results choose. Gate: a browser check
   of each navigation, and of a new tab opening in the culture its address names.

   Probes come first, each recording its configuration and what it observed, and their results choose the
   routing and synchronization mechanism:
   - whether a culture-prefixed `<base href>` makes interactive route matching find `/he/other`, judged
     against the whole requirement: an in-place switch, links, assets, an application under a path base,
     and both prefix policies; one that works only by reloading on a switch establishes a supported full
     load and doesn't complete this step;
   - the order of the address, the state's change, the page's patch, the document's attributes and the
     visible output, for a switch, a plain link and Back and Forward, with the document culture synchronizer in place,
     under delayed delivery and through rapid consecutive navigations; an eventual `he/rtl` doesn't show the
     absence of a flash;
   - whether reconciliation can run, at the start and on reconnection, before the document culture synchronizer
     applies the culture the connection arrived with, since the circuit handler starts it at once today;
   - what a resumed circuit's state starts from, since step 6 saw it start from its connection's culture
     and lose an in-place switch across a pause.

   ✅ Open with the design, and settled in its culture addresses by the probes below: `CultureRouter` is
   a singleton while navigation and the state belong to a circuit, so the switching entry point must
   respect that separation, and its `CurrentRoute` reads the ambient UI culture, so work begun before a
   switch can report the old route. The cookie is written `HttpOnly`, and the middleware leaves one whose
   token already matches, so the switch to a cookie script can write includes replacing an existing one.

   The gate adds: two tabs in two cultures, one switching while the other reconnects; an existing
   installation's `HttpOnly` cookie, alone and beside the new one, at `/` and under a path base; a circuit
   closing while the document waits for its acknowledgement; and the flash checks measured from the traced
   order, in the attributes and in a surviving island's content.

   The probes ran on step 2's probe host under .NET 11 release candidate 1, driven in Chrome, the host
   started in each configuration by the probe itself, with English the default culture, Hebrew beside it
   and `iw` its alias:
   - ✅ A culture-prefixed `<base href>` lets interactive routing find a page at its culture's address, and
     leaves every navigation to another culture's address outside the base. Under global interactivity
     with `ExceptDefault`, a full load of `/he/about` rendered the page with the base `/he/`, where the
     plain base reaches not-found, and a relative link reached `/he/other` in the same circuit; `Always`
     behaved the same, `/about` redirecting to `/en/about` under the base `/en/`. A link to `/about`, the
     switch made by navigating, and Back across the cultures each loaded a new document and circuit;
     `/iw/other` and `/HE/other` reached `/he/other` through the middleware's redirect, `/he/en/other` was
     refused with 400 and `/xx/other` answered 404. Islands behaved the same. So the base gives a
     supported full load, and doesn't make a switch in place.
   - ✅ Under a culture-prefixed base, the framework's own requests take the culture segment,
     `/he/_blazor/negotiate` and `/he/_blazor/disconnect`, and the middleware, which tells a framework
     request by its first segment, takes them for documents and writes the cookie for them. Leaving
     `/he/about` for `/about`, the old page's disconnect beacon arrived after `/about` had written `en`,
     set the cookie back to `he`, and the next circuit opened in Hebrew at the English address. The
     middleware now tells a framework request by its path without the culture segment, writes no cookie
     for one, and gives it the culture its segment names before the cookie's.
   - ✅ Under a `/app` path base, the circuit's negotiation at `/app/he/_blazor/negotiate` answered 405
     with the culture base, and the circuit connected with the plain base; the cause isn't established.
     The cookie is written at the path `/` whatever the path base.
   - ✅ An enhanced navigation tells the circuit of its location change as the navigation starts, before
     its response. With the state following each location change, a link to `/he/about` from English
     set `he/rtl` 17 ms in, over the English page, which the patch replaced at 34 ms; under 400 ms of
     latency, the English page stood under `he/rtl` from 30 ms to 429 ms. Back, Forward and the switch made
     by navigating had the same order, and two navigations 30 ms apart ended on the second's culture.
     The patch wrote neither attribute, since both already matched.
   - ✅ The address is available as a circuit opens: reconciling the state with it there, ahead of the
     culture's own handler, changed the state before the connection came up, so the synchronizer
     registered the address's culture and never wrote another.
   - ✅ A resumed circuit's state starts from the cookie at the time it resumes. After a second tab loaded
     `/he/about`, the first tab, at `/about`, paused and resumed into `he/rtl` over its English page and
     stayed there; with reconciliation at open, it resumed in English. An in-place switch made before the
     pause wasn't kept either way, since it changes neither the address nor the cookie today.

   A second round settled what the first left open, on the same host and runtime:
   - ✅ Blazor's routing doesn't offer a public way from an address to `RouteData`: `RouteData`,
     `RouteView` and the `Router` are public, while the route table, its factory and the template matcher
     are internal, and the `Router` reads its address from the `NavigationManager` alone. A routing
     component matching the address without its culture segment would copy Blazor's matching, so it fails
     as a candidate.
   - ✅ A page carrying a culture route beside its plain one, `@page "/tabout"` and
     `@page "/{culture}/tabout"`, switches in place under global interactivity: a link from `/tabout` to
     `/he/tabout` kept the circuit and the page's instance, the document took `he/rtl` at 14 ms and the
     state at 16 ms, Back and Forward did the same, and a full load of `/he/tabout` rendered through the
     plain route on the server and the culture route in the browser. It binds the segment to whatever it
     holds, so `/xx/tabout` rendered the page with the culture `xx` and a root `/{culture}` caught `/zz`;
     a constraint holds beside it, `/he/titem/5` matching and `/he/titem/five` not; a page declaring the
     culture route without a `Culture` parameter ended its circuit with an exception; and a page without
     the culture route wasn't found at `/he/about`.
   - ✅ The circuit hears an enhanced navigation's location change before the client can report its start:
     across links, Back, Forward and a cancelled navigation, the report never arrived first. Following each
     location change, the document and the state took the new culture over the old page until its
     response, 400 ms under 400 ms of latency, and 150 ms for a navigation another replaced. Following the
     client's report that the page landed, the old culture held until the patch, which wrote the new
     pair; the synchronizer then wrote the owner's old pair back over it, and the circuit's follow, a
     round trip later, restored the new one: 18 ms of the old pair over the new page under 400 ms of
     latency. In both, the replaced navigation never restored its culture, every navigation ended on the
     culture of the address it left behind, and a navigation turned into a full load reconciled at open.
   - ✅ Under the `/app` path base with a culture-prefixed base, the transport's address is generated from
     the base, `/app/he/_blazor/negotiate`; after the path base it was `/he/_blazor/negotiate`, after the
     culture middleware `/_blazor/negotiate`, and by then an endpoint had already been chosen, ASP.NET
     Core's 405 for an unsupported method, which answered the request. With the plain base the transport's
     address didn't carry a culture, nothing had been chosen by that point, and the circuit connected. Why
     the endpoint was chosen that early isn't established; the circuit takes its culture from its address
     as it opens, so the transport doesn't need a culture of its own.
   - ✅ Two culture cookies at `/` and at `/app` are both kept, and under `/app` the browser sends the one at
     `/app` first while ASP.NET Core reads the last, the one at `/`. Script sees only a cookie it can write,
     can replace the one at `/app`, and can't replace one written `HttpOnly` at `/`; the server deleted that
     one by naming its path. The middleware writes at `/` whatever the path base, so under `/app` its
     cookie is the one the server reads.

   A last round tried the page base and the handoff, on the same host and runtime:
   - ✅ A page base that validates its `Culture` parameter in `SetParametersAsync` and calls
     `NavigationManager.NotFound()` for a segment that isn't a culture's token stops the derived page
     before its lifecycle: for `/xx/tabout` the page's `OnInitialized` never ran. With only the `Router`'s
     `NotFound` fragment, an interactive navigation to `/xx/tabout` left the previous page shown, and a
     full request answered 404 with an empty body. With the `Router`'s `NotFoundPage` naming a page, the
     full request answered 404 with that page, the interactive navigation rendered it in the same circuit,
     and `/he/tabout` still answered 200.
   - ✅ A synchronizer that holds the document's pair from an enhanced navigation's start until the circuit
     acknowledges reconciling the state for that navigation, the one landed last, kept the attributes in
     agreement with the page under islands. The latency delayed the document's responses, while the
     circuit's messages ran at local speed, the landing's report reaching the server within 2 ms. A link
     to `/he/about` from English kept `en/ltr` over the English page until the patch wrote `he/rtl` with
     the Hebrew page at 427 ms, and the pair never went back; the surviving island's state readout showed
     `en` for a further 16 ms, until the circuit reconciled and rendered it. A link within Hebrew, Back,
     two navigations 30 ms apart and a navigation another replaced after 150 ms under 700 ms of latency
     kept the attributes and the content in agreement throughout; every landed navigation was
     acknowledged, the same-culture ones included, and a replaced navigation was never applied. With the
     acknowledgement sent 300 ms late and an update sent into each wait, the synchronizer held each update
     off until the acknowledgement; on the link to `/he/about` that update was a stale `en/ltr`, the
     attributes kept `he/rtl`, and the island lagged 18 ms.
   - ✅ `UsePathBase` chooses an endpoint as it removes the path base: for `/he/_blazor/negotiate`, whose
     path matched a route only for another method, it chose the 405 before the culture middleware removed
     the culture segment, and explicit routing kept it. The middleware now clears an endpoint and route
     values chosen for an address whose culture segment it removes, and the circuit connected under `/app`
     with the culture base.

   The step is built in parts, as the design's culture addresses set them out, each reviewed and committed
   on its own:
   1. ✅ **The cookie.** The middleware stores the route in `forma-culture-route` at the path base, its
      name carrying the base, which script can write, reads the earlier `forma-culture` only where the new
      one is absent, and expires it at `/` on a document's response. In Chrome, at `/` and under `/app`,
      an installation's `HttpOnly` cookie naming Hebrew was replaced on loading `/about` by the new cookie
      naming English at the path base, script saw only the new one, and the circuit opened in English;
      written again beside the new one, it was expired on the next document, and the new one took that
      document's culture. Under `/app`, beside another application's cookie at `/` naming English, a
      framework request took the Hebrew of its own.
   2. ✅ **Reconciliation and the switcher.** The circuit reconciles its state with the address as it
      opens and as it resumes, before the document culture synchronizer starts; the switcher requests
      another culture's address, and its current route is the state's, which replaces the router's
      current route read from the ambient culture. In Chrome, under islands and under global
      interactivity, a tab at `/about` paused and resumed into `en/ltr` with its state in English after a
      second tab had loaded `/he/about` and written the cookie in Hebrew. The switcher's switch from
      `/about` loaded `/he/about` as a new document whose circuit opened in Hebrew; under global
      interactivity the page there reached not-found, as the plain base leaves any full load of another
      culture's address, which the culture base of part 3 settles. The Showcase's language links marked
      the page's culture and its page links stayed in it, in Hebrew, in English and after Back.
   3. ✅ **The document's base.** Forma supplies the base in the address's culture, so a switch is a full
      load by default. Links are settled with it: `GetPath` and `GetUrl` give a path from the
      application's root, `/he/about`, which an `href` resolves from the host's root, so under a path base
      it misses the base, and the switcher's address and the reconciliation read the address relative to
      the document's base, which a culture base changes. A component writes the base, and the switcher
      reads the address and builds a link from the application's root, the base without its culture
      segment.

      In Chrome, under `ExceptDefault` with the culture base, the default culture's base `/` holds
      `/he/about`, and under global interactivity a link to it from `/about`, plain or with enhanced
      navigation turned off, loaded a new document, since interactive routing found no page there; so did
      Back. Reading the address relative to the base, that circuit reconciled to English at `/he/about`;
      read from the application's root, it opened in Hebrew. Under islands a plain link was an enhanced
      navigation, which the state follows from the handoff on. The switcher's switch to Hebrew and back
      loaded `/he/about` and `/about` in their cultures under islands with the plain base, under global
      interactivity with the culture base, and under both with the culture base and the `/app` path base,
      `/app/he/` the base of `/app/he/about`, under `ExceptDefault` and under `Always`. A tab at
      `/he/about`, its base `/he/`, paused and resumed into Hebrew after another tab had written the
      cookie in English, under islands and under global interactivity.
   4. ✅ **The handoff.** The document culture synchronizer holds the document through an enhanced
      navigation until the circuit acknowledges reconciling the navigation that landed last. Gate, beside
      the step's: a landing within one culture acknowledged though the state announces no change, an
      in-place change while a report is pending, and the navigation's end after its landing but before
      the reply, each in the tests, and the first and the last in Chrome under islands, with and without
      a path base.

      In Chrome under islands with the plain base, at `/` and under `/app`, with 400 ms of latency on the
      document's responses: a link from `/about` to `/he/about` kept `en/ltr` until the patch wrote
      `he/rtl`, and the synchronizer wrote nothing; the surviving island read `en` for a further 17 ms
      until its circuit handled the address. A link within Hebrew, Back, two navigations 30 ms apart and
      a navigation another replaced after 150 ms under 700 ms agreed throughout. Every navigation ended
      after its landing and before its reply. An in-place switch to English 100 ms into a link to
      `/he/other` rendered the island in English while the document kept `he/rtl`, and the landing set
      the state back to Hebrew. After a reply, an in-place switch wrote `en/ltr` and then `he/rtl` at
      once. A navigation to a page without islands kept `he/rtl` as its circuit closed, and a tab that
      followed a link to `/about` after a second tab had written the cookie in Hebrew stayed English. An
      in-place change while a report waits, a circuit closing while one waits, and a refused report's
      retry were covered by the tests alone, since the circuit's messages ran at local speed and replied
      within milliseconds.
   5. ✅ **Culture-bearing interactive routes.** The host's declaration, `CultureRouterOptions.Switch` set to
      `CultureSwitchMode.InPlace`; the page base, `CultureRoutePage`, following its incoming route;
      `CultureRouteScope` around the host's `Router`, marking its render tree; the plain base; the culture
      state's contract, its change made before its task returns and an unchanged pair applied to the calling
      flow; and the cookie written from script by `Forma.Framework.Culture.Router.Interop`. Gate, beside the
      step's: in the tests, an unchanged pair restoring a calling flow's stale cultures without notifying or
      advancing the revision, and an interactive page outside `CultureRouteScope`, as an island renders,
      leaving the state to the handoff; in Chrome under global interactivity, with and without `/app`, a
      switch in place keeping the user's work, a reused page from `/he/about` to `/about` clearing its
      token, a held subscriber delaying the reconciliation's renders, rapid Back and Forward, a not-found
      segment and the canonical replacements.

      The rule the design first gave, the state following the circuit's location change, was replaced by
      the page following its route, on probes run on the same host under .NET 11 release candidate 1 and
      repeated under release candidate 3:
      - ✅ A circuit's location change can't tell interactive routing from an enhanced navigation:
        `IsNavigationIntercepted` was true for a link and false for `NavigateTo` under islands and under
        global interactivity alike, and false for Back and Forward under islands and true under global
        interactivity; under islands the location change reached the circuit before the client's report
        that the navigation started.
      - ✅ An interactive page can't tell from its renderer or its assigned render mode whether a router
        rendered it: `RendererInfo` was `Server` and interactive, and `AssignedRenderMode`
        `InteractiveServerRenderMode`, both for a page under the interactive router and for a page that is
        an island of its own. A value cascaded around the `Router` inside `Routes` reached the first on
        every delivery, reached each one's static render, and never reached the island's interactive
        instance. No `RouteData` is cascaded.
      - ✅ The route value arrives under the template's spelling, `culture`, which a case-sensitive lookup
        of `Culture` among the parameters misses; a page reused from `/he/rabout` to `/rabout` received
        `culture` as null, its earlier value `he`; an island page under islands received no culture at
        `/he/risland`, since the middleware removed the segment; and a prerender under global
        interactivity is a static delivery.

      In Chrome under global interactivity with `ExceptDefault`, at `/` and under `/app`, every
      navigation stayed in the first document and kept the draft typed in the layout. The switcher's
      switch from `/about` took `/he/about` with `he/rtl`, the reused page's parameter callback running in
      `he` in the state and the calling flow, the layout's readout `he`, and the cookie `he` at the path
      base under its name there, `forma-culture-route%2Fapp` under `/app`. A link from `/he/about` to
      `/about` reused the page with its token cleared and took `en/ltr`, and Back and Forward each took
      their address's culture. Back, Back and Forward 40 ms apart, and Back, Forward and Back 20 ms apart,
      each ended on the culture of the address they left. `/xx/about` rendered the not-found page in the
      same document, `/iw/about` and `/HE/other` were replaced by `/he/about` and `/he/other`, and
      `/en/about` by `/about`. A reload of `/he/about` loaded in Hebrew, and the cookie was `SameSite=Lax`
      at the path base. With a subscriber holding each notification 800 ms, the page rendered in Hebrew
      and the document took `he/rtl` within 150 ms of the switch, and the layout, notified after that
      subscriber, kept `en` until 1200 ms.
8. ✅ **The edit in progress.** An uncommitted edit keeps its parsing culture until it commits or is
   replaced, as the design's editing section sets out. The fields that take typed text are the number
   input, the currency input through it, the time picker and the date-time picker's time; the date
   pickers take a date from a calendar alone. Until now a field held typed text only after a rejected
   commit, and every parse read the current culture.
   - One shared draft holds an edit's text and the culture it is read in, taken from the field's input
     events without changing the bound value or validating; each field keeps its own commit rules. An
     input event reaches the server for each change to the text, and typing doesn't wait for it.
   - The number input steps from its value as it did: a step that succeeds replaces the edit and shows
     its result in the current culture. Stepping from the number the edit's text writes is a decision of
     its own, left out of this step.
   - The date-time picker tells a missing time, an entered but unreadable one and a read one apart,
     blank text counting as missing, and reading the time ends nothing: its edit ends only when the
     whole field commits, or is replaced.
   - Escape still closes the time list and leaves the edit in progress.

   Gate, in the component tests, for the number, currency, time and date-time fields, each with a sample
   the first culture reads and the second refuses or reads otherwise, `1,234` under English and German,
   `1:30 אחה״צ` under Hebrew and English, and `13.30` under Finnish and English: typing, switching the
   culture with the edit uncommitted, finding the exact text kept, then committing and finding the first
   culture's meaning; a second switch keeping the first culture; a rejection keeping the edit; a commit
   to the value already held ending the edit and formatting it again; a parent's render with the bound
   value unchanged keeping the edit, and a changed value replacing it; a culture-only render keeping a
   clean field clean and a modified one modified. In Chrome, with the in-place switch: typing into a
   number and a time, then Back changing the culture with the same input still focused and nothing
   committed before the change, the text kept, and a commit taking the first culture's meaning.

   The common times read alike in English, German, Hebrew and French, `1:30 PM` and `13:30` among them,
   so the time samples are Hebrew's own designators and Finnish's separator. Each culture test failed
   with the field reading in the current culture, each test of the text kept failed without the input
   events, and the tests of blank text and of a different bound value failed with those rules removed.

   In Chrome under global interactivity, on a host switching in place between English, Hebrew and German,
   every step stayed in the first document. `1,234` typed into a number at `/edit`, under English, was
   kept through Back to `/de/edit` with the number still focused and no commit, and Tab committed 1234,
   which German would read as 1.234, and showed `1234`. `1:30 אחה״צ` typed into a time at `/he/edit` was
   kept through Back to `/de/edit` the same way, and Tab committed 13:30, from a text German refuses, and
   showed `13:30`.
9. ✅ **The Showcase.** The Showcase switches language in place, keeping the user's work, with the new load
   still supported; the README, the culture READMEs and the localization and authoring rules follow.
   Gate, in a browser: switching between English and Hebrew in place lays the page out right to left and
   keeps it there on the next click, an edit in progress keeps its text and its parsing culture, and a
   popover already open takes the new direction.

   The Showcase's pages are islands under a static router, so it switches in place through enhanced
   navigation, as the design's islands case sets out, rather than through culture-bearing interactive
   routes: `CultureSwitchMode.InPlace`, its language links no longer forcing a new document, its base
   already the application's root. Three things the switch needed of the Showcase itself:
   - Its pages follow the culture state through a shared base the pages' folder applies, which renders a
     page again on each change and gives it strings and cultures read from the state, so a render that
     work begun before the switch asks for is in the new language too. A label a page built once, the
     data view's sort labels among them, is built again on each change, keeping the reader's choices;
     text an event produced keeps its language, as the contract excepts. Its nav takes the same base, and
     its sidebar, which outlives a navigation, subscribes itself; before, a page kept its text, and the
     sidebar its links, in the culture the switch left, so the next page link led back to it.
   - The reader's place is returned to after an enhanced navigation too, and held while the page's
     islands render again in the new language, until the reader moves, the page settles or the next
     navigation begins; the hold belongs to the address it began at.
   - `FocusOnNavigate` still moves focus to the heading on every navigation, so a navigation may move
     focus off a field and attempt a commit. An accepted edit keeps its meaning and is shown in the
     current culture, and a rejected one keeps its text and its parsing culture, whichever side of the
     change the commit reaches, as step 8 guarantees.

   In Chrome on the Showcase, in one document throughout: `1,234` typed into a number under English,
   then the Hebrew language link, committed 1234 and took `he/rtl`, the page's text, the sidebar's current
   language and its links to `/he/...`; the layout stayed right to left through two later clicks, and the
   next page link led to `/he/text-box`. `1,234` typed under English and carried to `/fr/number-input` by
   Forward committed 1234, where French reads 1.234. A drop-down menu left open through a link to
   `/he/drop-down-menu` stayed open, took `rtl`, anchored itself to the trigger's other edge and showed
   its items in Hebrew. A language link from the fourth section kept the reader one pixel into the same
   section in Hebrew. On the step 8 host, with the server holding each event 400 ms, a number and a time
   typed at 25 ms a key kept every key and committed their meaning; Chrome's emulated latency doesn't
   reach the circuit's WebSocket, so the check delays the circuit's inbound events instead.

   A review found that work begun before the switch rendered in the language it began in, that text a
   page built once kept its language, and that a held place followed the reader onto the next page. In
   Chrome, against the Showcase before and after the fixes: a save begun in English and finished after
   the switch to Hebrew read "Saved." before and "נשמר." after; a held place, followed by a link opened
   from script or by going back two entries, put the next page 705 and 729 pixels down before and at its
   top after. After the fixes the data view sorted by Price kept the sort and named it "מחיר"; an action
   recorded under English read "פעולה אחרונה: View"; a validation message shown in English kept its
   English, and the next submit produced it in Hebrew; and the localization page's French column stayed
   French.
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
