# Forma Component Foundations Design

## Scope

The base classes every Forma component derives from, and the three services they carry:
the browser runtime, logging, and localization. Authoritative for `FormaComponentBase`,
`FormaElementBase`, `FormaInputBase<TValue>`, `IComponentContext` and `IJSEnvironment`,
`CssClassBuilder` and `CssStyleBuilder`, the null objects `UnavailableJSRuntime` and
`NullStringLocaleFactory`, and how logging reaches every assembly of the library.

## Purpose

A base carries what every component of its tier is: the attribute bag the host fills, the
runtime, the strings, the log, the class and style merged from the component's own and the
host's, the root element reference, and a disposal that tears the component's interop
down once. What a component says about itself, a dropped circuit or a decorative asset
that fails to load, reaches a host through the log the host already configures.

> What is every Forma component, what does every input add to that, and how does a
> component say what happened to it without a host having to set anything up?

## Decision summary

- **Three bases, named for what they are.** `FormaComponentBase` is what every
  component is; `FormaElementBase` is a component with a root element its host
  addresses; `FormaInputBase<TValue>` is the framework's `InputBase<TValue>` with the
  same reach, strings, log, host surface and disposal as any other Forma component. The
  `Base` suffix is the .NET shape for an abstract root, as `ComponentBase` and
  `InputBase` are.
- **One tier per placement.** A component a host places is an element; a form input is
  an input; a part that only lives inside its owner and takes nothing from a host, an
  explorer's toolbar or a composer's dialog, is plain. Nothing derives from
  `ComponentBase` or `InputBase<TValue>` directly.
- **The root is the element the host addresses.** On an input that is the control inside
  its `FormaField` (the `<textarea>`, or the trigger `<button>` of a select), never the
  field wrapper; on a composite it is the control the host names and styles (a drop-down
  menu's trigger button, a checkbox's `<input>` inside its label). A wrapper the component
  draws around that control is an internal part, held in a private reference where
  interop needs it. There is no separate control member: the root is the control.
- **Required services, null defaults in the container.** The runtime, the logger factory
  and the locale factory are injected, required. A default on an injected property is
  dead code, since Blazor sets the property after construction and throws when the
  service is missing; the defaults are null objects registered by `AddFormaUI` with
  `TryAdd`, so a host's own registration wins by coming first, as every Forma default
  behaves.
- **A logger per component, under its own type.** `Logger` is created lazily from the
  concrete type through `ILoggerFactory`, so a host filters by
  `Forma.Blazor.Components.Windows.FormaWindow` and nothing else. Interop wrappers take
  the component's JS environment, which carries that logger and the component's identity,
  so `FormaFrame`'s interop logs under `FormaFrame` and names the instance.
- **Strings per component, under its own type unless it says otherwise.** `L` is created
  lazily through `IStringLocaleFactory` from `LocalizationSourceType`, which is the
  component's own type by default and a feature's type where components share a file,
  as `FormaExplorer`'s and `FormaComposer`'s do.
- **Debug for what the library absorbs; anything louder decided per site.** What the
  library swallows by design is logged at `Debug` through `[LoggerMessage]`. Every
  standard provider hides `Debug` unless asked, so a host sees nothing new until it turns
  a category on. `Warning` and `Error` are decided per site, never blanket-applied: a
  plugin assembly that loads only partially is a `Warning`, since it can leave plugins
  unavailable. Normal behavior is silent because it is normal: a superseded delay,
  resolve or fetch is the mechanism working, not a failure absorbed.
- **The host's values in, the root's values out.** `Id`, `Class` and `Style` are what
  the host passed; `RootClass` and `RootStyle` are what the base writes on the root,
  built by `BuildRootClass` and `BuildRootStyle` through the two CSS builders, which
  each base creates, fills and reads in its own getter. `AdditionalAttributes` keeps the
  framework's name, because `InputBase<TValue>` fixes it and the two tiers must read the
  same.
- **One context per component, handed down whole.** `IComponentContext` is what a
  component is given: its identity, its strings, its log and its JS environment. The
  base builds it once and forwards nothing but `L`, so a component reads
  `Context.RuntimeId`, `Context.Logger` and `Context.JS`, and a new thing every component
  needs is added to the context once rather than to a base twice or to every constructor
  that passes it on. `Context` is reserved for it: anything else context-like a component
  holds is named for its subject alone, `Composer` for `FormaComposer`'s parts' cascaded state
  and `JS` for the environment, never `ComposerContext` or `JSEnvironment`.
- **The JS environment is a runtime.** `IJSEnvironment : IJSRuntime` adds the owner's
  `RuntimeId` and `Logger` to the runtime, so a wrapper calls it as the runtime it is
  and logs through it as its owner. It lives in `Forma.Framework.JavaScript.Interop`, which
  knows nothing of components; the component context builds its environment from its
  own identity and log, so the two can never disagree. `JSInterop` and `JSModule` take
  the `JS` casing of `Microsoft.JSInterop` and of every Forma type named for it.
- **Style is built, not concatenated.** `CssStyleBuilder` beside `CssClassBuilder`, so
  `BuildRootStyle` adds declarations the way `BuildRootClass` adds classes, and the
  termination rule lives in one place.
- **Async disposal runs the framework's cleanup.** `FormaInputBase<TValue>.DisposeAsync`
  calls `Dispose(true)`, which the renderer never does on an `IAsyncDisposable`.
- **One identity per instance, and `Id` means the consumer's DOM id everywhere.**
  `RuntimeId` is a `Guid` every component carries for its lifetime: the generated ids
  of its elements derive from it, a log line can name the instance, a window's workspace
  identity is it (`IWindow.RuntimeId`), and the root element carries it as
  `data-rid="@Context.RuntimeId"`. `Id` is the id a consumer gives the root element and
  nothing else, on every component without exception; the two sit side by side on the
  root, `id="@Id" data-rid="@Context.RuntimeId"`, so a consumer's id is never displaced
  by ours.

## The bases

### `FormaComponentBase`

```csharp
public abstract class FormaComponentBase : ComponentBase, IAsyncDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; }
    [Inject] private ILoggerFactory LoggerFactory { get; set; }
    [Inject] private IStringLocaleFactory LocaleFactory { get; set; }

    public bool IsDisposed { get; private set; }

    protected virtual Type LocalizationSourceType => GetType();

    protected IComponentContext Context { get; }   // built once, from the three injections and LocalizationSourceType

    protected IStringLocale L => Context.Locale;

    protected virtual ValueTask DisposeAsyncCore();

    public async ValueTask DisposeAsync();         // sets IsDisposed, then runs DisposeAsyncCore once
}
```

The context and the environment behind `Context`:

```csharp
public interface IComponentContext
{
    Guid RuntimeId { get; }

    IStringLocale Locale { get; }

    ILogger Logger { get; }

    IJSEnvironment JS { get; }
}

public interface IJSEnvironment : IJSRuntime
{
    Guid RuntimeId { get; }

    ILogger Logger { get; }
}
```

`ComponentContext` takes the runtime, the locale and the logger as `Func<T>` and holds
each in a `Lazy<T>`, mints `RuntimeId`, and builds its `JS` once as
`new JSEnvironment(runtime, Logger, RuntimeId)`, so the environment's identity and log are
the context's by construction. `JSEnvironment` delegates the two `InvokeAsync` overloads
to the runtime it wraps; the framework's `InvokeVoidAsync` and the rest are extension
methods on `IJSRuntime` and work on it unchanged. A service that is not a component, such
as the input router or the emoji sprite loader, has no context and builds its own
environment from the runtime and the `ILogger<T>` it takes, minting an identity for the
instance. Synchronous calls are not wrapped: no Forma wrapper makes one. Should one need
to, a second environment, `JSInProcessEnvironment : IJSEnvironment, IJSInProcessRuntime`,
forwards the synchronous calls as well, and the context builds it where the injected
runtime is in-process, so a wrapper tests its environment for `IJSInProcessRuntime`
exactly as the framework tests the runtime.

`RuntimeId` is a version 7 GUID, so instances sort by creation and a log filtered to one
component reads in the order its instances were made. A root that needs an id when the
consumer gives none reads `ElementId` from its base, the consumer's `Id` or one minted
from `Context.RuntimeId`, computed on each read rather than held in a field initializer,
which would run before the base's; every part hangs off that stem by its role,
`PopoverId => $"{ElementId}-popover"`, so a consumer who names the root names its parts
and an unnamed instance's parts all share one stem; and the root element writes the
identity itself as `data-rid="@Context.RuntimeId"`, so a reader of the DOM, a log line
and a workspace all name the same instance the same way.

The logger's category is the concrete type, which for a generic component the logging
library prints without its arguments. The locale's category follows the JSON factory,
which takes a closed generic to its open definition and strips the arity, so
`FormaDataGrid<Row>` reads `FormaDataGrid`'s files. Both are lazy: a component that
never logs and never reads a string pays for neither.

### `FormaElementBase`

```csharp
public abstract class FormaElementBase : FormaComponentBase
{
    [Parameter] public string? Id { get; set; }

    [Parameter] public string? Class { get; set; }

    [Parameter] public string? Style { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? AdditionalAttributes { get; set; }

    public virtual ElementReference RootElement { get; protected set; }

    protected string? RootClass { get; }   // BuildRootClass, then Class; null when nothing was added

    protected string? RootStyle { get; }   // BuildRootStyle, then Style; null when nothing was added

    protected virtual void BuildRootClass(CssClassBuilder builder);

    protected virtual void BuildRootStyle(CssStyleBuilder builder);
}
```

Blazor binds attributes to parameters case-insensitively, so a host's `class="…"` lands
in `Class` and its `style="…"` in `Style`, and neither reaches `AdditionalAttributes`.
The root line of every element component's `.razor` is then the same:

```razor
<div id="@Id" data-rid="@Context.RuntimeId" class="@RootClass" style="@RootStyle" @attributes="AdditionalAttributes" @ref="RootElement">
```

A component that roots in another Forma component forwards the four to the one inside
and overrides `RootElement` to answer with that component's root, so a reader gets the
same element whichever of the two it holds. The identifiers rule holds: `Id` is the
root's id verbatim, and a component whose root needs an id when the host gives none
reads the base's `ElementId`.

`CssClassBuilder` lives in `Forma.Framework.Css` beside `CssColor` and is the one place a
class rule can live: a class is added outright or under a condition, and `ToString()`
joins what was added. `CssStyleBuilder` sits beside it: a declaration the component
composes is added as a property and a value, a string the host supplied is added whole,
and `ToString()` terminates every declaration.

Each base composes its root's values in the getters themselves: it creates the builder,
lets the component fill it, adds the host's value and, on the input tier, the field's
validation class, and reads it through `ToStringOrNull`, null when nothing was added so
the attribute is omitted. The component's own declarations come first and the host's
after, `inline-size: 12ch; color: red;`, and a length is added as a `CssLength`, so it
renders in the invariant culture without the component formatting it. The builder hooks
are the extension points, and the composition rule is the base's.

### `FormaInputBase<TValue>`

```csharp
public abstract class FormaInputBase<TValue> : InputBase<TValue>, IAsyncDisposable
{
    // The three injections, Context and L, LocalizationSourceType, IsDisposed and DisposeAsyncCore
    // as on FormaComponentBase; Id, Class, Style, RootElement, RootClass, RootStyle, BuildRootClass and
    // BuildRootStyle as on FormaElementBase. AdditionalAttributes is InputBase's own. RootClass is built from
    // BuildRootClass, Class and the field's validation class, in that order.

    public async ValueTask DisposeAsync();   // sets IsDisposed, runs DisposeAsyncCore once, then InputBase's Dispose(true)
}
```

C# has one base slot, so the input base repeats what the other two carry rather than
deriving from them. The context carries what both can share, the identity, the strings,
the log and the environment, and the two builders carry the class and style
composition; what stays written out twice is the three injections,
`LocalizationSourceType`, `IsDisposed`, the dispose pair and the host parameters.

A dialog base a composite owns derives from `FormaComponentBase` and overrides
`LocalizationSourceType` once, to the composite's type, for every dialog under it.

## The services

### The null objects

| Contract | Null object | Lives in | Behavior |
| --- | --- | --- | --- |
| `IJSRuntime` | `UnavailableJSRuntime.Instance` | `Forma.Framework.JavaScript.Interop` | Throws `JSRuntimeUnavailableException` on any call. |
| `ILoggerFactory` | `NullLoggerFactory.Instance` | `Microsoft.Extensions.Logging.Abstractions` | Discards. |
| `ILogger<T>` | `NullLogger<T>` | `Microsoft.Extensions.Logging.Abstractions` | Discards; for services that inject a typed logger. |
| `IStringLocaleFactory` | `NullStringLocaleFactory.Instance` | `Forma.Framework.Localization` | Every key is its own text, holes and plurals resolved for the source language, as the JSON locale does when no file matches. |

`AddFormaUI` registers all four with `TryAdd`. A host that wants translations calls
`AddJsonLocalization()` first, as the Showcase does; a host with its own logging or its
own runtime has it in place before `AddFormaUI` runs, as ASP.NET Core's are. A feature
registrar whose services log also `TryAdd`s the logger fallback for them, the way `Timing`
registers `TimeProvider.System`, so a feature stays usable on its own.

The runtime's null object throws rather than discards because a component that reaches
the browser without one is misconfigured, and a clear exception at the first interop
call beats a "no registered service" throw at construction. It applies where no runtime
exists at all: a bare container in a framework test, tooling, or `HtmlRenderer` rendering
a component to a string. In a Blazor host the framework's runtime is registered first and
the null object never takes effect.

`NullStringLocaleFactory` is the name of the null object for the factory; the locales it
makes are not empty but source-only, the behavior a JSON locale has for a key with no
translation.

### Logging

Every assembly that logs takes `Microsoft.Extensions.Logging.Abstractions` from the
target framework, which supplies it and the `[LoggerMessage]` generator to every
project, so no project references the package. Nothing references the concrete logging
package; a host's providers are the host's.

A component logs through `Logger`. A service that is not a component takes
`ILoggerFactory` or `ILogger<T>` through its constructor, required. An interop wrapper,
`JSInterop` or `JSModule` and every subclass, takes the owner's `IJSEnvironment`, which
carries the owner's logger and identity, so everything about a frame, its interop
included, lands under `Forma.Blazor.Components.Layout.FormaFrame`, and every interop
message names the instance it was about. A wrapper reports a failure at the stage it
happened, import, call or release, and a failed import is reported once, where it
happened, never again at release.

Messages are source-generated, one `internal static partial class {Feature}Log` per
feature, so every message a feature can emit is read in one place:

```csharp
internal static partial class EmojiLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Sprite sheet {Sheet} did not load for {RuntimeId}.")]
    public static partial void SpriteSheetDidNotLoad(ILogger logger, string sheet, Guid runtimeId);
}
```

What the library swallows by design is logged at `Debug`: the dropped circuit and thrown
script behind a `Try*` interop call, a decorative asset that would not load, a plural
expression that fell back. The `Try*` contract itself is unchanged: the call still
returns `false` or `default`, and the silence is visible to whoever turns the category
on. A message names the event and keeps application and user data out: a plural argument
is reported by its name and type, never its value. What is normal stays silent: a
superseded delay, resolve or fetch, a disposal race, a failure the component already
hands to its host as a domain object, a discarded `Try*` result the wrapper already
reports, and a missing locale file, because the locale walks the culture chain and
cannot tell an ordinary step to the parent from a missing expected file.

## What the bases do not carry

- **The self reference.** `DotNetObjectReference<T>` exists only for interop callbacks
  and is created by the components that need one. The base has no `T`.
- **Generated ids.** They belong to the elements that need them and vary by element; a
  component keeps its own computed properties and derives each from `Context.RuntimeId`,
  as the identifiers rule prescribes.
- **The input scope.** A component that registers with the input router does so itself.
- **Anything a subset needs.** The line to hold is that a base carries what every
  component of its tier is, never what some need; the rest is composition.

## Non-goals

- A Forma-specific logging abstraction. `ILogger` is the abstraction every host already
  configures and filters.
- Logging on the script side beyond its one `console.error` for the rare script-side
  failure; the gap this design closes is on the .NET side.
- Retiring `IStringLocale<T>`. It stays for services and hosts.
- A root object, `Root.Class` and `Root.Style`, and the splatted attribute dictionary it
  would own. With the class and style built through the two builders, such an object holds
  two strings and no rule of its own, and `RootElement` stays outside it either way, since
  forwarders override it and `@ref` binds it. It is worth having only alongside the splat,
  which is where a precedence rule would live: the renderer lets a later attribute win and
  omits a null one, so the splat would work, but it turns the attribute-order rule into a
  precedence rule that has to be written down first, and that is its own change.
