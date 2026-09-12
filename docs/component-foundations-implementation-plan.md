# Forma Component Foundations Implementation Plan

The plan for building the bases and services that [the component foundations design](component-foundations-design.md) states, and the record of what landed at each step.

## Status

Every step of the implementation plan is built: the builders, the null objects, the
three bases and their fallback registrations exist, every component derives from a base,
interop logs through the JS environment, every component reads its context, a root's
style is built the way its class is, and the failures the features absorb are logged.
The steps below record what landed at each and where it departed from the sketches in
the design. Two departures apply to the sketches themselves: the root's
class and style are `string?`, null when there is nothing to write, so an element
carries no empty `class` or `style` attribute; and the root reference is named
`RootElement`.

Reviewing step 3 changed the design: threading one logger touched every wrapper, every
construction site and two services, so what a component hands down is now one object,
its context, and what a wrapper takes is one object, the JS environment inside it.
Steps 6 to 8 carry that revision, the context, the environment and a style builder to
match the class builder; step 9, a root object over the class and style, was withdrawn
once the builders left it nothing to own.

## What the survey found

The design rests on a survey of the 106 components of `Forma.Blazor` as of this
document, not on impression.

| Fact | Consequence |
| --- | --- |
| No component renders nothing of its own: 58 own one root element, 34 root in another Forma component and forward the host's attributes to it, the rest wrap an owned element in a cascade or beside a sibling. | The plain tier exists for the few that take nothing from a host; the element tier is nearly everyone. |
| 61 components inject a locale; 31 of them deliberately read a feature's file, `FormaExplorer`'s twenty parts sharing its file and `FormaComposer`'s sharing its own. | `L` cannot be made from the component's own type alone; `LocalizationSourceType` is the override. |
| Generic components inject a locale for a non-generic marker type, five of them, because `IStringLocale<T>` needs a non-generic argument; the JSON factory already resolves a closed generic to its open definition's files. | By-type creation retires the marker types. |
| 47 test classes register localization in their own constructor because the test context does not. | With a null factory registered by `AddFormaUI`, the test context inherits it and the 47 registrations go. |
| 29 components inject `IJSRuntime`, 41 sites construct an interop wrapper by hand, 32 implement `IAsyncDisposable`, 16 hold a `DotNetObjectReference` to self. | The runtime goes on the base; the self reference stays with the interop-owning components, since it is theirs alone. |
| The renderer, decoded from the installed framework, tests `IAsyncDisposable` first and returns without touching `IDisposable`. | Four inputs that add `IAsyncDisposable` leak their `EditContext` subscription today; the input base closes it. |
| Nothing in `src/` logs: no `ILogger`, no console writes in C#, one `console.error` in a script. Every catch either swallows by design or hands the exception to the host as a domain object. | Logging is new ground, and the first messages are the swallowed catches. |

## Weighed and set aside

A root object holding the two values,
`Root.Class` and `Root.Style`, was weighed and set aside: it would hold two strings and
no rule of its own, and `RootElement` would stay outside it, since forwarders override
it and `@ref` binds it. It comes back only with a splatted attribute dictionary, which
is where a precedence rule would live. That splat was weighed too: the renderer lets a
later attribute win and omits a null one, so it would work, but it turns the
attribute-order rule into a precedence rule that has to be written down first, and that
is its own change.

## Migration notes

`ComposerDialogBase` re-bases onto `FormaComponentBase`, since a dialog takes nothing
from a host, overrides `LocalizationSourceType` once to `typeof(FormaComposer)` for its
dialogs, and keeps its name.

`FormaCheckBox` and `FormaDatePicker` were element-tier components, not form inputs.
Both are inputs now: the checkbox binds `Value` like `FormaToggleSwitch`, and the
library's control-style uses of it name a field through `ValueExpression`; the date
picker binds one `DateOnly?`, and its range mode became `FormaDateRangePicker`,
binding one `DateRange` — a value type in `Models/` — over a shared `FormaCalendar`
that carries the month mechanics while each picker owns its selection rules.

## Steps

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
   exercise disposal, `FormaWindow` and `FormaExplorer`, in both languages. **Built.** The
   tier rule the ledger settled: a component a host places is an element (52), a form
   input is an input (11), and a part that only lives inside its owner and takes nothing
   from a host is plain (43: `FormaExplorer`'s parts, `FormaComposer`'s parts and dialogs,
   `FormaPageHead`, `FormaConfirmDialog`, `FormaToastContainer`). What changed beyond the
   ledger: `FormaExplorer`'s helper types take a plain `IStringLocale`; a field holding a
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
   component constructs a wrapper as `new X(Context.JS)`. `InteropLog` carries one message
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
   class per feature, applied after review. **Built.** Five sites log: a sprite sheet
   that did not load, reported once by the loader that forgets it; a composer asset
   that did not load; a sprite sheet whose load threw, in `FormaEmojiPicker`, with the
   exception; a plural argument that is not a count, once per format, naming the
   argument and its type but never its value; and a plugin assembly that loaded only
   partially, at `Warning`, with the loader exceptions, through an optional logger on
   `ComposerPluginDiscovery.Discover`. A missing translation file stays silent, because
   the locale walks the culture chain and the cache cannot tell an ordinary step to the
   parent from a missing expected file. The superseded delays, resolves and fetches, the
   uploader's disposal race and its failures handed to the host through `OnFailed`, the
   satellite probes, and the discarded `Try*` results a wrapper already reports stay
   silent by design. `JsonStringLocaleFactory` takes an `ILoggerFactory` and is
   registered through a factory delegate, with the null factory as its fallback.
5. **Docs.** `forma-authoring-components` gains the three bases, `BuildRootClass`,
   `BuildRootStyle`, `DisposeAsyncCore` and a logging section on levels and
   `[LoggerMessage]`; `forma-localizing` gains `L` from the base and
   `LocalizationSourceType`; `forma-scripting` gains the wrapper's logger and notes that
   the silent-failure contract is visible at `Debug`; `forma-testing` drops per-test
   localization registration; the component catalog gains the base rows and tree entries. The
   identifiers rule already carries `RuntimeId` as its seventh kind; the vocabulary and
   the gate are unchanged. **Built**: the authoring skill has a Bases
   section, the localizing, testing and scripting skills speak of `L`,
   `LocalizationSourceType`, the test context and `DisposeAsyncCore`, and the inventory's
   tree lists the bases. The logging passages landed with steps 3 and 4; the context, the environment, the style builder and the root
   object land in the skills with steps 6 to 9, and the vocabulary gains
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
   bases; both bases build `Context` once and forward `L` alone, so a component reads
   `Context.RuntimeId`, `Context.Logger` and `Context.JS`; the 41 construction sites read
   `new X(Context.JS)` and every root writes `data-rid="@Context.RuntimeId"`; the base tests
   gain one test that the environment's identity and log are the context's. A Showcase
   walk, since every component's construction changes. **Built.** `RuntimeId` is minted
   by the context, so it exists from the first read of `Context` and never from a field
   initializer; the three functions are deferred, so a derived initializer that reads
   `Context` no longer runs ahead of injection. `FormaWindow` answers `IWindow.RuntimeId`
   explicitly. `FormaComposer`'s parts' cascaded state, which was also named `Context`, is
   `Composer`, under the rule that `Context` is the component's and anything else
   context-like is named for its subject alone.
8. **The style builder.** `CssStyleBuilder` in `Forma.Framework.Css` with its tests;
   `BuildRootStyle(CssStyleBuilder)` on both bases; the five overrides,
   `FormaStatusBadge`, `FormaTextBox`, `FormaFrame`, `FormaFreezePane` and `FormaWindow`,
   adding declarations instead of building strings. **Built.** A root's style is every
   declaration terminated and space-separated, the component's own first and the host's
   after, `inline-size: 12ch; color: red;`, whichever of the two supplied them; a length
   is added as a `CssLength`, so a placement renders in the invariant culture without a
   component formatting it.
9. **The root object.** Withdrawn. With the class and style built through the two
   builders, the object would hold two strings and no rule of its own, and `RootElement`
   would stay outside it. The static helper that composed the values went with it: each
   base creates, fills and reads its builders in the `RootClass` and `RootStyle` getters,
   and the builders gained `ToStringOrNull` for the omitted attribute. The object
   returns with the splatted dictionary, if that is ever decided.
