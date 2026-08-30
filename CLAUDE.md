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
- Do give a feature one assembly, `Forma.UI.Features.{Feature}`, holding its contracts and the default implementation of them together, because the components cannot function without that default and a second assembly would only buy a reference that is easy to get wrong.
- Do split contracts into an `.Abstractions` assembly of their own only where a consumer can genuinely run without the implementation behind them, which is what makes the split right for a framework concern and wrong for a feature the components depend on.
- Do decide by ownership, not by how many assemblies consume it: a contract a single feature owns stays with that feature no matter how many projects reference it, such as `IToastService` in `Feedback`.
- Do keep a folder and its namespace in step, so `Forma.UI.Blazor/Components/Design/` holds `Forma.UI.Blazor.Components.Design` and `Forma.UI.Features.Media/Providers/` holds `Forma.UI.Features.Media.Providers` — the namespace is the assembly name plus the folder path, with any `.Abstractions` suffix dropped.

- Do make components depend on abstractions such as `IToastService`; let the host choose implementations through DI.
- Do represent colours and other CSS values with typed values such as `CssColor.FromRgba(...)` and `CssColor.FromHex(...)`.
- Do share one namespace across a pair that is genuinely split, so the assembly name carries the split and the namespace does not, matching `Microsoft.Extensions.DependencyInjection` and its `.Abstractions`.
- Do give every other project a namespace equal to its assembly name, so `Forma.UI.Blazor` holds `Forma.UI.Blazor.*` and `Forma.UI.Design.Icons` holds `Forma.UI.Design.Icons.*`.
- Do keep these identities distinct: assembly and namespace `Forma.UI.Blazor`, package ID `2Site.Forma.UI.Blazor`, static asset base path `_content/Forma.UI`.
- Do let `Forma.UI.Blazor` reference the feature assemblies its components need and no more; anything a component works without belongs behind a package a host opts into.
- Do ship a feature as a package of its own only when it is optional — interchangeable content such as an artwork set, which a host adds deliberately — carrying its own assets, with the contracts it needs travelling inside it through `PrivateAssets="all"`.
- Do let the components' own package carry the features they depend on, so a host installing the library needs nothing else to render.
- Do give a feature with services to register a `ServiceCollectionExtensions` at its project root, registering with `TryAdd` so a host that registered its own first still wins, and name the method for what lands in the container — an artwork package adds a pack named for its source, `AddFormaTwemojiPack()`, while a feature whose defaults are services is `AddFormaFeedbackServices()`.
- Do ship exactly one aggregate registrar from `Forma.UI.Blazor`, `AddFormaUI()`, fanning out to every default a component needs, so a host makes one call and still overrides any single service by registering it first.
- Do let a component whose optional contract is unregistered fall back to a default the library carries rather than failing, as `FormaIcon` does with the library's own glyph artwork; a contract carrying data nobody can synthesise, such as an emoji catalog, instead leaves the component in its empty state.
- Don't represent typed CSS values as arbitrary strings.
- Don't set `AssemblyName` where it would only repeat the project name, and don't give a project a `PackageId` unless a host is meant to install it by name; everything else ships inside whichever package needs it.
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
- Do give a supporting type its owning feature's name as a prefix only where that reads well and adds meaning, as `ExplorerAction` and `ExplorerSelection` do against `System.Action` and a bare `Selection`; `Collections/` needs no `CollectionsXxx` and takes none.
- Don't carry the `Forma` component prefix on a type that is not a component, which is why a feature's options record is `ExplorerOptions` rather than `FormaExplorerOptions`.
- Do find a better name when a prefix is needed to disambiguate but reads badly.
- Do call a thing Forma ships a **component**, and raw HTML an **element** — one word per concept. "Control" is Web Forms vocabulary inherited from Coi; keep it only in `aria-controls` and in descriptions of Coi's own markup.
- Don't apply one naming convention indiscriminately across Razor tags, CSS classes, and C# types.
- Do name a glyph for its recognizable visual form — `ArrowLeft`, not `Back` — and fall back to the form's conventional UI meaning only where the literal name would be obscure, cumbersome, or less recognizable: `Search`, not `MagnifyingGlass`.
- Don't name a glyph for an application's command or feature unless it was drawn specifically and exclusively for it, such as a bespoke `Checkout` glyph used only for checkout.

### Fields, locals, and parameters

A name earns its place by answering the question a reader arrives with. The declaration already gives the type; repeating it, or describing the plumbing underneath it, leaves the reader no better off than before.

- Do name a field for the role its value plays rather than for its type or its mechanism, so a field holding an interop behaviour says which capability that behaviour gives the component; a bare `_watcher` or `_interop` says only what its own declaration already said.
- Do spell a name out. An abbreviation is unreadable without jumping back to the declaration, and single letters belong to loop counters and maths alone.
- Do keep a lambda's parameter short only while its expression reads as a single thought, and name it as soon as the body outgrows one line.
- Do name a Blazor event handler's argument `args`; `e` is the older `(sender, e)` idiom and says nothing about what a handler is reading.
- Do give a computed property a property's name, because a leading underscore promises a field at every call site that reads it.
- Do say `Element` in the name of an `ElementReference` wherever the bare noun would read as the thing rendered rather than the DOM node, which matters most where the element sits beside the interop object attached to it.
- Do spell a recurring field the way its counterparts in sibling components spell it; several names for one recurring thing is itself the defect, even where each reads well alone.
- Don't leave a name that has drifted from what it holds; a rename is cheap and the compiler proves it.

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
   - Do name a generated id after the sub-element it identifies instead, however many there are, whenever it is not the component's own primary element: `_triggerId` and `_popupId` in `FormaSelect` and `FormaMultiSelect`, `_labelId` and `_noteId` in `FormaVideoPlayer`, and a lone `_popupId` in `FormaDropDownMenu` or `_navigationInputId` in `FormaPagination`.
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

- Do state what a thing is in XML docs, CSS banners, inline comments, and README files.
- Do explain how something works only where the mechanism is intricate enough that a reader could not recover it from the code, and then describe the mechanism rather than the reasoning that chose it.
- Do use US English spelling in documentation and comments — color, license, catalog — with one deliberate exception: behaviour keeps its British spelling.
- Do comment only when the code cannot express an important constraint, such as a necessary `!important`, a non-obvious `-webkit-` property, or an off-by-one invariant.
- Do keep comments to one line whenever possible.
- Do add a one-line XML `<summary>` describing what every public parameter and public type is.
- Do put `/// <inheritdoc />` on every lifecycle override and interface implementation — `OnInitialized`, `OnParametersSet`, `OnAfterRenderAsync`, `Dispose`, `DisposeAsync`, `TryParseValueFromString`, `FormatValueAsString` — rather than restating what the base member already documents.
- Do keep historical origin and usage mapping in the intentional *Old pattern* and *Used in* rows of `COMPONENTS.md`.
- Don't document history, rejected alternatives, decision arguments, roadmap position, call sites, mirrors, or other cross-references.
- Don't write what a thing might do, does not do, or would otherwise have done; a sentence about code that is not there sends the reader looking for it.
- Don't restate obvious code or pad files with unnecessary prose; delete a comment that carries nothing rather than rewording it.

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
- Do put a supporting type in the feature assembly instead once the concern has one, because a type a host names in a callback or an options parameter is the feature's vocabulary rather than one component's detail; what stays beside the component is what no caller ever names, such as a stream wrapper driving a progress bar.
- Do put script a component needs in `wwwroot/scripts/`, never beside the component.
- Do add the translation pair only when the component invents user-facing text.
- Don't use `@code` blocks in library components.
- Don't file such a component under a category naming its subject matter instead, the way `Files` would swallow `Explorer`.
- Don't put component-specific styles in global stylesheets or scatter supporting types into unrelated shared folders.

## View and interaction

A component whose sub-components only work inside it still has to hand them state and let them act on it. Split that contract in two, naming both halves for the **subject** the contract describes rather than for its place in a hierarchy, because the split is worth making wherever reading and acting want separating and the subject is not always a parent.

- Do declare `I{Subject}View` for everything a consumer reads and `I{Subject}Interaction` for everything a consumer invokes.
- Do have the subject implement both itself, so a member returns the field or parameter the subject already owns and there is nothing to copy and nothing to keep in step.
- Do implement the members explicitly, which keeps a public component from growing public surface for an internal contract.
- Do route every member that changes state through one private helper on the subject that changes it and then reports it, so a call site cannot do the one without the other.
- Do put the pair with the feature the subject belongs to rather than beside the component implementing it, so the component folder holds components and the vocabulary they speak lives in `Forma.UI.Features.{Feature}`.
- Do give the pair the visibility its assembly implies: `internal` while it sits beside its only implementer, `public` once it ships from the feature assembly a component reads it from.
- Do let a consumer talk to its immediate subject alone, and have a subject that is itself a consumer re-expose what its own consumers need.
- Do let a consumer resolve its own localization through `IStringLocale<T>`.
- Do cascade the two interfaces as two values where the consumers are Blazor children, and prefer a plain `[Parameter]` wherever the child is reachable directly in the subject's markup.
- Don't mirror the subject's parameters onto a separate object, which drifts the moment a parameter is added and no test can catch it.
- Don't pass label factories or other closures over the subject as data; they are indirection for something the consumer can resolve itself.
- Don't reach past the immediate subject to the one behind it.

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
- Do build a component from the library's own components wherever one exists — a button is a `FormaButton`, a text entry a `FormaTextBox`, a glyph a `FormaIcon` — so the library is its own first consumer and a control's hover, disabled, focus and busy states come from one place; a raw element the library already wraps is a finding: either the component can do the job and replaces it, or it cannot and that gap is a capability to add, never a private element with private styling.
- Do specialize by composition, not by widening a general component: a context that needs its own kind of control gets a component that composes the base and exposes only what that context means (`FormaToggleButton` over `FormaButton`, carrying a toggle's pressed state and the toolbar's look), and the base takes a new parameter only when its meaning is general to every use of the base.
- Don't place component logic in markup.
- Don't use `.ConfigureAwait(false)` in `Forma.UI.Blazor` components; leaving the synchronization context means state touched after an await has to be marshalled back through `InvokeAsync`. `src/Forma.UI.Blazor/.editorconfig` sets `CA2007` to `none`, so nothing will flag it for you.
- Don't omit `.ConfigureAwait(false)` from awaited service code in a `Forma.UI.Features.*` project, which runs off the renderer and where the root `.editorconfig` sets `CA2007` to `error`.

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
- Don't hardcode a colour or a measurement as a `var()` fallback, or invent aliases for existing tokens; a global token is defined once and needs no fallback at all, and a component's own hook falls back to a global token, because a literal fallback restates the value at every call site where a theme override cannot reach it.
- Don't add component rules to any global stylesheet.
- Don't set `font-family` on form elements.
- Don't use motion as the only channel communicating state.

### One scale for size and space

Every measurement a component makes comes from one scale, so a child agrees with its parent and one component agrees with the next. The scale is the 4px grid that Material, Apple's guidelines and Bootstrap all build on, expressed in rem so it follows the reader's font size; nothing about it is Forma's invention, and nothing outside it is a component's to invent.

- Do take every gap, padding and margin from the spacing tokens, and give each step one job: `--space-1` (0.25rem) between a glyph and its label inside one control, `--space-2` (0.5rem) between controls in a row and as a control's block padding, `--space-3` (0.75rem) as a control's inline padding and between groups in a row, `--space-4` (1rem) between blocks and as a panel's padding, `--space-5` (1.5rem) and `--space-6` (2rem) between sections.
- Do take every text size from the type tokens — `--font-size-xs` for a caption or a badge, `--font-size-sm` for a control's label and dense data, `--font-size-base` for prose and inputs, `--font-size-lg` for a section or dialog heading, `--font-size-xl` for a page title, `--font-size-2xl` for a display figure — and line height from `--line-height-prose` (1.5) or `--line-height-control` (1.25).
- Do take every corner from the radius tokens: `--radius-sm` on a control, `--radius-md` on a card, popup or panel, `--radius-lg` on a dialog or toast, `--radius-pill` on a badge or a chip; `50%` stays for a genuine circle.
- Do keep `em` for what must scale with the text it sits in — a glyph inline with text, sub- and superscript, the typography of authored content — and `px` for a hairline, a focus ring, a shadow offset and a 1–2px optical nudge against a border; everything else is a token.
- Do leave a glyph inline with text at its default `em` size and give a standalone one its `IconSize` on the `FormaIcon` itself — `Small` on a button, in a toolbar, in a menu or in a dense row, `Medium` leading a panel or a cell, `Large` as a section's figure, `Display` as an empty state's or a drop zone's figure — because each size names both an icon token and the artwork grid whose stroke is crisp at it; a stylesheet that sizes an icon from outside defeats that pairing and is kept only as a fallback for a glyph a caller left unsized. Keep a control's target at the 24×24 floor and 44×44 under a coarse pointer, which is what the standard and both platform guidelines specify.
- Do take the box a control is pressed on from the control tokens — `--control-size-sm` for an icon button, a compact toolbar cell or a dense row, `--control-size-md` for a button, an input, a toolbar button or a menu item, `--control-size-lg` for a prominent action, `--control-size-xl` for a field that stacks a label over its value, `--control-size-touch` for any control under a coarse pointer — so a target's size is one decision made once, and an input stands exactly as tall as the button beside it; a box that is not a control, such as a thumbnail or a figure, takes an icon token instead.
- Do derive a control's padding from the step it stands on rather than the other way round: the step is fixed, the text line is known, and the padding is what remains.
- Do derive a width threshold from the content it guards and say so beside it — the widths of the items that must fit, added up — rather than from a device; a tier is arithmetic, not a guess.
- Don't write a bare `rem`, `em` or `px` for spacing, type, radius or a control's box in a component; a value not on the scale is either a token that is missing, which is added, or a mistake.
- Don't reach for `overflow: auto` to make a row or a table fit; sideways scrolling inside a page is the last resort after packing, hiding by priority and stacking, and a table drops its least important columns before it scrolls.

### Responsive by design

A component is handed a width, never a device: it sits in a phone's full column, a tablet's half, a desktop's dialog or a sidebar's sliver, and must fit whatever it is given rather than a viewport it assumed.

- Do size in `rem` for spacing, radii and type so a reader's own font size carries through, and keep `px` for a hairline, a focus ring, a shadow, and the 24×24 and 44×44 target minimums the standard defines in CSS pixels.
- Do adapt to the component's own width with a `@container` query, and reserve `@media` for facts about the reader — hover, pointer, motion, forced colours — or the viewport as a whole; a component inside a narrow pane is narrow whatever the window is.
- Do choose how a row gives way when it cannot fit, and make it the design rather than the accident: a bar of commands scrolls behind arrows, a pager hides the pages it cannot show, a two-column layout stacks. Wrapping onto a second line is a choice too, and rarely the right one for a control row.
- Do pack before you stack: a grid of tiles or cells shrinks them to keep several across a narrow column, and a pager keeps as many pages as fit, because one column of full-size items is a long scroll that shows less, not a layout that adapted.
- Do let a flex item shrink: an input has an intrinsic width, and a flex child defaults to `min-inline-size: auto`, so either pushes its parent wider than the page unless told `min-inline-size: 0`.
- Do give a wheel or a gesture to the nearest thing that can answer it; a popup inside a scrolling bar takes the wheel before the bar does.
- Do check every component at a phone's width and a tablet's before review, in a real browser and by looking, because a page can pass a width measurement while wrapping, clipping, or hiding what a reader needs.
- Don't size a component against `100vw`; the viewport is not the space it was given.
- Don't set a fixed `inline-size` on a control row or a floor wider than a phone; `min(…, 100%)` keeps a floor without pushing the page.

## Services

- Do place a contract and the sealed implementation shipped with it in `Forma.UI.Features.{Feature}/{Category}/`, `IXService.cs` beside `XService.cs`.
- Do extend `INotifyStateChanged` when a component must rerender after service state changes.
- Do compose `StateNotifier` for `Func<Task>` subscribers, validate arguments, and put an optional `CancellationToken` last.
- Do subscribe from the component in `OnInitialized` and unsubscribe in `Dispose`.
- Do let a host replace any service by registering its own before the registrar runs, which is what `TryAdd` is there to allow.
- Do register a service the whole application shares, and construct one per component instance where each component owns its own state, as a composer owns its own undo history.
- Don't ship a second `AddForma...` extension from `Forma.UI.Blazor`; the aggregate registrar is the library's only one, and a feature's own belongs at that feature's root.

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

### Scripts

Task scripts live in `scripts/` and are PowerShell 7, cross-platform.

- Do name a script `Verb-Noun.ps1` with a verb from PowerShell's approved list (`Get-Verb`) and one PascalCase noun, so `Get-TwemojiPack.ps1`, `Build-GlyphPack.ps1`, `Update-PluralRules.ps1`; a second hyphen or a lowercase `fetch-` style name is wrong.
- Do write a generated data asset compact by default, and take a `-Format` switch that writes it indented for a person reviewing it.
- Do make a script that generates an asset validate its inputs and fail loudly, so a stale or malformed asset cannot be produced silently.
- Don't introduce another runtime for tooling; a script needs only PowerShell and the .NET SDK.

## Working agreement

- Do build, review, and commit one component at a time.
- Do pick the next component from the inventory and Épica's remaining set.
- Do derive parameters, validation, disposal, CSS, ARIA, localization, and tests from established repository patterns.
- Do deliver the complete component set: `.razor`, `.razor.cs`, `.razor.css`, warranted translation or JS files, bUnit tests, and Showcase page.
- Do verify both directions, colour-independent states, accessible names and focus, and 24×24 touch targets before review — the `forma-a11y-touch-rtl` skill carries the full standard.
- Do present the component for review and stop; iterate on that same component until the maintainer declares it complete.
- Do mark completion in both documents after approval: add `✅` to the component heading and build-order entry in `Forma/COMPONENTS.md`, then change `☐` to `✅` and update status in `EpicaDeportiva/docs/forma-components.md`.
- Do finish a piece of work with it unstaged, report what changed and what the gate said, and propose a commit message for the maintainer to run themselves.
- Do keep changes minimal and on-scope.
- Don't move to the next component before the current one is approved.
- Don't mark a component complete on your own judgement.
- Don't run a state-changing Git command — `add`, `commit`, `push`, `reset`, `restore`, `stash`, a path-scoped `checkout` — without permission for that command, asked for once the work exists and the maintainer can see what is in it. An instruction given up front about work not yet done grants nothing, and neither does permission for an earlier commit; read-only Git needs no permission at all.
- Don't retrofit new conventions onto existing components unless asked.
