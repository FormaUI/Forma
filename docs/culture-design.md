# Forma Culture Design

A Forma host decides the culture an application runs in, and may change it while the application runs,
with a new load or in place; every component follows it, and so do the document and the address. This
design states the contract, the culture state each user scope holds, how a render, an edit, the document
and a culture address follow it, and where each part lives. The locale files themselves, their paths,
packs and precedence, are the [localization design](localization-design.md)'s, and the steps that build
this design are the [implementation plan](culture-implementation-plan.md)'s.

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
document's language and direction. The host tells Forma the new culture through one entry point and
never sets `CultureInfo` itself or wires a component, and a host that switches with a new load needs no
additional setup.

The contract covers the render modes Forma supports, static server rendering and Interactive Server.
WebAssembly and Hybrid each apply the culture their own way, as [their section](#webassembly-and-hybrid)
sets out.

Two kinds of text are exceptions, stated as such: text an event produced, an announcement or a
validation message, stays in the language it was produced in until the next such event produces it
again; and an edit in progress keeps the culture it began in, as [editing](#an-edit-in-progress-keeps-its-culture)
sets out.

## One culture state per user scope

A user scope is a circuit, and each holds one culture state: the culture, which formats dates, numbers
and collation, and the UI culture, which chooses strings and direction, kept apart as the platform keeps
them so that a host can show English text with German formats. The state starts from the culture in
force when the scope is created, the request's, and announces each change through `INotifyStateChanged`,
as every Forma service a component follows does.

```csharp
public interface ICultureState : INotifyStateChanged
{
    CultureInfo Culture { get; }
    CultureInfo UICulture { get; }
    bool IsRightToLeft { get; }
    int Revision { get; }
    Task ChangeAsync(CultureInfo culture, CultureInfo? uiCulture = null);
}
```

Its rules:

- An omitted UI culture is the culture.
- `IsRightToLeft` is whether the UI culture is written right to left.
- A change to the pair already held is no change: it neither notifies nor advances the revision.
- The state holds read-only cultures, `CultureInfo.ReadOnly` of what it is given, so no caller can alter
  a culture behind it.
- The revision is the state's, advanced once per change, and the one source every component base and
  render gate reads. It advances after the change's cultures are in place, so a reader that reads the
  revision before the cultures never pairs it with cultures older than it; the state may be read from
  several threads while it changes, and a change is made from one flow at a time.
- `ChangeAsync` changes the state before it returns its task, which completes once every subscriber has
  been notified: each component's notification dispatches its render through its renderer, and the
  document's update goes through the browser, so a completed change means the state has changed and its
  renders are scheduled, not that the browser shows them.
- `ChangeAsync` applies the new cultures to its calling flow before it advances the revision and notifies,
  so the renders it schedules inherit them.
- A change takes no cancellation: once the state has changed, every subscriber is notified, so no
  component is left showing the culture before it.

Components depend on the abstraction, and the library registers its default, `DefaultCultureState`, one
per user scope with `TryAddScoped`, so every component resolves a state and a host that registers its own
first still wins. The state knows nothing of navigation or addresses.

## Output takes its culture from the state

A render can begin in three ways: the event that calls `ChangeAsync`, an event from the browser, and work
begun before a switch, a task started in the old culture that finishes after it and asks for a render
through `InvokeAsync(StateHasChanged)` or a parameter it sets. Whatever began it, a component's output is
in the state's cultures, because the component reads them from the state rather than from the ambient
culture of the flow that rendered it.

Both component bases inject the state as `CultureState`, and a component formats, parses, compares, sorts
and lays out by its cultures and its direction:

```csharp
[Inject] protected ICultureState CultureState { get; set; } = default!;
```

```razor
<span>@date.ToString("d", CultureState.Culture)</span>
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
restores both in a `finally`. The scope surrounds the operation alone; it spans no asynchronous work and
never assumes that a parent's render renders its children within it.

The ambient culture is kept in step too, for whatever reads it outside Forma's output. The circuit handler
sets both cultures from the state before every inbound activity, every UI event and every call from script
into .NET, through the circuit's inbound activity handler
([circuit activity](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr)), and `ChangeAsync`
applies the new cultures to its calling flow before it notifies. Work begun before a switch finishes in the
culture it captured, which is the platform's rule, while the render it asks for reads the state.

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

An uncommitted edit keeps the culture it is parsed in until it is committed or cancelled, and the field
follows the current culture afterward, so `03/04/2026` typed as the fourth of March under one culture
commits as the fourth of March under any other:

- An edit that fails to parse keeps its text and its parsing culture.
- A committed value may be formatted again in the new culture, without changing the value or marking the
  field modified.
- Focus alone is no edit in progress; a field holding no uncommitted text follows a change at once.
- Only the parsing culture is kept: the field's labels, accessible names and direction follow the new UI
  culture.

## The document follows the state

In a Blazor Web App the `<html>` element belongs to the static `App` component, which no interactive
component renders. The host's static render sets the document's initial language and direction, and a
document culture synchronizer keeps them in step with a culture state from then on: `lang` is the state's
UI culture's name, and `dir` its direction.

- **One owner.** While a document-language registration is live, its culture state determines the
  document's language and direction. A document follows one culture state: registering again under the
  live registration's identity recovers it and applies the latest pair, a registration under another
  identity is refused, and a call from a registration that no longer owns the document changes nothing.
- **What the server served.** The document keeps the pair the server last rendered: the attributes as the
  document loaded, and as the first page update after each enhanced navigation leaves them, read before an
  owner sets its own. An enhanced navigation's response patches the whole document, `<html>`'s attributes
  among them, while a streaming update patches only its component's region, so a streaming update never
  changes the served pair. An attribute the server left out is absent from the pair. The served pair is
  tracked for the document's lifetime, whether or not anything owns it.
- **After a page update.** A streaming update can undo an owner's pair, so the owner sets its latest pair
  again after each.
- **During an enhanced navigation.** From an enhanced navigation's start the document is held: the owner's
  pair stays until the response patches the document, the patch's pair stands, and every pair the owner
  sends meanwhile waits. The hold ends when the circuit acknowledges reconciling its state for the
  navigation that landed last, a navigation within one culture included, and the owner then sets the pair
  that acknowledgement carries. A navigation another replaced is never acknowledged, and a closed circuit
  ends the hold by releasing its registration. A surviving interactive island renders in the new culture
  once its circuit has reconciled, a round trip after the patch.
- **When the owner goes.** A circuit's registration is released when the client closes the circuit: once
  no interactive server component remains, after the circuit's inactivity timeout, and before a paused
  circuit resumes. Until then the circuit's state keeps the document. Releasing stops the owner setting its
  pair and restores the served pair, removing an attribute the server left out. A circuit ended by a fatal
  server error is not closed this way, and its registration stays until the page reloads, as Blazor's error
  UI asks.
- **Applying a change.** Changes are applied one reconciliation at a time. A reconciliation reads the
  state's revision and then its UI culture, from which both attributes come, does nothing when that
  revision is applied, and otherwise sends the pair, recording the revision only once the browser has it;
  since the revision is published after the cultures, a recorded revision never claims a culture that
  wasn't sent. A change during a reconciliation starts another, a failed call leaves its revision for the
  next, and each connection to the browser registers again, so a registration the browser released is
  never taken as live.

## Culture addresses

The address decides a page's culture, and the state follows the address, never the reverse, so an address
and the culture it shows never disagree. A host with culture addresses switches through a switcher, one per
circuit beside the application's one `CultureRouter`: it requests the other culture's address of the
current page, and the navigation that request becomes changes the state. The switcher's current route is
the culture state's, so work begun before a switch reads the route it began in. Calling `ChangeAsync`
directly is for a host without culture addresses.

How a switch navigates is the host's choice, made once where it configures culture routing:

- **A full load, by default.** Forma supplies the document's base in the address's culture, `/he/`, or
  `/app/he/` under the path base `/app`, so interactive routing finds a page within its culture, and an
  address in another culture lies outside the base and loads a new document and circuit.
- **In place, when the host declares culture-bearing interactive routes.** Every page then carries its
  culture route beside its plain one, `@page "/about"` and `@page "/{culture}/about"`, and the document's
  base is the plain one, so interactive routing reaches another culture's address in the same circuit,
  keeping the user's work. Forma's page base supplies only the `Culture` parameter and its validation; it
  doesn't declare a route, so each page still declares its own. A segment that isn't a culture's token
  stops the page before its lifecycle with the router's not-found outcome: an interactive navigation
  renders the router's not-found page in the same circuit, which needs the `Router`'s `NotFoundPage`, and
  a full request answers 404 with it. An alias or a token in another case is replaced by the canonical
  address, and so is the default culture's token under `ExceptDefault`.

The state follows the address at the event that settles each kind of navigation:

- **A full load** is a request: the middleware renders it in its address's culture, and the circuit,
  as it opens and again as it resumes, reconciles its state with the address before the document culture
  synchronizer starts.
- **An enhanced navigation** is a request too, and the circuit hears its location change as it starts,
  before its response, so the state follows the client's report that the navigation landed, never the
  location change, and the circuit acknowledges each reconciliation, as
  [the document](#the-document-follows-the-state) sets out.
- **Interactive routing** makes no request, so under culture-bearing interactive routes the circuit's
  location change is the navigation itself, and the state follows it there. Without them, the circuit
  doesn't follow a location change.
- **Back and Forward** are navigations of the kind they become, and restore the culture of the address
  they return to.

A navigation resolves by the address it leaves behind:

- one cancelled before the address changes leaves the state and the cookie as they were;
- one that lands sets the state from its final address, a redirect's included;
- an older navigation's late completion never overwrites a newer one;
- a failure after the address changed is resolved by the address the browser shows, never by restoring the
  culture the navigation left.

The `forma-culture-route` cookie only names the culture last chosen, for a request whose address carries
none, such as the circuit's own connection. A new tab opens in the culture its address names; under
`ExceptDefault`, `/` is the default culture. So two tabs in two cultures each keep their own: one tab's
switch rewrites the shared cookie, and the other, reconnecting, is reconciled with its own address.

The cookie belongs to the application, so it is written at the application's path base, and it isn't
`HttpOnly`, since a switch that makes no request writes it from the browser's script. The `forma-culture`
cookie an earlier version wrote, `HttpOnly` at `/`, is replaced: a request carrying it alone takes its
token as the culture last chosen where it names a route and the address names none, and a document's
response writes the new cookie and expires the old one at `/`; where both arrive, the new one decides and
the old one is expired. Each name is written at one path, so the order in which a browser sends two
same-named cookies is never part of the contract, an application at `/` included.

The culture segment is the first segment after the path base, and a request whose culture segment the
middleware removes is routed again, since `UsePathBase` routes a request as it removes the base. The
circuit's transport takes the culture its own address names, which the document's base makes the page's.

## WebAssembly and Hybrid

Each platform applies the culture its own way, and neither is part of the contract until Forma supports
its render mode.

- **WebAssembly** runs one user scope in one process, so a change can set the process's default culture,
  and the host loads the globalization data of every culture it serves,
  `BlazorWebAssemblyLoadAllGlobalizationData`, since the default data covers only the starting culture.
- **Hybrid** must first choose its ownership: one culture shared by every window of the application, or
  one per `BlazorWebView` scope, reached through `BlazorWebView.TryDispatchAsync`. The process's default
  culture can't serve the second, since it applies across the application domain and never overrides a
  culture an execution context has assigned
  ([`CultureInfo.DefaultThreadCurrentCulture`](https://learn.microsoft.com/dotnet/api/system.globalization.cultureinfo.defaultthreadcurrentculture)),
  and a native window is not the scope its Blazor components resolve services from. Output that reads
  its scope's state lets scopes differ in what they show, but it covers Forma's output only, not a native
  callback, a lifecycle operation or a parse that reads the ambient culture.

## Assemblies and registration

The culture state is platform-neutral. A half serving a render mode takes the render mode's name, as
`Microsoft.AspNetCore.Components.Server` does, and a half serving the HTTP pipeline takes the platform's,
as an integration with ASP.NET Core does across .NET:

| Assembly | Holds |
| --- | --- |
| `Forma.Framework.Globalization` | The culture state's abstraction, its default and its registrar, with no dependency on ASP.NET Core or navigation |
| `Forma.Framework.Globalization.Interop` | The document culture synchronizer: its script, the initializer that tracks what the server served and releases a closed circuit's registration, and the service that keeps the document on a culture state |
| `Forma.Framework.Globalization.Interop.Server` | The circuit handler that applies the culture before every inbound activity and starts the document culture synchronizer |
| `Forma.Framework.Globalization.AspNetCore` | The routing middleware, `CultureRouter`, the switcher, the page base, the document's base, the address reconciliation and the cookie |
| `Forma.Framework.Browser.Interop.Server` | The DOM's circuit handler |

The product's aggregate registrar registers all of them but the routing, which takes the host's options, so a
host makes one call for the rest and can still override any single service.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| C1 | Where does the culture state live? | A platform-neutral `Forma.Framework.Globalization`, its ASP.NET Core parts in `Forma.Framework.Globalization.AspNetCore` |
| C2 | What is the state called? | `ICultureState`, which says what it holds and that it changes |
| C3 | Who keeps `<html lang dir>` in step? | The document culture synchronizer, which the culture's circuit handler starts with each circuit, so a switch is complete by default |
| C4 | Does the Showcase switch in place? | Yes, keeping the user's work, with the new load still supported |
| C5 | What does a platform half take its name from? | What it serves: a render mode's half takes the render mode's name, as the DOM's `Forma.Framework.Browser.Interop.Server` and the culture's `Forma.Framework.Globalization.Interop.Server` do, and the HTTP pipeline's takes the platform's, as `Forma.Framework.Globalization.AspNetCore` does |
| C6 | What culture does an edit in progress parse in? | The one it began in, until it is committed or cancelled, as [editing](#an-edit-in-progress-keeps-its-culture) sets out |
| C7 | How does output take the state's culture? | Components read the state's cultures explicitly, and an operation that reads the ambient culture runs in a bounded synchronous scope that saves and restores both, as [output](#output-takes-its-culture-from-the-state) sets out |
| C8 | How does a switch navigate? | By a full load by default; in place where the host declares culture-bearing interactive routes, each page carrying its culture route, as [culture addresses](#culture-addresses) sets out |
| C9 | What does the cookie migrate by? | A new name, `forma-culture-route`, at the application's path base, which replaces the `HttpOnly` `forma-culture` without relying on the order of same-named cookies |
