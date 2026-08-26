# Forma.UI.Blazor

A reusable Blazor **component library**. **.NET 11 · C# latest · Razor Class Library · Interactive Server · JS-free by default.**

## Task guides

Detailed standards live in `.claude/skills/` and load on demand. Read the matching guide before starting that kind of work — each is authoritative for its area.

| Skill | Read it before |
|---|---|
| `forma-legacy-coi` | Designing any component that replaces a legacy Coi admin screen or widget |
| `forma-js-interop` | Writing or changing anything under `wwwroot/scripts/` or `Interop/` |
| `forma-a11y-touch-rtl` | Finishing or reviewing a component — the three gates it must pass |
| `forma-localization` | Adding user-facing strings or `.he.json` / `.es.json` files |
| `forma-tests` | Writing bUnit tests or `wwwroot/scripts/` Vitest tests |
| `forma-showcase` | Adding or updating a Showcase page |

## Source of truth

- Do use [COMPONENTS.md](COMPONENTS.md) for the component inventory, build order, and completion status.
- Do use [README.md](README.md) for host consumption — install, service registration, stylesheets, and localization from the consumer's side.
- Do use [docs/composer-design.md](docs/composer-design.md) for `FormaComposer`'s architecture, plugin model, assemblies, and phases.
- Do put a component's own design document in `docs/` when it is large enough to own a plan, and treat it as authoritative for that component over the inventory's summary row.
- Do read `.editorconfig` before writing C#; it is the authority on style, and the root file and `src/Forma.UI.Blazor/.editorconfig` differ deliberately.
- Do use `EpicaDeportiva/docs/forma-components.md` for Épica Deportiva's required and optional subset and remaining work.
- Do treat **Coi** (`c:\Users\eyal\Development\Projects\Coi`) and **Épica Deportiva** (`c:\Users\eyal\Development\Projects\EpicaDeportiva`) as the current consumers.
- Don't invent scope, status, packaging, or style rules when an authoritative file defines them.

## Solution structure

- Do split the family by audience: `Forma.UI.*` is one product serving the component library, while each `Forma.Framework.*` assembly stands alone — `Forma.Framework.Resources.Office` depends on `Forma.Framework.Resources.Abstractions` alone, never on anything in `Forma.UI`.
- Do put a horizontal concern no single vertical feature owns into the matching `Forma.UI.Design.*` project when only the UI needs it, and into `Forma.Framework.Resources.Abstractions` when a consumer outside the UI needs it too.
- Do decide by ownership, not by how many assemblies consume it: a contract a single feature owns stays with that feature in `Forma.UI.Features.Abstractions` no matter how many projects reference it, such as `IToastService` in `Feedback`.
- Do keep a folder and its namespace in step, so `Forma.UI.Blazor/Icons/` holds `Forma.UI.Blazor.Icons` and `Forma.UI.Features.Abstractions/Feedback/` holds `Forma.UI.Features.Feedback` — the namespace is the product root plus the folder path, with the `.Abstractions` suffix dropped.

- Do make components depend on abstractions such as `IToastService`; let the host choose implementations through DI.
- Do represent colours and other CSS values with typed values such as `CssColor.FromRgba(...)` and `CssColor.FromHex(...)`.
- Do share the `Forma.UI.Features` namespace across `Forma.UI.Features` and `Forma.UI.Features.Abstractions` — the assembly name carries the split, not the namespace (matches `Microsoft.Extensions.DependencyInjection` / `.Abstractions`).
- Do give every project outside such a pair a namespace equal to its assembly name, so `Forma.UI.Blazor` holds `Forma.UI.Blazor.*` and `Forma.UI.Design.Icons` holds `Forma.UI.Design.Icons.*`.
- Do keep these identities distinct: assembly and namespace `Forma.UI.Blazor`, package ID `2Site.Forma.UI.Blazor`, static asset base path `_content/Forma.UI`.
- Do keep `Forma.UI.Blazor` the only packable project; every other project sets `IsPackable=false`, and the ones it references ship inside its package through `PrivateAssets="all"`.
- Don't reference `Forma.UI.Features` from `Forma.UI.Blazor`; it takes `Forma.UI.Features.Abstractions`, and the two names differ only by suffix so the reference is easy to get wrong.
- Don't represent typed CSS values as arbitrary strings.
- Don't give any other project a `PackageId`, and don't set `AssemblyName` where it would only repeat the project name.
- Don't rename or conflate these identities; `_content/Forma.UI` is pinned and hardcoded by markup and JS, and deliberately does not track the namespace.

## Dependencies

A project's references are part of its contract, because every consumer inherits them.

- Do reference the narrowest package that supplies what the code uses, preferring the `.Abstractions` half when only contracts are needed.
- Do confine `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to projects that genuinely compile against ASP.NET Core.
- Do give every project its own reference for what it uses directly, test projects included, rather than relying on one arriving transitively.
- Do let the build settle it: NU1510 marks a package the framework already supplies, and an unresolved type marks one that is genuinely missing.
- Don't reach for a shared framework to obtain a handful of types; `Forma.Framework.Localization` needs `Microsoft.Extensions.Localization.Abstractions` alone.
- Don't keep a reference no code uses.

## Naming

- Do prefix component tags with `Forma`: `<FormaModal>`, `<FormaDataGrid>`.
- Do prefix CSS classes with `forma-`: `.forma-toggle-switch-track`.
- Do leave C# services, enums, and records unprefixed because namespaces disambiguate them: `ToastService`, `ModalSize`, `BreadcrumbItem`.
- Do give a supporting type its owning folder's name as a prefix only where that reads well and adds meaning, as `ExplorerAction`, `ExplorerContext`, and `ExplorerSelection` do against `System.Action` and a bare `Context`; `Collections/` needs no `CollectionsXxx` and takes none.
- Do find a better name when a prefix is needed to disambiguate but reads badly.
- Do call a thing Forma ships a **component**, and raw HTML an **element** — one word per concept. "Control" is Web Forms vocabulary inherited from Coi; keep it only in `aria-controls` and in descriptions of Coi's own markup.
- Don't apply one naming convention indiscriminately across Razor tags, CSS classes, and C# types.

### Booleans, events, and callbacks

- Do name a `[Parameter] public bool` bare and unprefixed when it configures the component's own rendering or behaviour: `Disabled`, `Busy`, `Opened`, `Required`, `ReadOnly`, `ShowLabels`, `EnablePagination`, `Virtualize`. One forwarding to a native HTML attribute belongs to the same family as `Class`, `Id`, and `Style`, and `Disabled` holds across every component without exception — never `IsDisabled`.
- Do use the affirmative `Is`/`Has`/`Can` phrasing where the flag states a condition rather than configuring the component: on every plain data record (`FormaToolbarItem.IsDisabled`, `FormaToolbarToggle.IsActive`, `BreadcrumbItem.IsCurrent`, `ExplorerAction.IsDestructive`), and on the few parameters that only reflect something the host already worked out (`FormaExplorerAddressBar.CanGoBack`, `FormaDropDownMenuItem.IsSelected`).
- Do prefix an `EventCallback` with `On` when it reports an action taken, with no bindable parameter of the same name behind it: `OnClick`, `OnCommand`, `OnDelete`, `OnRefresh`.
- Do drop the `On` prefix entirely when an `EventCallback<T>` is paired with a same-named `[Parameter] T`, so `@bind-{Name}` can find it: `Value`/`ValueChanged`, `ActiveTabId`/`ActiveTabIdChanged`, `SelectedItems`/`SelectedItemsChanged`, `ViewMode`/`ViewModeChanged`.
- Don't name a paired changed-callback `On{Name}Changed`; Blazor's binding convention looks for the bare `{Name}Changed`, so `@bind-{Name}` fails outright rather than falling back to the `On`-prefixed one.

## Identifiers

An identifier-shaped member is one of six different things, numbered below, and the name has to say which, because "Id" alone reads as a promise that whatever it names is literally this component's own HTML `id` attribute.

1. Do reserve a component's own `Id` parameter for the verbatim value of its root or primary element's `id` attribute — a pass-through, like `Class` and `Style`: `FormaTextBox.Id`, `FormaComposer.Id`, `FormaComposerPluginsToolbar.Id`, `FormaSearchBox.Id`.
   - Do add a private `_generatedId` and a computed `ElementId => Id ?? _generatedId` where the element needs an id even when the caller supplies none, as `FormaTextBox` and `FormaComposer` do for a label to point at; leave the parameter to stand alone where an absent id is simply absent.
   - Do give a component a single private `_id` where the generated id belongs to its own primary element and no caller can override it: `FormaColorPicker`, `FormaPasswordBox`, `FormaSlider`, `FormaTextArea`, `FormaToggleSwitch`.
   - Do name a generated id after the sub-element it identifies instead, however many there are, whenever it is not the component's own primary element: `_triggerId` and `_popupId` in `FormaSelect` and `FormaMultiSelect`, `_labelId` and `_noteId` in `FormaVideoPlayer`, and a lone `_popupId` in `FormaDropDownMenu` or `_pageInputId` in `FormaPagination`.
   - Do say `ElementId` in the name of anything producing a DOM id that a `{Noun}Id` (2) of the same noun could be mistaken for, as `FormaTabs.GetTabElementId` must against `FormaTab.TabId`.
   - Don't give a component two `_id`-shaped fields.
2. Do name a component parameter `{Noun}Id` when it identifies which item among a set this is, or which one a caller is selecting, even where that value is not rendered anywhere verbatim: `FormaTab.TabId`, `FormaTabs.ActiveTabId`/`ActiveTabIdChanged`, `FormaComposer.ActiveEditorId`/`ActiveEditorIdChanged`.
   - A bare `Id` here would misread as (1), the way `FormaTab.Id` once did before a controlled `FormaTabs` matched it against `ActiveId` and derived a *different*, prefixed DOM id from it.
   - Do fall back to a private `_generatedId` where the caller may omit it, behind a computed member named for this kind rather than `ElementId`: `FormaTab.EffectiveTabId => TabId ?? _generatedId`.
3. Do leave `Id` bare on a plain data record where the whole record IS the identified thing, the way a primary key column would be: `ExplorerAction.Id`, `FormaToolbarItem.Id`, `ComposerToolbarItem.Id`, `ToastMessage.Id`, `IComposerPlugin.Id`.
   - The ambiguity between (1) and (2) is a components-only problem; a record has no DOM element to be confused with.
4. Do name a parameter `TargetId` (or `{Purpose}Id`) when it names *another* element's id that this component points at, such as a `for` target: `FormaField.TargetId`.
5. Do name a lookup or re-render key `{Noun}Key`, not `Id`, when it exists for Blazor's own benefit — `@key`, virtualization, or re-associating a re-rendered row with its prior state — rather than to name the domain item: `FormaDataGrid.ItemKey`, `FormaSortableList.ItemKey`.
   - A key's `Func<T, object>` need not read any real identifier field on `T` at all.
6. Do call a value `Token`, not `Id`, when its only job is correlating a rendered element with the JS side of an interop call, as `FormaDataGrid`'s row-selection `RowTokenTable` does; it is never a domain identity and a host never sees it.

## Comments and documentation

These rules govern prose that *describes a thing*. They do not apply to this file, or to documents whose purpose is to teach judgment or record decisions.

- Do state only what a thing is or does in XML docs, CSS banners, inline comments, and README files.
- Do use US English spelling in documentation and comments — color, license, catalog — with one deliberate exception: behaviour keeps its British spelling.
- Do comment only when the code cannot express an important constraint, such as a necessary `!important`, a non-obvious `-webkit-` property, or an off-by-one invariant.
- Do keep comments to one line whenever possible.
- Do add a one-line XML `<summary>` describing what every public parameter and public type is.
- Do put `/// <inheritdoc />` on every lifecycle override and interface implementation — `OnInitialized`, `OnParametersSet`, `OnAfterRenderAsync`, `Dispose`, `DisposeAsync`, `TryParseValueFromString`, `FormatValueAsString` — rather than restating what the base member already documents.
- Do keep historical origin and usage mapping in the intentional *Old pattern* and *Used in* rows of `COMPONENTS.md`.
- Don't document history, rejected alternatives, decision arguments, roadmap position, call sites, mirrors, or other cross-references.
- Don't restate obvious code or pad files with unnecessary prose.

### Keeping prose true as code changes

A description is wrong the moment the thing it describes widens, and nothing in the build will say so. Re-read the prose around whatever you touch.

- Do describe a member at the full breadth of its type, so a parameter typed `IReadOnlyList<FormaToolbarItem>` is "the items the toolbar renders", never "the action buttons" — that hierarchy also holds groups, menus, toggles, readouts, and custom items.
- Do re-check a class-level summary naming what a component is built on whenever its markup changes; "built on `FormaCommandArea` and `FormaCommandBar`" survived long after the markup used only one of them.
- Do drop a "no JavaScript" or "pure CSS" banner the moment the component gains interop, and keep it where it is still true.
- Do rewrite a `COMPONENTS.md` **Parameters** row when a parameter is added, removed, renamed, or changes type or required-ness.
- Do rewrite a **Blazor approach** row once the component is built, so it records what shipped rather than the plan; a row still reading "Use Blazor `InputDate`" for a bespoke calendar is worse than no row.
- Do update a JS module path in prose when the script moves between `behaviours/`, `operations/`, and `components/`.
- Do delete a sentence saying another component *should* adopt something once it has.
- Don't trust a search for renamed identifiers to find this; the drift is in wording that still compiles and still reads plausibly.

## Component file layout

Components live in `src/Forma.UI.Blazor/Components/{Category}/`, where `{Category}` names a UI role such as `Bars`, `Feedback`, `Inputs`, or `Navigation`.

```text
Components/{Category}/
├── FormaX.razor          # markup only; @namespace first
├── FormaX.razor.cs       # all component logic
├── FormaX.razor.css      # isolated CSS with banner
├── FormaX.he.json        # translations, when needed
└── FormaX.es.json
```

- Do keep markup in `.razor`, logic in `.razor.cs`, and component styling in `.razor.css`.
- Do give a component a category of its own, such as `Explorer`, when it is large enough to grow sub-components and supporting types of its own; the category then owns its folder and namespace.
- Do place supporting types such as `ModalSize`, `BreadcrumbItem`, and `SortableDragFrom` beside the component that owns them.
- Do put script a component needs in `wwwroot/scripts/`, never beside the component.
- Do add the translation pair only when the component invents user-facing text.
- Don't use `@code` blocks in library components.
- Don't file such a component under a category naming its subject matter instead, the way `Files` would swallow `Explorer`.
- Don't put component-specific styles in global stylesheets or scatter supporting types into unrelated shared folders.

## Component authoring

- Do declare components `public sealed partial class FormaX : ComponentBase` by default; unseal only when a child must reference the parent type.
- Do use a file-scoped namespace matching the folder path.
- Do order members by role: constants → `[Parameter]`s → `[Inject]`s → fields → computed properties → lifecycle methods → handlers/helpers → `InputBase` overrides → disposal.
- Do keep related parameters together rather than sorting members alphabetically.
- Do put an expression-bodied arrow on the next indented line:

  ```csharp
  private void Close()
      => _isOpen = false;
  ```

- Do suffix asynchronous methods and event handlers with `Async`, such as `HandleAddNewAsync` and `OnJumpKeyDownAsync`.
- Do keep parameter declarations on one line and use `[Parameter, EditorRequired]` for mandatory parameters.
- Do add `AdditionalAttributes` with `CaptureUnmatchedValues = true` and `@attributes` when the root element should accept pass-through attributes.
- Do generate element IDs as `$"prefix-{Guid.NewGuid():N}"` so multiple instances can coexist.
- Don't place component logic in markup.
- Don't use `.ConfigureAwait(false)` in `Forma.UI.Blazor` components; leaving the synchronization context means state touched after an await has to be marshalled back through `InvokeAsync`. `src/Forma.UI.Blazor/.editorconfig` sets `CA2007` to `none`, so nothing will flag it for you.
- Don't omit `.ConfigureAwait(false)` from awaited service code in `Forma.UI.Features`, which runs off the renderer and where the root `.editorconfig` sets `CA2007` to `error`.

## Options records

- Do group configuration into a `sealed record {Component}Options` when three or more related flags travel together, callers reuse named presets, the concern is expected to grow, or a composing parent forwards the settings.
- Do use `init`-only properties with the same defaults as the parameters they replace.
- Do add a static `Default` property when a bare `new()` is the common case.
- Do name the component's own options parameter `Options`.
- Do prefix forwarded child options by concern, such as `PaginationOptions` on `FormaDataGrid`.
- Do keep parameters standalone when each controls an independent feature area or when a lone flag has no siblings or planned growth.
- Do let a record serve forwarding alone: the component that owns the settings keeps its standalone parameters when its own callers set one at a time, as `FormaButton` does beside `FormaButtonOptions`.
- Do expose only the settings a composed child may safely vary; `FormaCloseButton` forwards `Size` and pins the rest, because a caller changing them would stop it being a close button.
- Don't create an options record merely to collect unrelated settings.
- Don't force independent settings such as `EnablePagination`, `EnableCheckboxes`, and `Virtualize` into one record.

## Validation and binding

- Do validate parameters in `OnParametersSet` with throw helpers such as `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfLessThan`, and `ArgumentNullException.ThrowIfNull`.
- Do throw `InvalidOperationException` from `OnInitialized` for structural misuse such as a child outside its required parent.
- Do derive form inputs from `InputBase<TValue>`, render `@CssClass`, emit `<ValidationMessage For="@ValueExpression" />`, and implement `TryParseValueFromString`.
- Do use a plain `Value` / `ValueChanged` pair for non-form components.
- Don't postpone validation until an event handler when lifecycle validation can fail earlier and more clearly.

## Parent/child composition and disposal

- Do compose parent/child pairs such as `FormaTabs` and `FormaTab` through a `CascadingValue` of the parent.
- Do register the child in `OnInitialized`, unregister it in `Dispose`, and keep parent registration methods `internal`.
- Do implement `IDisposable` or `IAsyncDisposable` for owned `CancellationTokenSource`, `Timer`, JS module references, and service subscriptions.
- Don't dispose dependencies the component does not own.
- Don't leave registrations, subscriptions, timers, or JS references alive after the component is disposed.

## CSS

- Do use CSS isolation and begin each `.razor.css` file with a banner naming the component and its approach.
- Do use logical properties such as `margin-block-end`, `inset-inline-start`, `padding-inline`, and `text-align: start` so RTL works without a second stylesheet.
- Do read colours, fonts, and shadows from `src/Forma.UI.Blazor/wwwroot/forma-tokens.css`.
- Do end every custom property containing a colour with `-color`.
- Do make component-local colour knobs fall back to a token: `var(--forma-toggle-switch-on-background-color, var(--main-color))`.
- Do reuse existing tokens such as `--main-color` and `--focus-color`; `--main-dark-color`, `--main-light-color`, and `--focus-color` derive from `--main-color`.
- Do keep `forma-tokens.css` value-only, `forma-reset.css` limited to cross-browser normalization, and `forma-base.css` limited to the opt-in element-level look.
- Do keep component rules in the component's isolated stylesheet.
- Do rely on the reset's `font: inherit` for form elements, set only the needed `font-size`, and use `var(--font-family-mono)` instead of bare `monospace`.
- Do animate decoration only; every state must remain understandable without motion.
- Don't use physical direction properties such as `left`, `right`, `margin-left`, or `padding-right`.
- Don't hardcode a colour as a component-local token fallback or invent aliases for existing tokens; a literal fallback is an island a theme override cannot reach.
- Don't add component rules to any global stylesheet.
- Don't set `font-family` on form elements.
- Don't use motion as the only channel communicating state.

## Services

- Do place contracts in `Forma.UI.Features.Abstractions/{Category}/IXService.cs`.
- Do extend `INotifyStateChanged` when a component must rerender after service state changes.
- Do place sealed implementations in `Forma.UI.Features/{Category}/XService.cs`.
- Do compose `StateNotifier` for `Func<Task>` subscribers, validate arguments, and put an optional `CancellationToken` last.
- Do subscribe from the component in `OnInitialized` and unsubscribe in `Dispose`.
- Do let the host register services explicitly, such as `AddScoped<IXService, XService>()`.
- Don't ship an `AddForma...` registration extension from the library.
- Don't make the packaged component project depend on service implementations.

## Build and verification

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
npm test --prefix tests/Forma.UI.Blazor.Tests.Scripts
dotnet run --project src/Forma.UI.Blazor.Showcase
```

No project sets `GenerateDocumentationFile`, so nothing reports an undocumented public member. Turn it on for one throwaway build to list them:

```powershell
dotnet build Forma.slnx -p:GenerateDocumentationFile=true --no-incremental -v:q -nologo |
    Select-String CS1591
```

- Do require build, tests, and the format check to pass before offering a component for review.
- Do run that `CS1591` sweep after adding or changing public API, and ignore the hits under `tests/`, which are test method names.
- Do run the format check rather than trusting the build: it catches using-directive ordering and line endings that compilation ignores, and it does not read `.razor` files at all, so a misordered `@using` has to be caught by eye.
- Do reopen `Forma.slnx` after adding a project; the C# extension rewrites it with a duplicate `<Folder>` entry and drops `<Configurations>`, which then fails the build with `MSB4025`.
- Do stop a running Showcase process before rebuilding, because it holds its `bin` DLLs open and the copy fails with `MSB3027`.
- Do confirm the rebuild actually reached the app before drawing any conclusion from the browser: the Showcase runs a compiled binary with no hot reload, so verify the build succeeded and that `src/Forma.UI.Blazor.Showcase/bin/Debug/net11.0/Forma.UI.Blazor.dll` is newer than the sources you changed. A stale process silently serves the old code and every experiment against it is void.
- Do inspect the component in all three cultures through the Showcase.
- Do drive end-to-end browser checks in a **visible** window rather than a headless one, so the maintainer can watch what is actually being exercised; a headless run reports only what its script thought to assert, so a mis-scoped page and a genuine pass look alike.
- Do slow such a run enough to be followed, and hold the final state on screen before closing.
- Don't pipe the Showcase command's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## Working agreement

- Do build, review, and commit one component at a time.
- Do pick the next component from the inventory and Épica's remaining set.
- Do derive parameters, validation, disposal, CSS, ARIA, localization, and tests from established repository patterns.
- Do deliver the complete component set: `.razor`, `.razor.cs`, `.razor.css`, warranted translation or JS files, bUnit tests, and Showcase page.
- Do verify both directions, colour-independent states, accessible names and focus, and 24×24 touch targets before review — the `forma-a11y-touch-rtl` skill carries the full standard.
- Do present the component for review and stop; iterate on that same component until the maintainer declares it complete.
- Do mark completion in both documents after approval: add `✅` to the component heading and build-order entry in `Forma/COMPONENTS.md`, then change `☐` to `✅` and update status in `EpicaDeportiva/docs/forma-components.md`.
- Do propose a per-component commit message and wait for explicit permission before changing Git state.
- Do keep changes minimal and on-scope.
- Don't move to the next component before the current one is approved.
- Don't mark a component complete on your own judgement.
- Don't run state-changing Git commands such as `add`, `commit`, `push`, or `reset` without explicit permission; read-only Git is allowed.
- Don't retrofit new conventions onto existing components unless asked.
