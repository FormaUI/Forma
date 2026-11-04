# Forma Culture Design

A Forma host decides the culture an application runs in, and may change it while the application runs,
with a new load or in place; every component follows it, in every runtime on the page, and so do the
document and the address. This design states the contract, the culture state each user scope holds, the
switch that changes it for the whole page, how a render, an edit, the document and a culture address
follow it, and where each part lives. The locale files themselves, their paths, packs and precedence, are
the [localization design](localization-design.md)'s, and what WebAssembly asks of the platform is the
[WebAssembly design](webassembly-design.md)'s.

## Purpose

Forma is a library, so it can't decide how its host switches language. A host may switch with a new
load, which renders every component fresh in the new culture, as the platform documents
([Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization)).
A host may also switch in place: Blazor Hybrid, in MAUI, WPF or WinForms, has no reload at all, and an
application may refuse to reload because a reload loses form input, open dialogs, scroll position and a
workflow under way. Both are a host's to choose, and a component is correct under either.

A culture changed in place has to reach everything a new load would have rebuilt: the culture the
runtime runs each event and each render in, every component's strings, formats and direction, the text
a component computed and kept, the document's language and direction, and, with culture addresses, the
address. A trigger that makes components render again reaches none of the rest, which is why the design
holds the culture in one state and has each of those follow it.

## The contract

Any Forma component shows the culture in force. A host may change the culture at any time, with a new
load or in place, and every component follows: its strings, its formats, its direction, and the
document's language and direction. The host tells Forma the new culture through one entry point, the
[switch](#one-switch-for-the-page), and never sets `CultureInfo` itself or wires a component, and a host
that switches with a new load needs no additional setup.

The contract covers the render modes Forma supports: static server rendering, Interactive Server,
Interactive WebAssembly and Interactive Auto, in a Web App and in a standalone WebAssembly app, and Blazor
Hybrid, whose application holds one culture across its web views, as [its section](#blazor-hybrid) sets out.

Two kinds of text are exceptions, stated as such: text an event produced, an announcement or a
validation message, stays in the language it was produced in until the next such event produces it
again; and an edit in progress keeps the culture it began in, as [editing](#an-edit-in-progress-keeps-its-culture)
sets out.

## One culture state per user scope

A user scope is a circuit, a WebAssembly runtime, or a Blazor Hybrid application, whose web views share
it, and each holds one culture state: the culture, which formats dates, numbers and collation, and the UI
culture, which chooses strings and direction, kept apart as the platform keeps them so that a host can
show English text with German formats. The state starts from the culture in force when the scope is
created, the request's on the server, the one the runtime starts in under WebAssembly, and the
application's as it first resolves the state under Blazor Hybrid, and announces each change through
`INotifyStateChanged`, as every Forma
service a component follows does. It changes only as its runtime's integration applies a pair
[the switch](#one-switch-for-the-page) accepted, with that pair's version, and a component only reads it.

```csharp
public interface ICultureState : INotifyStateChanged
{
    CultureInfo Culture { get; }
    CultureInfo UICulture { get; }
    bool IsRightToLeft { get; }
    int Revision { get; }
    CultureVersion Version { get; }
}
```

The integration applies through a contract of its own, which the default state implements beside the one
every component reads, so a component has no way to change the culture but the switch:

```csharp
public interface ICultureStateIntegration
{
    Task ApplyAsync(AcceptedCulture accepted);
}
```

Its rules:

- `IsRightToLeft` is whether the UI culture is written right to left.
- A pair whose version the state has applied already, or an older one from the same document, changes
  nothing, so a late delivery of an earlier switch never takes the state back. A version from another
  document is newer whatever its number, as [the coordinator](#the-coordinator) sets out.
- A newer version of the pair already held isn't a change: the state records the version, neither notifies
  nor advances the revision, and still applies the pair to its calling flow, which may hold other
  cultures. A culture is the one held when it has the same name and comparison, the same calendar with its
  eras and two-digit-year cutoff, and reads and writes dates, times and numbers alike, so a culture of the
  same name that pins its own patterns, as a host's 12- or 24-hour clock does, is a change.
- The state holds read-only cultures, `CultureInfo.ReadOnly` of what it is given, so no caller can alter
  a culture behind it.
- The revision is the state's, advanced once per change, and the one source every component base and
  render gate reads. It advances after the change's cultures are in place, so a reader that reads the
  revision before the cultures never pairs it with cultures older than it; the state may be read from
  several threads while it changes, and a change is applied from one flow at a time.
- `ApplyAsync` changes the state before it returns its task, which completes once every subscriber has
  been notified: each component's notification dispatches its render through its renderer, so a
  completed application means the state has changed and its renders are scheduled, not that the browser
  shows them.
- `ApplyAsync` applies the new cultures to its calling flow before it advances the revision and notifies,
  so the renders it schedules inherit them.
- An application takes no cancellation: once the state has changed, every subscriber is notified, so no
  component is left showing the culture before it.
- Applying is the runtime integration's work, its registration with the coordinator and the switches
  that run without it. A host changes the culture through the switch, since a pair applied to one state
  alone is the disagreement the switch exists to prevent, and an argument named accepted doesn't make a
  pair one the coordinator accepted: the version's document and number are what the state checks.

Components depend on the abstraction, and the library registers its default, `DefaultCultureState`, one
per user scope with `TryAddScoped`, or once for the application under Blazor Hybrid, so every component
resolves a state and a host that registers its own first still wins. The integration is resolved from that same instance, never from a default of its own, so
applying always reaches the exact state every component reads. A replacement that doesn't implement the
integration is rejected with `InvalidOperationException` as the state is first resolved in its scope,
before its runtime registers with the coordinator, or as it is registered where the registration is an
instance; no startup check builds a scoped state in the root
provider to look. The state knows nothing of navigation, addresses, cookies or the document; the switch
does.

### The application's cultures

An application can write dates, numbers or times with patterns of its own in a culture that keeps its
platform name, as a company's 12- or 24-hour clock does. A pair crosses the page by its names, to the
coordinator and back, in the marker and in a culture address, and a name alone stands for the platform's
culture, so the application tells Forma its culture for each name through a provider of its own:

```csharp
public interface ICultureProvider
{
    CultureInfo GetCulture(string name);
}
```

- Where one is registered, every culture Forma holds or applies is the provider's for its name: the
  state's start and every pair it applies, whichever switch accepted the pair, each pair the coordinator
  delivers by name, and a request's cultures where culture routing sets them. Without one, a name the
  coordinator delivers stands for the platform's culture, and a culture a switch is given is held as it
  is.
- The provider returns a culture carrying the name it is asked for, since the document's language, the
  culture addresses and the coordinator name a culture by its name. A culture under another name is
  rejected with `InvalidOperationException` and changes nothing, and a delivery the provider fails is
  logged, since the coordinator hands that failure to the switch that requested the pair alone.
- The provider takes a lifetime the state reading it can resolve: on a server, scoped where its cultures
  differ by user scope; under WebAssembly, whose runtime is one user scope, and for a Blazor Hybrid
  application's culture, whose state is the application's, a singleton.
- A request's cultures are its pipeline's to choose. Culture routing chooses the provider's. ASP.NET
  Core's request localization takes its cultures from lists fixed at startup, so a host choosing the
  request's cultures through it adds the request step after it, `UseFormaRequestCulture`, which applies
  the provider's cultures for the names it chose to the request's ambient cultures and its request culture
  feature, so the host's own code reads them in a prerender as Forma's output does, which reads the
  state.

## One switch for the page

A page can hold more than one runtime, islands of Interactive Server beside islands of WebAssembly, or
Auto's two, each with its own culture state. The application has one culture pair across all of them: a
switch is the application's, never one runtime's, and every runtime on the page converges on the pair it
accepts, since a document that is right while another runtime's components still show the previous
culture isn't an answer. Content localized on purpose, such as a preview in another language, carries its
own explicit culture and isn't a second authority.

```csharp
public interface ICultureSwitch
{
    Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null);
}
```

The switch is the one entry point a host calls, in any runtime, and an omitted UI culture is the culture.
Requesting a switch and applying one are kept apart: the request goes through the switch, and each
runtime's integration applies the pair the switch accepted to its state, locally, requesting nothing, so an
application never loops back into a request, and applying never navigates, writes a cookie or calls script.

### The coordinator

The document's culture coordinator orders every switch on the page. It is a service of the document, one
per page, as the input router is, and each runtime's culture state registers with it. A runtime requests a
switch from it; the coordinator accepts requests one at a time, in the order they reach it, gives each
accepted switch the next number in the document's sequence, and sends the pair with its version to every
registration. A culture version is the document's identity with that number, so it orders the switches of
one document and marks where a new document begins:

- **Within one document,** a registration ignores a version its state has applied already or an older
  one, so the order every runtime applies switches in is the coordinator's.
- **A new document,** each full load, starts a new identity and a new sequence. A registration presenting
  a version from another document, a participant that outlived its document or reconnected across one,
  adopts the coordinator's current pair whatever its number.
- **Registering and reconnecting are not switches.** A registration, a circuit reconnecting or a
  WebAssembly runtime starting, receives the accepted pair with its version and applies it, and never
  replaces the coordinator's pair with the pair its own startup chose. Registration never stands in for
  a handoff its host disabled, as the [WebAssembly design](webassembly-design.md) sets out, and never
  reads the server's marker again.
- **The first pair** is what the page started in, number zero:
  - A prerendered page's is the pair the server resolved for the request and rendered in, which
    `FormaPageHead` publishes in the document's head as its culture marker, both cultures named, beside
    the language and direction the server serves. The coordinator reads it once, as the document loads.
    The marker names each culture on its own, and names the invariant culture, which a server can render
    in, by the empty name; a marker without either name, or with a name that isn't a language tag, names
    an invalid culture. A coordinated prerendered page whose marker is missing or names an invalid culture
    fails the coordinator's initialization with an error naming `FormaPageHead` and the coordinated prerendered
    arrangement that requires it, and never falls back to the first runtime that registers.
  - A standalone WebAssembly app has neither a server nor a marker: its startup initializes the
    coordinator with the pair and the switch's policy, and its runtime's registration adopts the pair as
    any registration does. Its policy is the state alone or the cookie, since without a server nothing gives
    its document a base in each culture an address carries.
- **The marker describes what the server rendered,** never a second copy of the coordinator's pair.
  Registration never reads it, and after the first load it is read only as part of the landing it came
  with, as [landings](#landings) set out, so a head update alone, a delayed response's or a stale one,
  never undoes a newer switch.
- **The accepted pair outlives its registrations.** The coordinator keeps the last accepted pair and its
  version for the document's lifetime, with no registration live as with many. Releasing a registration
  removes a subscriber and changes no culture, so a gap between registrations resets nothing, and a
  runtime registering later adopts the retained pair. Only an accepted switch or an accepted landing
  changes it.

The coordinator owns ordering and distribution. Whether a switch persists and whether it navigates are its
[policy's](#switch-policies), and the coordinator accepts a switch once the policy has done what acceptance
needs.

### Landings

A navigation that lands in the document changes the page's culture through one landing record, the
navigation's identity with the pair resolved for it, which the coordinator accepts as the next switch,
unless it is the accepted pair already, without waiting for any registration. A navigation reaches its
record by one of two paths, according to how it lands:

- **A server-rendered landing,** an enhanced navigation's, takes its pair from the marker of that
  navigation's own response, which the server resolved for the request by the one resolution
  [culture addresses](#culture-addresses) states. The landing is the first page update after the
  navigation's start, so the marker it brings is the navigation's own.
- **An interactive landing,** which has no server response, takes its pair from the address the router
  accepted. A router renders a destination only once its navigation completes uncancelled, and a culture
  route page receives its route only from that render, so the page reports the address it was routed
  to, and the navigation's identity is its place among the reports its runtime sends, in the order the
  runtime renders them. The coordinator resolves its pair through the culture-route mapping the page
  publishes, which the browser and the server read by the same rules. A navigation a newer one
  cancelled never renders its page, so it reports nothing; neither a location change, which a
  navigation announces before its router starts and a superseded one announces too, nor a render of
  the router, which a parent's render causes as well, marks a landing. The previous landing's marker is
  never read for it, and interactive routing without culture routes records nothing.

A navigation that is cancelled, fails or is replaced by another records nothing, and a record from a
navigation another replaced is obsolete and accepts nothing.

### What awaiting a switch guarantees

A switch's task completes successfully once the switch was accepted, the persistence or navigation its
policy requires has completed, and the runtime that requested it has applied the accepted pair or a newer
one. The other registrations receive the pair and converge on it; one that is unavailable doesn't hold the
switch back, and adopts the current pair when it registers again. One accepted pair is not one
simultaneous render: each runtime renders the change as its own notifications reach its components.

A switch that can't be accepted fails and changes nothing:

- **Without coordination.** A runtime's registration is registering, registered or unavailable. A switch
  requested while it is registering awaits that attempt, which completes on its own whether or not a
  switch waits on it, and the attempt's failure, the registration's disposal or a disconnection fails the
  waiting switch. A runtime with no registration under way, or one unavailable, a disconnected circuit
  among them, fails the switch at once with `InvalidOperationException`: nothing waits for a registration
  nobody started, and no switch applies the pair locally in its place.
- **A pair without a route.** Under the address policy the destination is built before anything changes,
  so a pair without a culture route throws `InvalidOperationException` naming the pair and saying
  that no configured culture route represents it, and leaves the culture, the cookie, the document and
  the location as they were.
- **Persistence that fails.** A cookie the coordinator can't read back fails the switch before it is
  accepted.
- **A navigation that doesn't land.** An address switch whose navigation is cancelled, fails or is
  replaced by another is never accepted, and its task completes as canceled; the accepted pair stays the
  one the address that remained active names.
- **In a static render** there is no page to coordinate, so a switch throws `InvalidOperationException`;
  a static page switches with a request.

A switch that loads a new document doesn't complete in the document it began in, and nothing in it waits
for that: the new document starts its own sequence from the pair the server resolves, and a switch a
runtime outliving the old document still awaits fails as its registration is released. A switch takes no
cancellation: once accepted, every registration receives it.

### Switch policies

How a switch persists is the host's choice, made once where it registers the culture:

- **The cookie.** Where a cookie chooses the culture, as request localization's cookie provider does, the
  coordinator writes the cookie and reads it back before it accepts the switch, in one step, so the
  cookie names the last accepted pair; a cookie it can't read back fails the switch, and the requested
  pair is never published as accepted. Reading it back shows the cookie observable at that moment, and
  the next request resolving the pair is what the policy promises.
- **The address.** With culture addresses, a switch is a navigation to the other culture's address, and a
  navigation requested isn't a destination accepted: the coordinator accepts the landing's record, as
  [landings](#landings) set out, so the router and the coordinator share one acceptance path and neither
  commits a culture of its own.
- **The state alone.** A switch the host chose not to persist is accepted at once, and promises nothing
  beyond the page: the next server render, a navigation's or a reload's, is in the request's culture.

The policy is configuration, published apart from any pair: `FormaPageHead` publishes it as the document
loads, beside the initial marker, and a standalone app's startup supplies the same, so adopting a pair or
accepting a landing never changes it. The cookie policy publishes everything writing its cookie takes, the
name, path, domain, lifetime, `Secure` and `SameSite`, and writes the one serialization the server's reader
takes, request localization's `c=…|uic=…`; a cookie the policy writes can't be `HttpOnly`. The address
policy publishes the culture-route mapping, from which it builds a destination and resolves an interactive
landing, with the culture-route cookie's settings.

A switch requested while a navigation is in flight waits for the navigation to land or end, so a landing
never undoes it:

- a navigation that is cancelled or fails ends the wait without accepting its destination's pair;
- a landing is accepted only from its own record, naming what the browser landed at rather than what was
  requested, and an obsolete record accepts nothing;
- a switch that waited is then accepted against the address that landed, so an address switch navigates
  from the page the browser shows;
- a document replaced meanwhile fails what waited in it, as a switch that loads a new document does.

Nothing promises that the cookie, the address, the document and every runtime change in one atomic step;
each failure above leaves the accepted pair where it was, and the document and every registration
converge on it.

### Switches without the coordinator

Prerendering, a test and a host's deliberate choice each have their own switch, and none is inferred from
a failure:

- **Initialization isn't a switch.** A prerender's state starts from the request's resolved culture, and
  a standalone app's runtime from the pair its startup configures; neither goes through the switch, and
  nothing switches during a prerender.
- **A test** supplies a test switch, which accepts, fails or holds a switch as the test directs.
- **A host that wants one runtime switched alone** registers the local switch, which accepts at once in
  a sequence of its own and changes that runtime's culture state alone. It never writes the document's
  `lang` or `dir`, never reaches the page's coordinator, and never writes a culture address or the
  culture cookie, so it isn't a competing authority for the page, and it promises nothing across runtimes,
  the document or a navigation. Under WebAssembly its applications set the process's default cultures as
  every application there does, which reaches no further than its own runtime. It is a hosting choice,
  never a fallback from an interop failure.

## Output takes its culture from the state

A render can begin in three ways: the application of a switch, an event from the browser, and work
begun before a switch, a task started in the old culture that finishes after it and asks for a render
through `InvokeAsync(StateHasChanged)` or a parameter it sets. Whatever began it, a component's output is
in the state's cultures, because the component reads them from the state rather than from the ambient
culture of the flow that rendered it.

A component's context carries the state as `Context.CultureState`, and a component formats, parses,
compares, sorts and lays out by its cultures and its direction.

```razor
<span>@date.ToString("d", Context.CultureState.Culture)</span>
```

An edit in progress parses in the culture it retained, as [editing](#an-edit-in-progress-keeps-its-culture)
sets out.

Strings follow the same rule. The localization layer looks a string up in a UI culture it is given, and a
component's strings are looked up in the state's UI culture, directly or through a scoped locale that takes
it from the state. The file cache stays shared and keyed by culture, so no singleton depends on one user's
state.

An operation that reads the ambient culture and takes no culture of its own, such as
`DisplayAttribute.GetName()` or a callback a host supplies, runs in a bounded synchronous scope: it saves
`CurrentCulture` and `CurrentUICulture`, applies the cultures the output is for, runs the operation, and
restores both, whether it returns or throws. The scope surrounds the operation alone; it spans no asynchronous work and
never assumes that a parent's render renders its children within it.

The ambient culture is kept in step too, for whatever reads it outside Forma's output. The circuit handler
sets both cultures from the state before every inbound activity, every UI event and every call from script
into .NET, through the circuit's inbound activity handler
([circuit activity](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr)), and `ApplyAsync`
applies the new cultures to its calling flow before it notifies. Under WebAssembly, one runtime is one user
scope, so every application of a pair, the local switch's included, also sets the runtime's current
cultures and the process's default cultures, `DefaultThreadCurrentCulture` and
`DefaultThreadCurrentUICulture`, which each later event starts from; that is the WebAssembly integration's
work, and a Server runtime never sets the defaults, which every circuit in its process shares. Work begun before a switch finishes in the culture it
captured, which is the platform's rule, a default changed after it included, while the render it asks for
reads the state.

Components read the culture explicitly because the bases can't wrap a render. `ComponentBase` keeps its
render delegate private, that delegate calls the component's own `BuildRenderTree`, which a component
written in Razor overrides without calling a base's, and `ShouldRender` decides whether a render is queued
rather than surrounding it, and never sees the first one. A culture scope written around a component's
markup would have to be authored in each component, and a fragment it creates may run after the scope has
ended, so the explicit read is the one approach that holds for every component.

## Components follow the state

Both component bases, Forma's component base and its input base, subscribe to the state as a component
initializes, render again on each change and unsubscribe as it is disposed, so every component follows
with no host wiring, at the cost of one subscription per live component; a virtualized collection keeps
only its visible items alive.

A component that gates its renders counts the state's revision among what it compares, so a row that
skips a render for unchanged data still renders for a changed culture.

One rule covers every component's output: its strings, formats and direction come from the culture in
force as it renders, and nothing that depends on the culture outlives a render unless it is keyed by the
culture.

A host's own components follow by subscribing to the same state, and text they pass down, such as a
tab's title, reaches Forma's components through their parameters.

## An edit in progress keeps its culture

An edit in progress is text typed into a field and not yet committed. It keeps the culture in force when
the typing began until it is committed or replaced, and the field follows the current culture afterward,
so `1,234` typed under English as a thousand and more commits as that number under German, which would
read it as one and a fraction:

- An edit begins with the first change to the field's text, and the field holds its text through every
  render, a change of culture among them, without changing the bound value or validating it.
- An edit that is rejected keeps its text and its parsing culture, whether its text can't be read or the
  value it reads lies outside the range, off the step or past the decimal places the field takes.
- An edit ends when it commits, even to the value the field already holds, or when it is replaced: by a
  value the field steps to, a suggestion the reader chooses, or a different value bound to the field. The
  same value bound again leaves it in progress.
- A committed value may be formatted again in the new culture, without changing the value or the field's
  modified state, which stays clean or modified as it was.
- Focus alone isn't an edit in progress; a field holding no uncommitted text follows a change at once.
- Only the parsing culture is kept: the field's labels, accessible names, layout and direction follow the
  new UI culture, and a number's own text reads left to right in every culture.
- A field of two parts, a date with its time, holds the edit of its typed part until the whole field
  commits. That part is missing, entered but unreadable, or read; blank text is missing, and a part read
  while the field still waits for the other, or after the field rejected its value, keeps its edit.

## The document follows the state

In a Blazor Web App the `<html>` element belongs to the static `App` component, which no interactive
component renders. The host's static render sets the document's initial language and direction, and
[the coordinator](#the-coordinator) keeps them on the accepted pair from then on: `lang` is the accepted UI
culture's name, and `dir` its direction.

- **The accepted pair.** The pair the coordinator accepted last determines the document's language and
  direction, whichever runtime requested it and whether or not a registration is live. Registering again
  under a live registration's identity recovers that registration, and a call from a registration since
  released changes nothing.
- **What the server served.** The document keeps the pair the server last rendered: the attributes as the
  document loaded, and as the first page update after each enhanced navigation leaves them, read before the
  coordinator sets the accepted pair. An enhanced navigation's response patches the whole document,
  `<html>`'s attributes among them, while a streaming update patches only its component's region, so a
  streaming update never changes the served pair. An attribute the server left out is absent from the
  pair. The served pair is tracked for the document's lifetime, whether or not anything is registered, and
  gives the direction of the pairs the server rendered, the page's first pair and each landing's, since
  the marker names their cultures alone.
- **After a page update.** A streaming update can undo the accepted pair, so the coordinator sets it
  again after each.
- **During an enhanced navigation.** From an enhanced navigation's start the document is held: the
  accepted pair stays until the response patches the document, the patch's pair stands, and a switch
  requested meanwhile waits, as [the policies](#switch-policies) set out. The first page update after the
  start is the navigation's landing, whose record the coordinator accepts from that response's marker, as
  [landings](#landings) set out, and the acceptance ends the hold; a navigation that ends without landing
  ends it too, leaving the accepted pair. A surviving interactive island renders in the new culture once
  its runtime has applied the accepted pair, a round trip after the patch.
- **When a registration goes.** A circuit's registration is released when the client closes the circuit:
  once no interactive server component remains, after the circuit's inactivity timeout, and before a
  paused circuit resumes. A WebAssembly runtime's is released as the runtime's scope is disposed. A circuit
  ended by a fatal server error is not closed this way, and its registration stays until the page reloads,
  as Blazor's error UI asks. Releasing is cleanup, never a switch: when the last registration goes, the
  document keeps the accepted pair, which the coordinator retains for a runtime that registers later, as
  [the coordinator](#the-coordinator) sets out, and a landing meanwhile is accepted from its record as
  any other is.
- **Applying a change.** Each registration applies the pairs the coordinator sends in the order of their
  versions, ignoring one its state has applied already. A delivery that fails is recovered by registering
  again, which adopts the current pair, and each connection to the browser registers again, so a
  registration the browser released is never taken as live.

## Culture addresses

The address decides a page's culture, and the state follows the address, never the reverse, so an address
and the culture it shows never disagree. A host with culture addresses switches through the switch, as every
host does, or through the switcher, one per user scope beside the application's one `CultureRouter`, which
switches to a route's pair through the switch. Under [the address policy](#switch-policies) a switch refuses a
pair no route carries before it requests anything; the coordinator builds the current page's address in the
route carrying the pair and navigates to it, and accepts the record of the navigation that request becomes,
once it lands. The switcher's current route is the route carrying the culture state's pair, so work reads the
route in force. A host without culture addresses switches under the cookie policy or the state alone.

A culture route names its UI culture, and may name a formatting culture of its own; a route that names
none formats in its UI culture, so `he` alone is `he/he`, and a host that wants German formats under
Hebrew text names `de-DE` on its `he` route. An address thus carries the whole pair, the formatting
culture survives every navigation, and a pair without a route can't be reached by address. One
resolution decides the pair an address names, for a server request, an interactive navigation, Back and
Forward and a landing's record alike, and the address decides over the cookie, which only names the
culture last chosen for a request whose address carries none, as the cookie's rules below set out. The
mapping between addresses and pairs is defined once, in the render-mode-neutral routing assembly, and
published to the page, so the server, a WebAssembly runtime and the coordinator resolve and build
addresses by the same rules, the whole pair included.

How a switch navigates is the host's choice, made once where it configures culture routing:

- **A full load, by default.** Forma supplies the document's base in the address's culture, `/he/`, or
  `/app/he/` under the path base `/app`, so interactive routing finds a page within its culture, and an
  address in another culture loads a new document and circuit: it lies outside the base, or, from the
  default culture's base under `ExceptDefault`, inside it where interactive routing finds no page and
  hands the address to the server. The application's root is the base without its culture segment, so
  a path base doesn't end in a segment that is a culture's token, and Forma reads an address and builds
  a link from that root. An address outside a culture's base but inside the root is read like any other;
  one outside the root, at another origin or outside the path base, belongs to another application and
  leaves the state as it is.
- **In place, when the host declares culture-bearing interactive routes,** `CultureSwitchMode.InPlace`.
  Every page then carries its culture route beside its plain one, `@page "/about"` and
  `@page "/{culture}/about"`, and derives Forma's page base, `CultureRoutePage`; the host's `Routes`
  component places `CultureRouteScope` around its `Router`, within the router's render tree; and the
  document's base is the plain one, so interactive routing reaches another culture's address in the same
  circuit, keeping the user's work. The page base supplies the `Culture` parameter, its validation and the
  page's following of its route; it doesn't declare a route, so each page still declares its own. A
  segment that isn't a culture's token stops the page before its lifecycle with the router's not-found
  outcome: an interactive navigation renders the router's not-found page in the same circuit, which needs
  the `Router`'s `NotFoundPage`, and a full request answers 404 with it. An alias or a token in another
  case is replaced by the canonical address, and so is the default culture's token under
  `ExceptDefault`, and so is a plain address under `Always`.
- **In place, when the host's pages are islands,** `CultureSwitchMode.InPlace` under a static router.
  The document's base is the plain one, so a link to another culture's address is an enhanced
  navigation: the server renders the page in that culture, each island the page already holds stays in
  its circuit with its state, and the circuit's state follows the landing's record. The pages carry
  their plain routes alone, since the middleware removes the culture segment before routing. An island
  keeps the markup it rendered before the switch until it renders again, so a host's own interactive
  components follow as Forma's do: they render again on each change of the state, and read their strings
  and cultures from the state, as [output](#output-takes-its-culture-from-the-state) sets out, so a
  render that work begun before the switch asks for is in the new culture too. Text that presents the
  component's current state, such as the labels of the options it offers, follows the change: a label
  the component builds once it builds again on each change, keeping the reader's choices among it. Text
  an event produced keeps its language, as [the contract](#the-contract) sets out.

The state follows the address at the event that settles each kind of navigation:

- **A full load** is a request: the middleware renders it in its address's pair, which `FormaPageHead`
  publishes as the marker the new document's coordinator starts from, and the circuit adopts the
  accepted pair as it registers, and again as it resumes.
- **An enhanced navigation** is a request too, and the circuit hears its location change as it starts,
  before its response, so the state follows the landing, never the location change: the coordinator
  accepts the landing's record from the marker of the navigation's own response, as
  [landings](#landings) set out, and the circuit applies it as every registration does.
- **Interactive routing** makes no request, and a circuit's location change can't tell it from an
  enhanced navigation's start, so the state follows the page the router renders: under culture-bearing
  interactive routes, a culture route page reports its interactive landing with the address its router
  accepted before its interactive page lifecycle runs, the coordinator resolves and accepts the landing's
  record, and the page runs that lifecycle once its state has applied the accepted pair, so its
  initialization and parameter callbacks run in the route's cultures, in the state and in their calling
  flow. Without them, interactive routing leaves the state as it is.
- **Back and Forward** are navigations of the kind they become, and restore the culture of the address
  they return to.

A navigation resolves by the address it leaves behind:

- one cancelled before the address changes leaves the state and the cookie as they were;
- one that lands sets the state from its final address, a redirect's included;
- an older navigation's late completion never overwrites a newer one, since its pair is accepted before the
  newer one's or not at all;
- a failure after the address changed is resolved by the address the browser shows, never by restoring the
  culture the navigation left.

A culture route page follows its route where it renders interactively within `CultureRouteScope`, whose
cascaded value reaches every page an interactive router renders within it and stops at the boundary of a
page that is an island of its own, as every cascading value stops where the render mode changes. Such an
island receives no culture segment, since the middleware removed it before the static render, and is left to
the server-rendered landing, as every enhanced navigation is; a static render is the middleware's. The
scope is what tells the two apart: neither the renderer nor the assigned render mode does, since both read
the same for a page an interactive router rendered and for a page that is an island of its own. And no
routing component can match an address without its culture segment in the router's place, since Blazor
offers no public way from an address to its route data, so the page's own culture route carries the
segment. The scope's absence raises nothing, since an island never receives it, so in-place following rests on the host
placing the scope: without it no page follows, and a navigation within one culture conceals that.

The page reports the address it was routed to, never the navigation manager's latest address, so a delivery
for another reason changes nothing, and a reused page routed through its plain route reports its plain
address, which names the culture it follows. The page waits for the acceptance before its lifecycle and not
for the notification: once its state has applied the accepted pair, the state and the calling flow hold the
route's cultures, and the layout and other components render again as their notifications reach them, one
subscriber after another, so a subscriber that holds its notification delays those after it and the update
isn't one atomic step. A notification that fails is logged. An interactive landing's acceptance writes the
culture-route cookie before it is accepted, since no request carries it.

A page outside that contract follows nothing, since only a culture route page reports an interactive
landing. A page that carries only its plain route has no page at another culture's address: Blazor's router
loads the address as a new document for a navigation it intercepted, such as a link, which starts from its
own marker, and renders its not-found page for one it didn't, such as `NavigateTo`. A page that declares the
culture route without deriving the base binds the segment unvalidated and leaves the state as it is.

The `forma-culture-route` cookie only names the culture last chosen, for a request whose address carries
none, such as the circuit's own connection. A new tab opens in the culture its address names; under
`ExceptDefault`, `/` is the default culture. So two tabs in two cultures each keep their own: one tab's
switch rewrites the shared cookie, and the other keeps its own document's accepted pair as its circuit
reconnects.

The cookie belongs to the application: it is written at the application's path base, and under a path
base its name carries that base, escaped, `forma-culture-route%2Fapp` under `/app`, so applications sharing
a host, one at `/` and another under `/app`, each read their own. It isn't `HttpOnly`, since a change a
culture route page makes in place, with no request, writes it from the browser's script: the coordinator
writes it as it accepts an interactive landing, from the cookie settings the address policy publishes.

The culture segment is the first segment after the path base, and a request whose culture segment the
middleware removes is routed again, since `UsePathBase` routes a request as it removes the base. The
circuit's transport takes the culture its own address names, which the document's base makes the page's.
A framework request, one under `/_` such as the circuit's negotiation or its disconnect beacon, is told by
its path without the culture segment: it takes the culture its segment names before the cookie's and writes
no cookie, so the old page's disconnect beacon, arriving after a switch, can't set the cookie back.

## WebAssembly

WebAssembly takes part in the contract. Under coordinated pages, the default, a runtime's state registers
with the page's coordinator as a circuit's does, as the runtime starts, and is released as the runtime
ends; a prerendered runtime starts in the pair the server rendered its prerender in, which the server hands
it, as the [WebAssembly design](webassembly-design.md) sets out; and, being one user scope in one process,
it applies each accepted pair to the process's default cultures too, as
[output](#output-takes-its-culture-from-the-state) sets out. A host that declares its client's culture
independent leaves the runtime outside the page's pair, on the local switch, whose applications set the
process's defaults too, as the WebAssembly design's registration sets out. The host loads the globalization
data of every culture it serves, `BlazorWebAssemblyLoadAllGlobalizationData`, since the default data covers
only the starting culture.

## Blazor Hybrid

A Blazor Hybrid application renders its components to a web view in each of its native windows, each
`BlazorWebView` with a scope and a document of its own, all in one process, as the
[WebView design](webview-design.md) sets out. By default the application holds one culture pair across its web
views, which this contract makes deliberate: a web view is one view of the application, not a user of its own.
An application whose native windows each show a culture of their own chooses
[a culture per web view](#a-culture-per-web-view) instead.

### One culture for the application

- **One state and one switch.** The culture state is one instance for the application, which every web
  view's scope resolves, and so is `ICultureSwitch`, so a component and the host's native code switch
  through the same entry point.
- **The application's coordinator orders every switch.** It is a service of the application, and takes the
  place the document's coordinator holds in a Web App: it accepts requests one at a time, in the order they
  reach it, numbers each accepted pair in the application's sequence, whose identity is the application's
  run, applies the pair to the state, and delivers it to every web view. Each web view's document
  coordinator follows what it delivers, keeping the document's language and direction, the web view's
  landings and its address, and accepts no pair of its own.
- **A switch is accepted at once.** Its task completes once the state has applied the pair; every web view's
  document and address follow it as the coordinator delivers it, and one web view that is slow or refuses
  doesn't hold the switch back.
- **The coordinator is the integration.** The `ICultureStateIntegration` a web view's scope resolves is the
  application's coordinator, which applies through the state's own, so a pair applied through it enters the
  application's sequence as a switch does and every web view follows it. A pair a web view's document
  accepts, a landing at another culture's address, names its cultures alone, so the coordinator takes it as
  a switch only where it names another pair than the state's, and a pair the state holds with formats of
  its own stays.
- **A web view takes part as its first Forma component initializes,** or as a culture route page in it
  lands, whichever comes first, registering with the coordinator,
  which hands it the accepted pair with its version; its document coordinator starts from it, as on a
  standalone app's page. A web view opened after a switch starts in the pair that switch accepted.
- **A web view, disposed as its native window closes,** releases its registration and every subscription
  its components made, and one the host never disposed is dropped at the first delivery that fails or goes
  a minute unanswered, the time Blazor Server gives a call into script by default, as a WPF web view's does
  once its native window closed. The
  state, the coordinator and the accepted pair stay the application's, so the last web view's release
  changes no culture.

### The ambient culture

After a switch is accepted, every event any web view dispatches and every render runs in the accepted pair.
The coordinator applies the pair to the process's default cultures, which a flow with no culture of its own
reads, and to the cultures each web view's UI thread runs its events in, posted to that thread outside any
flow a caller captured, so a culture the platform or the host assigned to a thread doesn't hide a switch and
the pair outlives the flow that applied it. Web views needn't share a thread: Windows Forms dispatches each
web view's components on its own control's thread, while WPF dispatches every web view's on the
application's, and each web view's thread receives the pair. Work begun before a switch finishes in the
culture it captured, which is the platform's rule, while the render it asks for reads the state, as
[output](#output-takes-its-culture-from-the-state) sets out.

### What Forma updates and what the host refreshes

Selecting the culture and refreshing what already shows it are separate responsibilities. Forma updates the
culture state, which notifies its subscribers; every Forma component in every web view; each web view's
document's language and direction; each web view's address, under culture routing; and the ambient cultures
above. The host refreshes what it rendered without the state: a component of its own that doesn't follow
the state, and its native user interface, its controls, native window titles and native resources, which
Forma never touches. The state's notification is what the host listens to, from a component or from native
code.

### Persistence

A native application keeps the user's choice in its own settings: it saves the pair as the state notifies
it, and applies the saved pair at startup, before its first web view opens, since the state starts from the
culture in force as the application first resolves it. The switch persists nothing itself: its policy is
the state alone, and a cookie policy is rejected at startup, since no request reaches a server to read the
cookie.

### Culture addresses across web views

A Blazor Hybrid application has addresses and navigation, though no address bar: each web view starts at its
`StartPath`, and every page renders at an address. Culture routing serves a host whose pages a Web App
shares, as Microsoft's template for a MAUI application beside a Web App shares them, so one culture route
page renders in both. A native application has no address a reader shares, bookmarks or finds indexed, so
under Blazor Hybrid the application's culture decides, and each web view's address follows it:

- **A navigation a web view makes,** a link or `NavigateTo`, landing at an address in another culture,
  requests a switch to that address's pair; once the coordinator accepts it, every web view follows it.
- **Every web view follows an accepted switch** by replacing its address with the same page, query and
  fragment in the route carrying the pair, in place of the current history entry. The replacement carries
  Forma's mark, and its landing requests nothing. Blazor runs the web view's navigation handlers for it as
  for any navigation, and a handler that guards the user's work tells it from a navigation away by that
  mark, since the page and the work stay.
- **A web view whose handler refuses to follow** keeps its address, and shows the accepted pair meanwhile,
  since its components and its document follow the state. The switch stands: a switch is the
  application's, and a landing the requesting web view shows is resolved by its address, as every landing
  is.
- **Back and Forward** return to their entry's page in the application's culture, since the language is the
  application's setting rather than the page's: a traversal requests no switch, and one that lands at an
  entry in another culture is replaced by that page's address in the application's culture.
- **A web view's first landing,** at its `StartPath`, adopts the application's culture the same way, so a
  web view opened at a stale address never switches the application.
- **A switch navigates in place.** Culture routing's `CultureSwitchMode.FullLoad` is rejected at startup,
  since a web view reloads only by restarting its session, which loses the work an in-place switch keeps,
  and the culture-route cookie is never written.

### A culture per web view

An application can give each web view a culture of its own, as a WebAssembly client can hold one apart from
its page's, by declaring the independent arrangement on `AddFormaUIWebView`. Each web view is then a page of
its own, as a standalone app's is:

- **A state and a switch per web view.** Each web view's scope resolves a culture state and a switch of its
  own; a host's own state is registered scoped, and one two web views share is rejected as the second
  starts.
- **The web view's document orders its switches.** As the web view's first Forma component initializes, its
  state registers with its own document's coordinator and starts it from the state's pair under the
  switch's policy, as a standalone app's page starts; the document accepts the web view's switches and
  landings in its own sequence, delivers each to the web view's state, and takes its language and
  direction. A switch in one web view changes nothing in another.
- **The address decides, as on a Web App's page.** Under culture routing, a web view's first landing, at its
  `StartPath`, a navigation to another culture's address and a traversal each switch the web view to the
  pair their address carries, so a native window opens a web view in a culture by its start path. No
  request reaches a server, so the route last chosen is named in no cookie.
- **The ambient cultures stay the host's.** The process's defaults and the UI threads are shared by every web
  view, and WPF runs every web view's events on one thread, so Forma applies no web view's pair to them.
  Forma's components read the web view's state, as [output](#output-takes-its-culture-from-the-state) sets
  out; a host's component does the same, formatting through `ICultureState` and reading its strings through
  a locale bound to it, while what reads the ambient culture, `IStringLocale<T>`, a `@bind` on a plain
  element or the host's native user interface, reads the culture the host set.
- **Native code switches a web view through its own services**, which the platform's `BlazorWebView` reaches
  through `TryDispatchAsync`, since the application holds no switch of its own.

What holds for every web view holds here: the switch persists the pair in the state alone, and culture
routing switches in place.

## Assemblies and registration

The culture state is platform-neutral. A half serving a render mode takes the render mode's name, as
`Microsoft.AspNetCore.Components.Server` does, and a half serving the HTTP pipeline takes the platform's,
as an integration with ASP.NET Core does across .NET:

| Assembly | Holds |
| --- | --- |
| `Forma.Framework.Culture` | The culture state's abstraction, its integration contract and its default, the switch's abstraction, the culture provider's abstraction, the culture version, the local switch and the registrar, with no dependency on ASP.NET Core or navigation |
| `Forma.Framework.Culture.Browser.Interop` | The document's culture coordinator: its script, which holds every policy, building an address switch's destination and resolving an interactive landing from the published mapping; the initializer that reads the marker and the published policy and releases a closed circuit's registration; the registration that keeps a culture state on the accepted pair and reports a page's landing; the switch that requests through the coordinator; and the culture addressing a page's culture follows, the contract culture routing gives, through which the head publishes the document's base and the mapping and the switch refuses a pair no route carries |
| `Forma.Framework.Culture.Blazor.Server` | The circuit handler that applies the culture before every inbound activity and registers the circuit's state with the coordinator, and the request step that runs a request in the culture provider's cultures for the names its pipeline chose |
| `Forma.Framework.Culture.Blazor.WebAssembly` | The startup that registers the runtime's state with the coordinator, and the application that sets the runtime's current and the process's default cultures on every application of a pair, the local switch's included |
| `Forma.Framework.Culture.Blazor.WebView` | The application's culture state and its coordinator, which orders every switch across the web views and applies each accepted pair to the state, the process's default cultures and each web view's UI thread; the application's switch; each web view's registration with the coordinator, which its first component starts and its document's coordinator follows; and the startup checks that reject a cookie policy and a full-load switch |
| `Forma.Framework.Culture.Router` | The culture routes, their tokens and the mapping between addresses and pairs, `CultureRouter`, which gives the page its culture addressing, the switcher, the page base and the culture route scope, with no dependency on ASP.NET Core, so the server, a WebAssembly runtime and the published mapping share one set of rules |
| `Forma.Framework.Culture.Router.AspNetCore` | The routing middleware and the cookie's server side |

The product's registrars register all of them but the routing, which takes the host's options: the
render-mode-neutral `AddFormaUI` registers the culture and its browser interop, and `AddFormaUIServer`,
`AddFormaUIWebAssembly` and `AddFormaUIWebView` each add their render mode's half and its switch, through
the culture's own `AddFormaCultureServer`, `AddFormaCultureWebAssembly` and `AddFormaCultureWebView`, as the
[WebAssembly design](webassembly-design.md) and the [WebView design](webview-design.md) set out, so a host
makes one call on each side and can still override any single service.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| C1 | Where does the culture state live? | A platform-neutral `Forma.Framework.Culture`, the routing's neutral rules in `Forma.Framework.Culture.Router` and its ASP.NET Core parts in `Forma.Framework.Culture.Router.AspNetCore` |
| C2 | What is the state called? | `ICultureState`, which says what it holds and that it changes |
| C3 | Who keeps `<html lang dir>` in step? | The document's culture coordinator, on the pair it accepted last, whichever runtime requested it, so a switch is complete by default |
| C4 | Does the Showcase switch in place? | Yes, keeping the user's work, with the new load still supported |
| C5 | What does a platform half take its name from? | What it serves: a render mode's half takes the render mode's name, as the DOM's `Forma.Framework.Browser.Blazor.Server` and the culture's `Forma.Framework.Culture.Blazor.Server` do, and the HTTP pipeline's takes the platform's, as `Forma.Framework.Culture.Router.AspNetCore` does |
| C6 | What culture does an edit in progress parse in? | The one it began in, until it is committed or replaced, as [editing](#an-edit-in-progress-keeps-its-culture) sets out |
| C7 | How does output take the state's culture? | Components read the state's cultures explicitly, and an operation that reads the ambient culture runs in a bounded synchronous scope that saves and restores both, as [output](#output-takes-its-culture-from-the-state) sets out |
| C8 | How does a switch navigate? | By a full load by default; in place under `CultureSwitchMode.InPlace`, through culture-bearing interactive routes where an interactive router renders the pages, each page carrying its culture route, or through an enhanced navigation where the pages are islands, as [culture addresses](#culture-addresses) sets out |
| C9 | What is the culture-route cookie named, and where is it written? | `forma-culture-route`, carrying the application's path base and written at it, so applications sharing a host read their own |
| C10 | When is the document held through an enhanced navigation? | From every enhanced navigation's start until its landing, whose record the coordinator accepts from that navigation's own marker, or until the navigation ends without landing, as [the document](#the-document-follows-the-state) sets out |
| C11 | How does the state follow interactive routing? | A culture route page reports its interactive landing with the address its router accepted before its interactive page lifecycle runs, where it renders interactively within `CultureRouteScope`, which the host places around its `Router`; the coordinator resolves the record through the published mapping, and the lifecycle runs once the state has applied it, as [culture addresses](#culture-addresses) sets out |
| C12 | How many culture pairs does a page hold? | One, across every runtime on it: a switch is the application's, and every runtime converges on the pair it accepts, as [the switch](#one-switch-for-the-page) sets out |
| C13 | Where are switches ordered? | In the document's culture coordinator, which numbers each accepted switch in the document's sequence and distributes it; a version is the document's identity with that number, and each state applies the pairs it receives locally, ignoring one it has applied or an older one, as [the coordinator](#the-coordinator) sets out; under Blazor Hybrid, in the application's coordinator, whose sequence is the application's run's |
| C14 | What does awaiting a switch guarantee? | That it was accepted, its policy's persistence or navigation completed, and the runtime that requested it applied that pair or a newer one; other runtimes converge without holding it back, as [awaiting a switch](#what-awaiting-a-switch-guarantees) sets out |
| C15 | What does a switch do without coordination? | It fails, and never applies locally in its place; a local switch is a host's explicit choice and a test supplies its own, as [switches without the coordinator](#switches-without-the-coordinator) sets out |
| C16 | What does a culture route name? | Its UI culture, and a formatting culture of its own or, naming none, the UI culture, so the address carries the whole pair; one mapping, defined in the neutral routing assembly and published to the page, reads it for every navigation, and it decides over the cookie, as [culture addresses](#culture-addresses) sets out |
| C17 | Who applies an accepted pair? | The runtime's integration, through `ICultureStateIntegration`, resolved from the very instance resolved as `ICultureState`; a replacement that lacks it is rejected as the state is first resolved in its scope, or as it is registered where the registration is an instance, and a component reads the state and changes it only through the switch |
| C18 | What does releasing the last registration do? | Nothing to the culture: the coordinator retains the accepted pair and version for the document's lifetime, and a later registration adopts it |
| C19 | Where does a landing's pair come from? | One record per landing: a server-rendered landing's from its own response's marker, an interactive one's from the address its router accepted, resolved through the published mapping; the coordinator accepts it without waiting for a registration, and a cancelled or obsolete navigation changes nothing, as [landings](#landings) set out |
| C20 | How does the browser learn the switch's policy? | From configuration `FormaPageHead` publishes, or a standalone app's startup supplies, apart from any pair: the cookie's every setting and its one serialization, or the culture-route mapping, as [the policies](#switch-policies) set out |
| C21 | What does a switch wait for while its runtime registers? | An attempt already started, which its failure, its disposal or a disconnection settles with an error; never a registration nobody started, as [awaiting a switch](#what-awaiting-a-switch-guarantees) sets out |
| C22 | What does a switch to a pair without a route do? | Under the address policy it throws `InvalidOperationException` before anything changes, naming the pair |
| C23 | How many cultures does a Blazor Hybrid application hold? | One, across its web views, by default: one state and one switch for the application, the application's coordinator ordering every switch and every web view following it; or, where the application declares the independent arrangement, one per web view, each web view's document ordering its own, with the ambient cultures left to the host, as [a culture per web view](#a-culture-per-web-view) sets out |
| C24 | What runs in a switched culture under Blazor Hybrid? | Every event any web view dispatches after the switch and every render: the coordinator applies the pair to the process's defaults and to each web view's UI thread, and work begun before the switch finishes in the culture it captured |
| C25 | What does Forma refresh under Blazor Hybrid, and what the host? | Forma: the state and its subscribers, its components, each document and each address; the host: its own components that don't follow the state and its native user interface, through the state's notification |
| C26 | How does a Blazor Hybrid application persist its choice? | In its own settings, saved as the state notifies and applied at startup; the switch's policy is the state alone, and a cookie policy is rejected at startup |
| C27 | How do addresses follow under Blazor Hybrid? | The application's culture decides: a navigation a web view makes to an address in another culture requests a switch, every web view replaces its address in the accepted culture under Forma's mark, which requests nothing, a web view refusing it keeps its address while showing the pair, Back, Forward and a web view's first landing adopt the application's culture, and only an in-place switch is accepted, as [culture addresses across web views](#culture-addresses-across-web-views) sets out |
| C28 | Which culture does a name stand for? | The application's, where it registers an `ICultureProvider`: every culture Forma holds or applies is the provider's for its name, checked to carry that name; the platform's otherwise, as [the application's cultures](#the-applications-cultures) sets out |
