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
- **One tier per responsibility.** A component that draws a root its host addresses is an
  element, whether that root is its own element or another component's it forwards the host
  surface to, as a confirm dialog forwards it to its dialog; a form input is an input; every
  other component is plain, a part that only lives inside its owner, an explorer's toolbar or
  a composer's dialog, and a component that draws no root of its own, a page's head or a
  grid's column. Nothing derives from `ComponentBase` or `InputBase<TValue>` directly.
- **The root is the element the host addresses.** On an input that is the control inside
  its `FormaField` (the `<textarea>`, or the trigger `<button>` of a select), never the
  field wrapper; on a composite it is the control the host names and styles (a drop-down
  menu's trigger button, a checkbox's `<input>` inside its label). A wrapper the component
  draws around that control is an internal part, held in a private reference where
  interop needs it. There is no separate control member: the root is the control.
- **Required services, never a stand-in for them.** The runtime, the logger factory and the
  locale factory are what every component needs, and each is supplied by another registrar,
  the platform's or a localization package's, with `TryAdd`. The bases resolve the runtime and
  the locale factory where they are registered and fall back to their null objects where they
  aren't, and the logger factory is the platform's, which `AddFormaUI` registers through
  `AddLogging`, so no registration of Forma's stands in for the real one, and the order a host
  registers in changes nothing.
- **Every render mode.** A component works under static server rendering, Interactive Server,
  WebAssembly and Auto, and Blazor Hybrid; under a static render it shows its state and offers
  no control whose action needs interactivity, as [render modes](#render-modes) sets out.
- **A logger per component, under its own type.** `Context.Logger` is created lazily from the
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
  culture state that can't register with the page's coordinator is a `Warning`, since the
  page's switches then leave its components behind. Normal behavior is silent because it is normal: a superseded delay,
  resolve or fetch is the mechanism working, not a failure absorbed.
- **The host's values in, the root's values out.** `Id`, `Class` and `Style` are what
  the host passed; `RootClass` and `RootStyle` are what the base writes on the root,
  built by `BuildRootClass` and `BuildRootStyle` through the two CSS builders, which
  each base creates, fills and reads in its own getter. `AdditionalAttributes` keeps the
  framework's name, because `InputBase<TValue>` fixes it and the two tiers must read the
  same.
- **One context per component, handed down whole.** `IComponentContext` is what a
  component is given: its identity, its cultures, its strings, its log, its JS environment,
  and the DOM watcher and engine its browser work goes through. The base builds it once and
  forwards nothing but `L`, so a component reads `Context.RuntimeId`, `Context.CultureState`,
  `Context.Logger`, `Context.JS`, `Context.DomWatcher` and `Context.DomEngine`, and a new
  thing every component needs is added to the context once rather than to a base twice or to
  every constructor that passes it on. `Context` is reserved for it: anything else context-like a component
  holds is named for its subject alone, `Composer` for `FormaComposer`'s parts' cascaded state
  and `JS` for the environment, never `ComposerContext` or `JSEnvironment`.
- **The JS environment is a runtime.** `IJSEnvironment : IJSRuntime` adds the owner's
  `RuntimeId` and `Logger` to the runtime, and the `TransferOptions` that bound what the
  browser sends the owner through the [browser transfer](browser-transfer-design.md), so a
  wrapper calls it as the runtime it is and logs through it as its owner. It lives in
  `Forma.Framework.JavaScript.Interop`, which knows nothing of components; the component
  context builds its environment from its own identity and log, so the two can never
  disagree. `JSInterop` and `JSModule` take the `JS` casing of `Microsoft.JSInterop` and of
  every Forma type named for it.
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

`FormaComponentBase` injects the logger factory, the service provider and the culture
state, all required, and resolves the runtime and the locale factory from the provider,
falling back to their null objects, as [the services](#the-services) set out. It subscribes
to the state, and builds the component's context once from them and its
`LocalizationSourceType`, finding the user scope's DOM bridge where one is registered. It
gives the component its strings as `L`, reports whether it is disposed, and runs
`DisposeAsyncCore` once on disposal, then releases a context it built, so the component's
own cleanup can still use it. The context carries the component's identity, cultures,
strings, log and JS environment, and the DOM watcher and engine it makes on first use, whose
observations and interactions its release ends in one call to the browser.

A component writes in the cultures of its user scope's culture state, which its context carries
as `Context.CultureState`, the one instance the user scope holds: its strings come from the
state's UI culture, and each change of the state renders it again, even where its parameters are
unchanged. The [culture design](culture-design.md) sets out the state, how a change reaches each
component, and how the document and the address follow it.

The context builds its JS environment once from the runtime, its logger, its identity and
the transfer options the host registered, or their default, so the environment's identity
and log are the context's by construction. The environment
carries the runtime's asynchronous calls, which are the ones every Forma wrapper makes. A
service that is not a component, such as the input router or the emoji sprite loader, has
no context and builds its own environment from the runtime and the `ILogger<T>` it takes,
minting an identity for the instance.

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

`FormaElementBase` adds the host surface: `Id`, `Class`, `Style` and the captured
attributes the host passes, `RootElement` once the root is rendered, and `RootClass` and
`RootStyle`, which the component builds through the `BuildRootClass` and `BuildRootStyle`
hooks and the base follows with the host's own values, omitting an attribute nothing was
added to.

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

`FormaInputBase<TValue>` derives from the framework's `InputBase<TValue>` and carries what
the other two do: the injections, the context and `L`, the disposal, and the host surface,
with `AdditionalAttributes` the framework's own and the field's validation class following
the component's and the host's classes on the root. Its disposal runs `InputBase`'s
`Dispose(true)` after `DisposeAsyncCore`, then releases the context.

C# has one base slot, so the input base repeats what the other two carry rather than
deriving from them. The context carries what both can share, the identity, the strings,
the log and the environment, and the two builders carry the class and style
composition; what stays written out twice is the injections,
`LocalizationSourceType`, `IsDisposed`, the dispose pair and the host parameters.

A dialog base a composite owns derives from `FormaComponentBase` and overrides
`LocalizationSourceType` once, to the composite's type, for every dialog under it.

## The services

### The null objects

| Contract | Null object | Behavior |
| --- | --- | --- |
| `IJSRuntime` | `UnavailableJSRuntime.Instance` | Throws `JSRuntimeUnavailableException` on any call. |
| `IStringLocaleFactory` | `NullStringLocaleFactory.Instance` | Every key is its own text, holes and plurals resolved for the source language, as the JSON locale does when no file matches. |

No registration Forma makes stands in for a service another registrar supplies, since that
registrar adds its own with `TryAdd`, as a web view's registrar adds the runtime and a
localization package its factory, and a stand-in registered first would keep it out. So:

- **The bases fall back themselves.** A base resolves the runtime and the locale factory where
  they are registered and falls back to their null objects where they aren't; a service built on
  the runtime resolves it the same way, through `GetJSRuntime`.
- **A registrar whose services log registers the platform's logging,** `AddLogging`, whose
  own registrations are `TryAdd`s, so a typed logger resolves in every host, the host's
  providers reach it whichever registrar runs first, and a feature stays usable on its own,
  as the events registrar's `TimeProvider.System` keeps it.
- **A contract a package supplies,** the emoji and icon providers an artwork pack registers, is
  resolved where it is registered, and its consumer falls back to the library's default where it
  isn't, the reader's font for an emoji and the bundled glyphs for an icon.

Each holds in either order, and a host's own registration made first still wins.

The runtime's null object throws rather than discards because a component that reaches
the browser without one is misconfigured, and a clear exception at the first interop
call beats a "no registered service" throw at construction. It applies where no runtime
exists at all: a bare container in a framework test, tooling, or `HtmlRenderer` rendering
a component to a string. In a Blazor host the framework's runtime is registered and the
null object never takes effect.

`NullStringLocaleFactory` is the name of the null object for the factory; the locales it
makes are not empty but source-only, the behavior a JSON locale has for a key no locale file
localizes.

### Logging

Every assembly that logs takes `Microsoft.Extensions.Logging.Abstractions` from the
target framework, which supplies it and the `[LoggerMessage]` generator to every
project, so no project references the package. A registrar whose services take a typed
logger references `Microsoft.Extensions.Logging` for `AddLogging` alone, which registers
no provider; a host's providers are the host's.

A component logs through `Context.Logger`. A service that is not a component takes
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
internal static partial class EmojiSpriteLoaderLog
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

## Render modes

Every component works in every render mode Forma supports: static server rendering, Interactive
Server, Interactive WebAssembly, Interactive Auto on either of its runtimes, and Blazor Hybrid's
web view. The interactive ones run the same component the same way, as the
[WebAssembly design](webassembly-design.md) and the [WebView design](webview-design.md) set out.
A static render runs no event and no script, so under it a component renders its state as
markup a reader can read, its accessible names, roles and states included, and offers no control
whose action needs interactivity: such a control renders disabled, as the button does with a
click handler, keeping the look that carries its state, while a link, a native form control and
a form's submit keep what the browser gives them. A component reads the render it is in from the framework's `RendererInfo`, and
nothing it starts in a static render waits on the browser.

A field posts from a plain form under the name Blazor's own inputs write for its bound value, and
in the form Blazor's form binding reads, which is the invariant culture's alone. A field whose
control a script draws, a number, a date, a time or a choice, draws the browser's own control
under a static render instead, which shows the value in the reader's culture and posts the
invariant form. A choice posts its options' values where the binding reads the value's type back
from text, and keeps its disabled trigger otherwise. A date range keeps its committed range in a
hidden input, as ISO 8601's interval of its two dates, which `DateRange` reads back, and a date
and time with an offset, which the browser's control holds no part of, shows it disabled and keeps
it in a hidden input. A field the application disables posts nothing, as a disabled control does,
and a field that posts nothing, a choice left empty among them, leaves the model's own default in
its place.

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
- Logging on the script side beyond a `console.error` for the rare script-side failure;
  the gap this design closes is on the .NET side.
- Retiring `IStringLocale<T>`. It stays for services and hosts.
- A root object, `Root.Class` and `Root.Style`, owning the splatted attributes. With the
  class and style built through the two builders, such an object holds two strings and no
  rule of its own, and `RootElement` stays outside it, since forwarders override it and
  `@ref` binds it.
