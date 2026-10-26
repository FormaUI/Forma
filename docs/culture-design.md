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
component renders. The host's static render sets the document's initial language and direction, and the
ASP.NET Core integration keeps them in step: on each change it sets `document.documentElement`'s `lang`
and `dir` through the scope's JavaScript environment, an operation that is idempotent, so several
interactive islands may run it. An enhanced navigation patches the document from a server response, and
such a patch can undo a dynamic change, so the integration sets the attributes again after each enhanced
navigation.

## Culture addresses

A host with culture addresses switches through `CultureRouter`, which owns the switch: the integration's
entry point navigates to the other culture's address, and the state follows the address, never the
reverse, so an address and the culture it shows never disagree. Calling `ChangeAsync` directly is for a host
without culture addresses.

The state follows the address in every way a navigation happens:

- **When the integration starts,** it reconciles the state with the current address.
- **An enhanced navigation** is a request, so the middleware renders its static content in the new
  culture, and the circuit's state follows the address it lands on.
- **Interactive routing** makes no request, so no middleware runs; the integration changes the state from
  the address on each location change, and route matching understands the culture segment there, which
  the middleware's rewrite cannot establish.
- **Back and Forward** are location changes like any other, and restore the culture of the address they
  return to.

A switch that makes no request writes the `forma-culture` cookie itself, so a circuit opened later, a
reconnection or a new tab, starts in the culture last chosen.

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

The culture state is platform-neutral, and every ASP.NET Core half takes the platform's name, as an
integration with ASP.NET Core does across .NET:

| Assembly | Holds |
| --- | --- |
| `Forma.Framework.Globalization` | The culture state's abstraction, its default and its registrar, with no dependency on ASP.NET Core or navigation |
| `Forma.Framework.Globalization.AspNetCore` | The routing middleware, `CultureRouter`, the switching entry point, the circuit handler that applies the culture, the address reconciliation, the cookie and the document's attributes |
| `Forma.Framework.Browser.Interop.AspNetCore` | The DOM's circuit handler |

The product's aggregate registrar registers them, so a host makes its one call and can still override any
single service.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| C1 | Where does the culture state live? | A platform-neutral `Forma.Framework.Globalization`, its ASP.NET Core parts in `Forma.Framework.Globalization.AspNetCore` |
| C2 | What is the state called? | `ICultureState`, which says what it holds and that it changes |
| C3 | Who keeps `<html lang dir>` in step? | The ASP.NET Core integration, so a switch is complete by default |
| C4 | Does the Showcase switch in place? | Yes, keeping the user's work, with the new load still supported |
| C5 | Does every ASP.NET Core half take the platform's name? | Yes: the DOM's server half is `Forma.Framework.Browser.Interop.AspNetCore` |
| C6 | What culture does an edit in progress parse in? | The one it began in, until it is committed or cancelled, as [editing](#an-edit-in-progress-keeps-its-culture) sets out |
| C7 | How does output take the state's culture? | Components read the state's cultures explicitly, and an operation that reads the ambient culture runs in a bounded synchronous scope that saves and restores both, as [output](#output-takes-its-culture-from-the-state) sets out |
