# Culture Switching Proposal

How a Forma host changes the culture of a running application, in place or with a new load, and how
every component follows it. This proposal sets out where Forma stands, the problem the change solves
and the design it proposes; once its decisions are made it becomes the culture design and its
implementation plan. The locale files themselves, their paths, packs and precedence, are the
[localization design](localization-design.md)'s.

## Where Forma stands

### The culture is decided per request

`CultureRoutingMiddleware`, in `Forma.Framework.Globalization`, reads the culture from the first
segment of the path, as in `/he/about`, or from the `forma-culture` cookie for a framework request
such as `/_blazor`, sets `CultureInfo.CurrentCulture`, `CultureInfo.CurrentUICulture` and the
`IRequestCultureFeature`, and removes the segment before routing. Every page has one canonical
address per culture, and `CultureRouter` builds the link to any page in any culture. The middleware
sees requests only: a navigation the router handles in the browser, under global interactivity, makes
no request, so the segment reaches the router as written.

### A circuit runs in the culture of its connection

A Blazor Server circuit runs over the `/_blazor` connection, and so in the culture of the request
that opened it. The circuit's host never sets a culture of its own: every event the browser sends
runs in the connection's culture. A culture assigned in an event handler is asynchronous-local, so it
holds for that event's continuation and nothing after it.

### Components read the culture in force when they render

Components read the ambient culture as they render: `CurrentCulture` for dates, numbers and
collation, `CurrentUICulture` for strings, and its `TextInfo.IsRightToLeft` for direction. The string
layer is culture-independent: `JsonStringLocale` reads `CurrentUICulture` on every lookup and its
file cache is keyed by culture, so a cached locale is never a stale one. Scripts take direction from
computed style or from the input profiles each render sends; none reads the browser's locale.

### The document's language and direction come from the request

The host's static `App` component renders `<html lang dir>` from the request's culture, as the
Showcase does and the [README](../README.md) tells a host to.

### A host switches language with a new load

The Showcase's language links are full page loads, and it keeps the reader's place across the load.
That is also the only approach the platform documents, on the server by setting a cookie and
navigating with `forceLoad: true`, and in WebAssembly by setting local storage and reloading
([Blazor globalization and localization](https://learn.microsoft.com/aspnet/core/blazor/globalization-localization)).
Under that model every component renders fresh in the new culture, and nothing is stale.

## The problem

### A general-purpose library can't require a reload

Forma is a library, so it can't decide how its host switches language. Blazor Hybrid, in MAUI, WPF or
WinForms, has no reload at all, so a host there can switch only in place. An application may refuse
to reload on principle, since a reload loses form input, open dialogs, scroll position and a workflow
under way. And WebAssembly, which Forma may come to support, makes an in-place change possible. A
host that switches in place today gets a broken result.

### What breaks when the culture changes in place

| What fails | Why | What the reader sees |
| --- | --- | --- |
| The change does not last on the server | The culture is asynchronous-local, and the next event runs in the connection's culture again | The new language once, and the old one again on the next click |
| Nothing renders again | A component renders again when its parameters change, and a culture change is no parameter | The date box keeps its old format |
| Some components refuse to render even when asked | The data view's rows, cards, list items and tree nodes gate their renders, and the culture is not among what they compare | Grid cells and their accessible names stay in the old language |
| Some text is computed once | It is built when the component initializes or kept in a field | The data view's view-switch names and group names, the input inspector's tools, the composer's shortcut rows |
| Direction is read once | It is kept from the first render, or from when a popover opened | The data tree's arrow keys stay reversed after switching between English and Hebrew, and an open popover keeps its old side |
| The document stays in the old language | `<html lang dir>` is static, and nothing in a circuit updates it | A page switched to Hebrew in place is laid out left to right |
| The address and the culture disagree | The culture changed and the address did not | `/he/about` shows English |

### A cascaded culture is not enough

A `CultureInfo` cascaded from the host's root to the component bases can make every component render
again when the culture changes, the inputs too, since their framework base accepts cascading
parameters as any component does. A trigger is all it is, though: it does not set the culture the
runtime runs in, so the server's next event renders in the old culture again; it does not reach a
render gate that compares parameters and skips cascading ones; it invalidates no text a component
computed once; and it updates neither the document nor the address.

### Two strings are wrong in every language

`FormaPropertyColumn` takes a property's title from `DisplayAttribute.Name` as written, where
`GetName()` resolves a resource-backed display name, and `FormaEmojiPicker`'s nine default group
titles, such as "Smileys & Emotion", are English text rendered as they are. Both are wrong under any
switching model.

## The proposed design

### The contract

Any Forma component shows the culture in force. A host may change the culture at any time, with a new
load or in place, and every component follows: its strings, its formats, its direction, and the
document's language and direction. The host tells Forma the new culture through one entry point and
never sets `CultureInfo` itself or wires a component, and a host that switches with a new load needs
no additional setup.

The contract covers the render modes Forma supports, static server rendering and Interactive Server.
WebAssembly and Hybrid are [later work](#webassembly-and-hybrid), each with its own application of the
culture.

Two kinds of text are exceptions, stated as such: text an event produced, an announcement or a
validation message, stays in the language it was produced in until the next such event produces it
again; and an edit in progress keeps the culture it began in, as [editing](#an-edit-in-progress-keeps-its-culture)
sets out.

### One culture state per user scope

A user scope is a circuit, and each holds one culture state: the culture, which formats dates, numbers
and collation, and the UI culture, which chooses strings and direction, kept apart as the platform
keeps them so that a host can show English text with German formats. The state starts from the
culture in force when the scope is created, the request's, and announces each change.

```csharp
public interface ICultureState
{
    CultureInfo Culture { get; }
    CultureInfo UICulture { get; }
    int Revision { get; }
    event Action? Changed;
    void Change(CultureInfo culture, CultureInfo? uiCulture = null);
}
```

Its rules:

- An omitted UI culture is the culture.
- A change to the pair already held is no change: it neither notifies nor advances the revision.
- The state holds read-only cultures, `CultureInfo.ReadOnly` of what it is given, so no caller can
  alter a culture behind it.
- The revision is the state's, advanced once per change, and the one source every component base and
  render gate reads.
- `Change` updates the state synchronously and schedules what follows: each component's notification
  dispatches its render through its renderer, and the document's update goes through the browser, so a
  return from `Change` means the state has changed, not that the browser shows it.
- `Change` applies the new cultures to its calling flow before it advances the revision and notifies,
  so the renders it schedules inherit them.

Components depend on the abstraction, and the library registers its default with `TryAdd`, so a host
that registers its own first still wins. The state knows nothing of navigation or addresses.

### Every render runs in the state's culture

A render can begin in three ways, and each takes its culture from the state:

- **The switch itself.** The event that calls `Change` continues in the new cultures, which `Change`
  applies to its calling flow on purpose, so the renders it schedules are already in them.
- **An event from the browser.** The circuit handler sets both cultures from the state before every
  inbound activity, every UI event and every call from script into .NET, through the circuit's
  inbound activity handler ([circuit activity](https://learn.microsoft.com/aspnet/core/blazor/fundamentals/signalr)).
- **Work begun before the switch.** A task started in the old culture may finish after the switch and
  ask for a render, through `InvokeAsync(StateHasChanged)` or a parameter it sets. The task finishes in
  the culture it captured, which is the platform's rule; its render shows the state's culture.

The third case needs a render to apply the state's culture temporarily: set both cultures from the
state as the component renders, and restore the caller's afterward, whether the render completes or
throws, so the culture of the work that asked for the render is unchanged once the render returns.
This differs from what `Change` does, which changes its calling flow for good.

That temporary application is provisional. Where a base can wrap a render is not settled: a component
written in Razor overrides `BuildRenderTree`, which a base's own override does not wrap, and
`ShouldRender` decides whether to render rather than wrapping the render, and is skipped for the first
one. The [working example](#plan) settles the boundary, through both component bases and real Razor
components, before the design adopts it.

### Components follow the state

Both component bases, Forma's component base and its input base, subscribe to the state as a component
initializes, render again on each change and unsubscribe as it is disposed, so every component follows
with no host wiring, at the cost of one subscription per live component; a virtualized collection keeps
only its visible items alive.

A component that gates its renders counts the state's revision among what it compares, so a row that
skips a render for unchanged data still renders for a changed culture.

One rule covers every component's output: its strings, formats and direction come from the culture in
force as it renders, and nothing that depends on the culture outlives a render unless it is keyed by
the culture.

A host's own components follow by subscribing to the same state, and text they pass down, such as a
tab's title, reaches Forma's components through their parameters.

### An edit in progress keeps its culture

An uncommitted edit keeps the culture it is parsed in until it is committed or cancelled, and the field
follows the current culture afterward, so `03/04/2026` typed as the fourth of March under one culture
commits as the fourth of March under any other:

- An edit that fails to parse keeps its text and its parsing culture.
- A committed value may be formatted again in the new culture, without changing the value or marking
  the field modified.
- Focus alone is no edit in progress; a field holding no uncommitted text follows a change at once.
- Only the parsing culture is kept: the field's labels, accessible names and direction follow the new
  UI culture.

### The document follows the state

In a Blazor Web App the `<html>` element belongs to the static `App` component, which no interactive
component renders. The host's static render sets the document's initial language and direction, and the
ASP.NET Core integration keeps them in step: on each change it sets `document.documentElement`'s `lang`
and `dir` through the scope's JavaScript environment, an operation that is idempotent, so several
interactive islands may run it. An enhanced navigation patches the document from a server response,
and the platform documents that such patches can undo a dynamic change, so the integration sets the
attributes again after each enhanced navigation.

### Culture addresses

A host with culture addresses switches through `CultureRouter`, which owns the switch: the
integration's entry point navigates to the other culture's address, and the state follows the address,
never the reverse, so an address and the culture it shows never disagree. Calling `Change` directly is
for a host without culture addresses.

The state follows the address in every way a navigation happens:

- **When the integration starts,** it reconciles the state with the current address.
- **An enhanced navigation** is a request, so the middleware renders its static content in the new
  culture, and the circuit's state follows the address it lands on.
- **Interactive routing** makes no request, so no middleware runs; the integration changes the state
  from the address on each location change, and route matching understands the culture segment there,
  which the middleware's rewrite cannot establish.
- **Back and Forward** are location changes like any other, and restore the culture of the address
  they return to.

A switch that makes no request writes the `forma-culture` cookie itself, so a circuit opened later, a
reconnection or a new tab, starts in the culture last chosen.

### WebAssembly and Hybrid

Each platform applies the culture its own way, and neither is part of the contract yet.

- **WebAssembly** runs one user scope in one process, so a change can set the process's default
  culture, and the host loads the globalization data of every culture it serves,
  `BlazorWebAssemblyLoadAllGlobalizationData`, since the default data covers only the starting culture.
- **Hybrid** must first choose its ownership: one culture shared by every window of the application,
  or one per `BlazorWebView` scope, reached through `BlazorWebView.TryDispatchAsync`. The process's
  default culture can't serve the second, since it applies across the application domain and never
  overrides a culture an execution context has assigned
  ([`CultureInfo.DefaultThreadCurrentCulture`](https://learn.microsoft.com/dotnet/api/system.globalization.cultureinfo.defaultthreadcurrentculture)),
  and a native window is not the scope its Blazor components resolve services from. Temporary
  application during a render, once proven, may let scopes differ, but it covers rendering only, not a
  native callback, a lifecycle operation or a parse outside a render.

### Assemblies and registration

The culture state is platform-neutral, and every ASP.NET Core half takes the platform's name, as an
integration with ASP.NET Core does across .NET:

| Assembly | Holds |
| --- | --- |
| `Forma.Framework.Globalization` | The culture state's abstraction, its default and its registrar, with no dependency on ASP.NET Core or navigation |
| `Forma.Framework.Globalization.AspNetCore` | The routing middleware, `CultureRouter`, the switching entry point, the circuit handler that applies the culture, the address reconciliation, the cookie and the document's attributes |
| `Forma.Framework.Browser.Interop.AspNetCore` | The DOM's circuit handler, renamed from `Forma.Framework.Browser.Interop.Server` under the same rule |

The product's aggregate registrar registers them, so a host makes its one call and can still override
any single service.

## Plan

Each step is built, reviewed and committed on its own.

1. Fix every component whose output outlives the culture it was made in, and the two strings that are
   wrong in every language; each fix is correct under any switching model.
2. Establish how culture addresses behave today under global interactivity, whether interactive route
   matching finds `/he/about`, and settle the [items to verify](#to-verify), before choosing a fix.
3. Build a small working example that proves the runtime behavior:
   - an immediate switch, whose scheduled renders are already in the new culture;
   - a delayed completion, a task begun in the old culture whose render shows the new one;
   - two isolated scopes in different cultures;
   - navigation, enhanced and interactive, Back and Forward among them;
   - culture application reaching real Razor rendering through both component bases, the input base's
     path among them;
   - a render in the new culture leaving the culture of the older work unchanged afterward, when the
     render throws too.
4. Add the culture state, subscribe both component bases to it, and count its revision in every render
   gate, applying a render's culture as the working example proved.
5. Apply the culture on the server before every inbound activity.
6. Keep the document's language and direction in step through the ASP.NET Core integration.
7. Follow culture addresses: the switching entry point, reconciliation, interactive routing, Back and
   Forward, and the cookie; and rename `Forma.Framework.Browser.Interop.Server`.
8. Switch language in place in the Showcase, keeping the user's work and the new load fully supported,
   and bring the README, the globalization READMEs, the localization and authoring rules and this design
   up to date.
9. Apply the culture in WebAssembly and Hybrid when those render modes are supported.

The tests show that a change renders an input, a component on the base and a gated grid row again; that
the circuit handler applies the culture around each inbound activity; and that the document operation
sets both attributes. In a browser, switching between English and Hebrew in place lays the page out
right to left and keeps it there on the next click, an edit in progress keeps its text and its parsing
culture, and a popover already open takes the new direction.

## To verify

- That a culture an event handler assigns doesn't reach the circuit's next event, which the circuit's
  host, setting no culture of its own, implies.
- Whether interactive route matching, under global interactivity, finds a page at a culture-prefixed
  address.
- Whether an enhanced navigation updates the `<html>` element's own attributes, or undoes a dynamic
  change to them.
- Whether location changes reach every interactive island on an enhanced navigation.

## Decisions

| | Question | Decision |
| --- | --- | --- |
| C1 | Where does the culture state live? | ✅ A platform-neutral `Forma.Framework.Globalization`, its ASP.NET Core parts moving to a new `Forma.Framework.Globalization.AspNetCore` |
| C2 | What is the state called? | ✅ `ICultureState`, which says what it holds and that it changes |
| C3 | Who keeps `<html lang dir>` in step? | ✅ The ASP.NET Core integration, so a switch is complete by default |
| C4 | Does the Showcase switch in place? | ✅ Yes, keeping the user's work, with the new load still supported |
| C5 | Does every ASP.NET Core half take the platform's name? | ✅ Yes: `Forma.Framework.Browser.Interop.Server` becomes `Forma.Framework.Browser.Interop.AspNetCore` |
| C6 | What culture does an edit in progress parse in? | ✅ The one it began in, until it is committed or cancelled, as [editing](#an-edit-in-progress-keeps-its-culture) sets out |
| C7 | How does a render take the state's culture? | Provisional: a base applies it for the render and restores the caller's afterward; adopted once the working example proves the boundary through both bases and real Razor components |
