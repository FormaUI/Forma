# Forma Component Bases

## Status

Design for the base classes every Forma component derives from, and for the three
services they carry: the browser runtime, logging, and localization. Authoritative for
`FormaComponentBase`, `FormaElementBase`, `FormaInputBase<TValue>`, `CssClassBuilder`,
the null objects `UnavailableJSRuntime` and `NullStringLocaleFactory`, and how logging
reaches every assembly of the library.

Steps 1 and 2 of the implementation plan are built: the builder, the null objects, the
three bases and the fallback registrations exist, and every component derives from a
base. The plan at the end records what landed at each step and where it departed from
the sketches below. Two departures apply to the sketches themselves: the root's class
and style are `string?`, null when there is nothing to write, so an element carries no
empty `class` or `style` attribute; and the root reference is named `RootElement`.

Step 3, interop logging, and step 6, the JS environment, are built. Reviewing step 3
changed the design: threading one logger touched every wrapper, every
construction site and two services, so what a component hands down is now one object,
its context, and what a wrapper takes is one object, the JS environment inside it.
Steps 6 to 9 below carry that revision: the context and the environment, a style
builder to match the class builder, and the root's class and style as one composed
object.

The root is the element the host addresses, on both tiers. On an input that is the
control inside its `FormaField` (the `<textarea>`, or the trigger `<button>` of a select),
never the field wrapper; on a composite it is the control the host names and styles (a
drop-down menu's trigger button, a checkbox's `<input>` inside its label). A wrapper the
component draws around that control is an internal part, held in a private reference
where interop needs it. There is no separate control member: the root is the control.

## Purpose

Every component repeats the same block: an attribute bag the host fills, a runtime
injection, a locale injection, a class merged from its own and the host's, a root element
reference, and a disposal that tears its interop down. Nothing in the library logs, so a
dropped circuit or a decorative asset that fails to load leaves no trace. And a form input
that adds asynchronous disposal never runs the framework's own cleanup, because the
renderer calls only `DisposeAsync` on a component that implements both interfaces.

> What is every Forma component, what does every input add to that, and how does a
> component say what happened to it without a host having to set anything up?

## Decision summary

- **Three bases, named for what they are.** `FormaComponentBase` is what every
  component is; `FormaElementBase` is a component with a root element its host
  addresses; `FormaInputBase<TValue>` is the framework's `InputBase<TValue>` with the
  same reach, strings, log, host surface and disposal as any other Forma component. The
  `Base` suffix is the .NET shape for an abstract root, as `ComponentBase` and
  `InputBase` are.
- **Required services, null defaults in the container.** The runtime, the logger factory
  and the locale factory are injected, required. A default on an injected property is
  dead code, since Blazor sets the property after construction and throws when the
  service is missing; the defaults are null objects registered by `AddFormaUI` with
  `TryAdd`, so a host's own registration wins by coming first, as every Forma default
  already behaves.
- **A logger per component, under its own type.** `Logger` is created lazily from the
  concrete type through `ILoggerFactory`, so a host filters by
  `Forma.Blazor.Components.Windows.FormaWindow` and nothing else. Interop wrappers take
  the component's JS environment, which carries that logger and the component's identity,
  so a frame's interop logs under the frame and names the frame.
- **Strings per component, under its own type unless it says otherwise.** `L` is created
  lazily through `IStringLocaleFactory` from `LocalizationSourceType`, which is the
  component's own type by default and a feature's type where components share a file,
  as the explorer's and the composer's do.
- **Debug only, and only what is already silent.** The first pass logs the exceptions the
  library already swallows by design, at `Debug`, through `[LoggerMessage]`. Every
  standard provider hides `Debug` unless asked, so a host sees nothing new until it turns a
  category on. `Warning` and `Error` are decided per site later, never blanket-applied.
- **The host's values in, the root's values out.** `Id`, `Class` and `Style` are what
  the host passed; `Root.Class` and `Root.Style` are what the base writes on the root,
  built by `BuildRootClass` and `BuildRootStyle` through the two CSS builders and
  composed by one object each base owns. `AdditionalAttributes` keeps the framework's
  name, because `InputBase<TValue>` fixes it and the two tiers must read the same.
- **One context per component, handed down whole.** `IComponentContext` is what a
  component is given: its identity, its strings, its log and its JS environment. The
  base builds it once and forwards `RuntimeId`, `L`, `Logger` and `JS` to it, so a
  component reads the same names it always has, and a new thing every component needs is
  added to the context once rather than to every constructor that passes it on.
- **The JS environment is a runtime.** `IJSEnvironment : IJSRuntime` adds the owner's
  `RuntimeId` and `Logger` to the runtime, so a wrapper calls it as the runtime it is
  and logs through it as its owner. It lives in `Forma.Framework.Core.Interop`, which
  knows nothing of components; the component context builds its environment from its
  own identity and log, so the two can never disagree. `JSInterop` and `JSModule` take
  the `JS` casing of `Microsoft.JSInterop` and of every Forma type already named for it.
- **Style is built, not concatenated.** `CssStyleBuilder` beside `CssClassBuilder`, so
  `BuildRootStyle` adds declarations the way `BuildRootClass` adds classes, and the
  termination rule lives in one place.
- **Async disposal runs the framework's cleanup.** `FormaInputBase<TValue>.DisposeAsync`
  calls `Dispose(true)`, which the renderer never does on an `IAsyncDisposable`.
- **One identity per instance, and `Id` means the consumer's DOM id everywhere.**
  `RuntimeId` is a `Guid` every component carries for its lifetime: the generated ids
  of its elements derive from it, a log line can name the instance, a window's workspace
  identity is it (`IWindow.RuntimeId`), and the root element carries it as
  `data-rid="@RuntimeId"`. `Id` is the id a consumer gives the root element and nothing
  else, on every component without exception; the two sit side by side on the root,
  `id="@Id" data-rid="@RuntimeId"`, so a consumer's id is never displaced by ours.

## What the survey found

The design rests on a survey of the 106 components of `Forma.Blazor` as of this
document, not on impression.

| Fact | Consequence |
| --- | --- |
| No component renders nothing of its own: 58 own one root element, 34 root in another Forma component and forward the host's attributes to it, the rest wrap an owned element in a cascade or beside a sibling. | The plain tier exists for the few that take nothing from a host; the element tier is nearly everyone. |
| 61 components inject a locale; 31 of them deliberately read a feature's file, the explorer's twenty sharing `FormaExplorer`'s and the composer's sharing `FormaComposer`'s. | `L` cannot be made from the component's own type alone; `LocalizationSourceType` is the override. |
| Generic components inject a locale for a non-generic marker type, five of them, because `IStringLocale<T>` needs a non-generic argument; the JSON factory already resolves a closed generic to its open definition's files. | By-type creation retires the marker types. |
| 47 test classes register localization in their own constructor because the test context does not. | With a null factory registered by `AddFormaUI`, the test context inherits it and the 47 registrations go. |
| 29 components inject `IJSRuntime`, 41 sites construct an interop wrapper by hand, 32 implement `IAsyncDisposable`, 16 hold a `DotNetObjectReference` to self. | The runtime goes on the base; the self reference stays with the interop-owning components, since it is theirs alone. |
| The renderer, decoded from the installed framework, tests `IAsyncDisposable` first and returns without touching `IDisposable`. | Four inputs that add `IAsyncDisposable` leak their `EditContext` subscription today; the input base closes it. |
| Nothing in `src/` logs: no `ILogger`, no console writes in C#, one `console.error` in a script. Every catch either swallows by design or hands the exception to the host as a domain object. | Logging is new ground, and the first messages are the swallowed catches. |

## The bases

### `FormaComponentBase`

```csharp
/// <summary>What every Forma component is: it reaches the browser, speaks the reader's language, can say what happened to it, and is disposed once.</summary>
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;
    [Inject] private ILoggerFactory LoggerFactory { get; set; } = default!;
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; } = default!;

    /// <summary>Whether the component has been disposed.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>The type whose translation files the component reads: its own, unless it shares a feature's.</summary>
    protected virtual Type LocalizationSourceType
        => GetType();

    /// <summary>What the component is given: its identity, its strings, its log and its JS environment.</summary>
    protected IComponentContext Context
        => field ??= new ComponentContext(
            () => JSRuntime,
            () => LocaleFactory.Create(LocalizationSourceType),
            () => LoggerFactory.CreateLogger(GetType()));

    /// <summary>The component's identity for its lifetime, which the ids of its elements derive from.</summary>
    public Guid RuntimeId
        => Context.RuntimeId;

    /// <summary>The JS environment the component's interop runs in.</summary>
    protected IJSEnvironment JS
        => Context.JS;

    /// <summary>The component's log, under its own type as the category.</summary>
    protected ILogger Logger
        => Context.Logger;

    /// <summary>The component's strings, in the reader's culture.</summary>
    protected IStringLocale L
        => Context.Locale;

    /// <summary>Releases what the component holds; runs once.</summary>
    protected virtual ValueTask DisposeAsyncCore()
        => ValueTask.CompletedTask;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        await DisposeAsyncCore();
        GC.SuppressFinalize(this);
    }
}
```

The context and the environment behind those forwards:

```csharp
/// <summary>What a component is given: its identity, its strings, its log and its JS environment.</summary>
public interface IComponentContext
{
    Guid RuntimeId { get; }

    IStringLocale Locale { get; }

    ILogger Logger { get; }

    IJSEnvironment JS { get; }
}

/// <summary>The runtime a wrapper calls, carrying the identity and log of the component that owns it.</summary>
public interface IJSEnvironment : IJSRuntime
{
    Guid RuntimeId { get; }

    ILogger Logger { get; }
}
```

`ComponentContext` takes the three as `Func<T>` and holds each in a `Lazy<T>`, mints
`RuntimeId`, and builds its `JS` once as `new JSEnvironment(runtime, Logger, RuntimeId)`,
so the environment's identity and log are the context's by construction.
`JSEnvironment` delegates the two `InvokeAsync` overloads to the runtime it wraps; the
framework's `InvokeVoidAsync` and the rest are extension methods on `IJSRuntime` and
work on it unchanged. A service that is not a component, the input router and the emoji
sprite loader, has no context and builds its own environment from the runtime and the
`ILogger<T>` it takes, minting an identity for the instance. Synchronous calls are not
wrapped: no Forma wrapper makes one. Should one need to, a second environment,
`JSInProcessEnvironment : IJSEnvironment, IJSInProcessRuntime`, forwards the synchronous
calls as well, and the context builds it where the injected runtime is in-process, so a
wrapper tests its environment for `IJSInProcessRuntime` exactly as the framework tests
the runtime. Creating a locale is a name
normalization and a dictionary lookup, with the file read on the first string, and a
logger factory caches its loggers, so the lazies are a shape choice, not a cost one.

`RuntimeId` is a version 7 GUID, so instances sort by creation and a log filtered to one
component reads in the order its instances were made. A root that needs an id when the
consumer gives none derives one from `RuntimeId` through a computed property,
`ElementId => Id ?? $"sel-{RuntimeId:N}"`, and every part hangs off that stem by its role,
`PopoverId => $"{ElementId}-popover"`, so a consumer who names the root names its parts
and an unnamed instance's parts all share one stem; and the root element writes the identity itself as
`data-rid="@RuntimeId"`, so a reader of the DOM, a log line and a workspace all name the
same instance the same way. `FormaWindow`'s `IWindow.RuntimeId` is that same property.

The logger's category is the concrete type, which for a generic component the logging
library prints without its arguments. The locale's category follows the JSON factory,
which already takes a closed generic to its open definition and strips the arity, so
`FormaDataGrid<Row>` reads `FormaDataGrid`'s files. Both are lazy: a component that
never logs and never reads a string pays for neither.

### `FormaElementBase`

```csharp
/// <summary>A component with a root element its host addresses: by id, class, style and any further attribute.</summary>
public abstract class FormaElementBase : FormaComponentBase
{
    /// <summary>The root element's id, verbatim.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>The classes the host adds to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The style the host adds to the root element.</summary>
    [Parameter] public string? Style { get; set; }

    /// <summary>Additional attributes applied to the root element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>The root element, once rendered.</summary>
    public ElementReference RootElement { get; protected set; }

    /// <summary>The class and style written on the root element: the component's own, then the host's.</summary>
    protected RootAttributes Root { get; }  // Composed once from BuildRootClass then Class, and BuildRootStyle then Style.

    /// <summary>The classes the component gives its root element in its current state.</summary>
    protected virtual void BuildRootClass(CssClassBuilder builder)
    {
    }

    /// <summary>The declarations the component gives its root element in its current state.</summary>
    protected virtual void BuildRootStyle(CssStyleBuilder builder)
    {
    }
}
```

Blazor binds attributes to parameters case-insensitively, so a host's `class="…"` lands
in `Class` and its `style="…"` in `Style`; neither reaches `AdditionalAttributes` any
more, and the components that dig a style out of the attribute bag today stop. The root
line of every element component's `.razor` is then the same:

```razor
<div id="@Id" data-rid="@RuntimeId" class="@Root.Class" style="@Root.Style" @attributes="AdditionalAttributes" @ref="RootElement">
```

A component that roots in another Forma component forwards the four to the one inside.
The identifiers rule holds: `Id` is the root's id verbatim, and a component whose root
needs an id when the host gives none keeps its own `ElementId` fallback.

`CssClassBuilder` lives in `Forma.Framework.Css` beside `CssColor`: `Add(name)`,
`Add(name, when)`, and a `ToString()` that joins what was added. It is the one place a
class rule can live, and it replaces the ternary-and-null idiom at every call site.
`CssStyleBuilder` sits beside it with `Add(property, value)` for a declaration the
component composes and `Add(declarations)` for a string the host supplied, `IsEmpty`,
and a `ToString()` that terminates every declaration; it replaces the lists and
`string.Create` calls the five style-building components carry, and the termination
helper on the bases.

`RootAttributes` is one sealed type, constructed with the two builder actions a base
owns, exposing `Class` and `Style` as `string?`, null when nothing was added so the
attribute is omitted. Each base passes its own composition: the element tier adds the
component's classes then the host's, the input tier adds the field's validation class
after those. `Root` is not virtual; the builder hooks are the extension points, and the
composition rule is the base's. A single splatted attribute dictionary was weighed and
set aside: the renderer lets a later attribute win and omits a null one, so it would
work, but it turns the attribute-order rule into a precedence rule that has to be
written down first, and that is its own change.

### `FormaInputBase<TValue>`

```csharp
/// <summary>What every Forma form input is: the framework's input with the same reach, strings, log, host surface and disposal as any other Forma component.</summary>
public abstract class FormaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
    // The three injections, Context and its four forwards, LocalizationSourceType, IsDisposed and DisposeAsyncCore
    // as on FormaComponentBase; Id, Class, Style, RootElement, Root, BuildRootClass and BuildRootStyle as on
    // FormaElementBase. AdditionalAttributes is InputBase's own. Root.Class is built from BuildRootClass, Class
    // and the field's validation class, in that order.

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;
        await DisposeAsyncCore();
        Dispose(disposing: true);   // InputBase's cleanup, which the renderer never calls on an IAsyncDisposable.
        GC.SuppressFinalize(this);
    }
}
```

C# has one base slot, so the input base repeats what the other two carry rather than
deriving from them. The context carries what both can share, the identity, the strings,
the log and the environment, and the root object carries the class and style
composition; what stays written out twice is the three injections,
`LocalizationSourceType`, `IsDisposed`, the dispose pair and the host parameters.

`ComposerDialogBase` re-bases onto `FormaElementBase`, overrides
`LocalizationSourceType` once to `typeof(FormaComposer)` for its seven dialogs, and
keeps its name.

## The services

### The null objects

| Contract | Null object | Lives in | Behaviour |
| --- | --- | --- | --- |
| `IJSRuntime` | `UnavailableJSRuntime.Instance` | `Forma.Framework.Core.Interop` | Throws `JSRuntimeUnavailableException` on any call. |
| `ILoggerFactory` | `NullLoggerFactory.Instance` | `Microsoft.Extensions.Logging.Abstractions` | Discards. |
| `ILogger<T>` | `NullLogger<T>` | `Microsoft.Extensions.Logging.Abstractions` | Discards; for services that inject a typed logger. |
| `IStringLocaleFactory` | `NullStringLocaleFactory.Instance` | `Forma.Framework.Localization` | Every key is its own text, holes and plurals resolved for the source language, as the JSON locale does when no file matches. |

`AddFormaUI` registers all four with `TryAdd`. A host that wants translations calls
`AddJsonLocalization()` first, as the Showcase does; a host with its own logging or its
own runtime has it in place before `AddFormaUI` runs, as ASP.NET Core's are. Each feature
registrar also `TryAdd`s the logger fallback for its own services, as `Timing` registers
`TimeProvider.System`, so a feature stays usable on its own.

The runtime's null object throws rather than discards because a component that reaches
the browser without one is misconfigured, and a clear exception at the first interop
call beats a "no registered service" throw at construction. It applies where no runtime
exists at all: a bare container in a framework test, tooling, or `HtmlRenderer` rendering
a component to a string. In a Blazor host the framework's runtime is registered first and
the null object never takes effect.

`NullStringLocaleFactory` is the name of the null object for the factory; the locales it
makes are not empty but source-only, the behaviour a JSON locale already has for a key
with no translation.

### Logging

Every assembly that logs takes `Microsoft.Extensions.Logging.Abstractions` from the
`Microsoft.AspNetCore.App` framework reference it already carries, which also supplies
the `[LoggerMessage]` generator. Nothing references the concrete logging package; a
host's providers are the host's.

A component logs through `Logger`. A service that is not a component takes
`ILoggerFactory` or `ILogger<T>` through its constructor, required. An interop wrapper,
`JSInterop` or `JSModule` and every subclass, takes the owner's `IJSEnvironment`, which
carries the owner's logger and identity, so everything about a frame, its interop
included, lands under `Forma.Blazor.Components.Layout.FormaFrame`, and every interop
message names the instance it was about.

Messages are source-generated, one `internal static partial class {Feature}Log` per
feature, so every message a feature can emit is read in one place:

```csharp
internal static partial class WindowLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while raising {Title}.")]
    public static partial void CircuitDroppedWhileRaising(ILogger logger, string title);
}
```

The first pass logs at `Debug` and only what the library already swallows: the dropped
circuit and thrown script behind a `Try*` interop call, a decorative asset that would
not load, a cancelled debounce, a plural expression that fell back. The `Try*`
contract itself is unchanged: the call still returns `false` or `default`, and the
silence is now visible to whoever turns the category on. Anything that deserves
`Warning` or `Error` is decided site by site, later, and never in bulk.

The script side keeps its one `console.error` for the rare script-side failure and gains
no framework of its own; the gap this design closes is on the .NET side.

## What the bases do not carry

- **The self reference.** `DotNetObjectReference<T>` exists only for interop callbacks
  and is created by the sixteen components that need one. The base has no `T`.
- **Generated ids.** They belong to the elements that need them and vary by element; a
  component keeps its own fields and derives each from `RuntimeId`, as the identifiers
  rule prescribes.
- **The input scope.** A component that registers with the input router does so itself.
- **Anything a subset needs.** The line to hold is that a base carries what every
  component of its tier is, never what some need; the rest is composition.

## Non-goals

- A Forma-specific logging abstraction. `ILogger` is the abstraction every host already
  configures and filters.
- Logging on the script side beyond what exists.
- `Warning` and `Error` levels in the first pass.
- Retiring `IStringLocale<T>`. It stays for services and hosts; components simply no
  longer need it.
- A splatted root attribute dictionary, `Root.Attributes`. Possible, and deferred until
  its precedence rules are written.

## Still open

- `FormaCheckBox` and `FormaDatePicker` were element-tier components, not form inputs.
  Both are inputs now: the checkbox binds `Value` like `FormaToggleSwitch`, and the
  library's control-style uses of it name a field through `ValueExpression`; the date
  picker binds one `DateOnly?`, and its range mode became `FormaDateRangePicker`,
  binding one `DateRange` — a value type in `Models/` — over a shared `FormaCalendar`
  that carries the month mechanics while each picker owns its selection rules.
- A component rooted in another Forma component forwards `Id`, `Class`, `Style` and the
  attribute bag to it and overrides `RootElement` to answer with that component's root,
  so a reader gets the same element whichever of the two it holds.

## Implementation plan

Each step is one gated commit: build with no warnings, every test project, the script
tests, the format check, and a visible browser walk where a step changes what a page
does.

1. **Foundations.** `CssClassBuilder` in `Forma.Framework.Css`; `UnavailableJSRuntime`
   and `JSRuntimeUnavailableException` in `Forma.Framework.Core.Interop`;
   `NullStringLocaleFactory` in `Forma.Framework.Localization`; the three bases in
   `Forma.Blazor/Components`; the fallback registrations in `AddFormaUI` and in
   `FormaBunitContext`; tests for each. Nothing migrates; the solution builds with the
   bases unused. The Showcase's and the README's registration order puts
   `AddRazorComponents()` before `AddFormaUI()`, so Blazor's registrations precede the
   fallbacks. **Built.**
2. **Migration ledger, then migration.** One row per component: its current base, its
   tier, whether it overrides `LocalizationSourceType`, and what it sheds — the locale
   injection, the runtime injection, the attribute bag, `CssClass` into
   `BuildRootClass`, a composed style into `BuildRootStyle`, `DisposeAsync` into
   `DisposeAsyncCore`, the root reference into `RootElement`, an explicit `Class` or `Id`
   into the base's, and every generated id re-derived from `RuntimeId`. The leaking
   inputs, the locale marker types to delete, the test-side localization registrations
   to remove, and `FormaWindow`'s Guid `Id` becoming `IWindow.RuntimeId` are marked. The ledger is reviewed
   before anything is applied; then the migration, the gate, and a walk of the pages that
   exercise disposal, the windows and the explorer, in both languages. **Built.** The
   tier rule the ledger settled: a component a host places is an element (52), a form
   input is an input (11), and a part that only lives inside its owner and takes nothing
   from a host is plain (43: the explorer's parts, the composer's parts and dialogs,
   `FormaPageHead`, `FormaConfirmDialog`, `FormaToastContainer`). What changed beyond the
   ledger: the explorer's helper types take a plain `IStringLocale`; a field holding a
   child component became a private property, the shape `FormaSelect.Popover` already
   had, because every component is now disposable and CA2213 would otherwise ask the
   holder to dispose what the renderer owns; a generated id is a computed property over
   `RuntimeId`, never a field initializer, which would run before the base's; `FormaTab`'s
   generated id is the full `RuntimeId` rather than eight characters of a random one;
   `FormaTextBox` derives an id whether or not it has a label, and writes its own width
   before a host's style; `FormaComposerPluginsBar` and `FormaCommandBar` put the host
   surface on the element inside their `FormaVisibility`; `FormaTabs` and
   `FormaPageTitle` are sealed like every other component. The test context keeps
   `AddJsonLocalization()`, since nine test classes assert translated text; the per-class
   registrations are gone.
3. **Interop logging.** The required `ILogger` on `JsInterop` and `JsModule`, threaded
   through every subclass, module holder and construction site; the swallowed
   `JSException` and `JSDisconnectedException` catches logged at `Debug` through a
   message class in `Forma.Framework.Core.Interop`. Interop tests construct wrappers with
   `NullLogger.Instance`; script tests are unaffected. **Built**, in the shape step 6
   gives it: the wrappers take the JS environment rather than a logger, and every
   component constructs a wrapper as `new X(JS)`. `InteropLog` carries one message
   per stage, import, call and release, for the dropped circuit and the thrown script,
   and a failed import is reported once, where it happened, never again at release;
   `InputInteropLog` carries the input scope's two, since the scope holds a handle with
   no module path. `InputRouter` and
   `EmojiSpriteLoader`, the two services that own a wrapper, take `ILogger<T>` and
   their registrars `TryAdd` the `NullLogger<>` fallback. The interop tests drive the
   wrappers through `FakeJSRuntime` and `FakeJSObjectReference` in the support project
   and read the entries through `FakeLoggerFactory`.
4. **Feature logging ledger, then feature logging.** Every catch site and every service
   that would take a logger, each with its proposed `Debug` message, one `{Feature}Log`
   class per feature, applied after review.
5. **Docs.** `forma-authoring-components` gains the three bases, `BuildRootClass`,
   `BuildRootStyle`, `DisposeAsyncCore` and a logging section on levels and
   `[LoggerMessage]`; `forma-localizing` gains `L` from the base and
   `LocalizationSourceType`; `forma-scripting` gains the wrapper's logger and notes that
   the silent-failure contract is visible at `Debug`; `forma-testing` drops per-test
   localization registration; `COMPONENTS.md` gains the base rows and tree entries. The
   identifiers rule already carries `RuntimeId` as its seventh kind; the vocabulary and
   the gate are unchanged. **Built for the bases**: the authoring skill has a Bases
   section, the localizing, testing and scripting skills speak of `L`,
   `LocalizationSourceType`, the test context and `DisposeAsyncCore`, and the inventory's
   tree lists the bases. The logging passages land with steps 3 and 4, once there is
   logging to describe; the context, the environment, the style builder and the root
   object land in the skills with steps 6 to 9, and `CLAUDE.md`'s glossary gains
   *component context*, *JS environment* and *root attributes* as each is introduced.
6. **The JS environment.** `IJSEnvironment : IJSRuntime` and `JSEnvironment` in
   `Forma.Framework.Core.Interop`; `JsModule` and `JsInterop` renamed `JSModule` and
   `JSInterop`, each taking `(IJSEnvironment js, string path)` and reading its logger
   and identity from it; every interop message naming the instance; the 31 wrappers,
   `SentinelObserver<TOwner>` and `JsComposerAssetLoader` (renamed `JSComposerAssetLoader`)
   back to one parameter; `InputScope` taking the environment for its log; `InputRouter`
   and `EmojiSpriteLoader` minting their own environment from the runtime and
   `ILogger<T>` they take. The interop tests construct `JSEnvironment` around the fakes,
   and `JSEnvironmentTests` proves the delegation and the two carried values. **Built.**
   Until step 7 lands, each base builds its `JS` from the injected runtime, its `Logger`
   and its `RuntimeId`; the context takes that over. The scripting, authoring and
   testing skills speak of the environment, and the vocabulary defines it.
7. **The component context.** `IComponentContext` and `ComponentContext` beside the
   bases; both bases build `Context` once and forward `RuntimeId`, `JS`, `Logger` and
   `L` to it; the 41 construction sites read `new X(JS)`; the base tests gain one test
   that the environment's identity and log are the context's. A Showcase walk, since
   every component's construction changes. **Built.** `RuntimeId` is minted by the
   context, so it exists from the first read of `Context` and never from a field
   initializer; the three functions are deferred, so a derived initializer that reads
   `Context` no longer runs ahead of injection.
8. **The style builder.** `CssStyleBuilder` in `Forma.Framework.Css` with its tests;
   `BuildRootStyle(CssStyleBuilder)` on both bases; the five overrides,
   `FormaStatusBadge`, `FormaTextBox`, `FormaFrame`, `FormaFreezePane` and `FormaWindow`,
   adding declarations instead of building strings.
9. **The root object.** `RootAttributes` replacing the static `RootAttributeValues`;
   `Root` on both bases with each tier's composition; the 64 razor files, forwarders
   included, on `@Root.Class` and `@Root.Style`; the skills, the inventory's tree and the
   glossary. A Showcase walk, since every root's attributes are composed anew.
