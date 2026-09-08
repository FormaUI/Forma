---
name: forma-authoring-components
description: Forma's component authoring standard — the file set and folder a component lives in, the class shape and member order, when settings travel in an options record and when they stay parameters, the view and interaction contract pair, validation in the lifecycle, parent and child composition and disposal, how a service is shaped and registered, and the Showcase page every component ships with, rendering every state it can enter. Use when creating a component, adding or changing a parameter or an options record, splitting a component into sub-components, adding a service a component depends on, or adding or extending a Showcase page.
---

# Components

## Component file layout

Components live in `src/Forma.Blazor/Components/{Category}/`, where `{Category}` names a UI role such as `Bars`, `Feedback`, `Inputs`, or `Navigation`.

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
- Do put script a component needs in the `wwwroot/scripts/` of the assembly that ships it, never beside the component.
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
- Do put the pair with the feature the subject belongs to rather than beside the component implementing it, so the component folder holds components and the vocabulary they speak lives in `Forma.Framework.{Feature}`.
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
- Do keep focus consistent across every surface: a transient surface — a menu, a popover, the composer's selection bar — holds Tab inside itself, wrapping at either end, and Escape hands focus back to what opened it, so the keyboard never falls through a floating surface into the page behind it; compose the shared `Focus` behaviour rather than handling Tab per component. A composite component whose commands sit above its working surface arranges its own Tab order — the surface first, its commands next, then out of the component — by composing the same `Focus` behaviour, so Tab from the surface reaches that component's commands rather than the next component on the page.
- Do specialize by composition, not by widening a general component: a context that needs its own kind of control gets a component that composes the base and exposes only what that context means (`FormaToggleButton` over `FormaButton`, carrying a toggle's pressed state and the toolbar's look), and the base takes a new parameter only when its meaning is general to every use of the base.
- Don't place component logic in markup.
- Don't use `.ConfigureAwait(false)` in `Forma.Blazor` components; leaving the synchronization context means state touched after an await has to be marshalled back through `InvokeAsync`. `src/Forma.Blazor/.editorconfig` sets `CA2007` to `none`, so nothing will flag it for you.
- Don't omit `.ConfigureAwait(false)` from awaited service code in a `Forma.Framework.*` project, which runs off the renderer and where the root `.editorconfig` sets `CA2007` to `error`; an interop assembly relaxes that rule as `Forma.Blazor` does, because its wrappers marshal back through the renderer.

## Options records

- Do group configuration into a `sealed record {Component}Options` when three or more related flags travel together, callers reuse named presets, the concern is expected to grow, or a composing parent forwards the settings.
- Do use `init`-only properties with the same defaults as the parameters they replace.
- Do add a static `Default` property when a bare `new()` is the common case.
- Do name the component's own options parameter `Options`.
- Do prefix forwarded child options by concern, such as `PaginationOptions` on `FormaDataGrid`.
- Do sort every parameter into one of four categories, and put only the fourth into the record:
  1. **State the reader changes** (`Placement`, `Maximized` on `FormaWindow`) stays a parameter with its `Changed` twin, so a host can `@bind` it.
  2. **Required configuration** (`Title`) stays a parameter marked `EditorRequired`.
  3. **Optional configuration a consumer derives from itself** (`Icon`) stays a parameter, because a consumer that already carries one would otherwise carry two.
  4. **Optional configuration consumers normally pass through unchanged** (`Closable`, `Resizable`, `MinResizeSize`) goes into the record.
- Do keep the record to what consumers normally forward unchanged, because a consumer exposes the record whole; anything a consumer derives from its own identity or state is a parameter on the component.
- Do keep parameters standalone when each controls an independent feature area or when a lone flag has no siblings or planned growth.
- Do let a record serve forwarding alone: the component that owns the settings keeps its standalone parameters when its own callers set one at a time, as `FormaButton` does beside `ButtonOptions`.
- Don't split settings of the fourth category within the same concern between the record and standalone parameters once the component takes `Options`; later settings belonging to that concern join the record.
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

## Services

- Do place a contract and the sealed implementation shipped with it in `Forma.Framework.{Feature}/{Category}/`, `IXService.cs` beside `XService.cs`.
- Do extend `INotifyStateChanged` when a component must rerender after service state changes.
- Do compose `StateNotifier` for `Func<Task>` subscribers, validate arguments, and put an optional `CancellationToken` last.
- Do subscribe from the component in `OnInitialized` and unsubscribe in `Dispose`.
- Do let a host replace any service by registering its own before the registrar runs, which is what `TryAdd` is there to allow.
- Do register a service the whole application shares, and construct one per component instance where each component owns its own state, as a composer owns its own undo history.
- Don't ship a second `AddForma...` extension from `Forma.Blazor`; the aggregate registrar is the library's only one, and a feature's own belongs at that feature's root.

## Showcase page

Every component requires `src/Forma.Blazor.Showcase/Components/Pages/{Category}/FormaXPage.razor` and its translation pair. The Showcase exists to be looked at, so a state it never renders is a state nobody has ever reviewed.

```razor
@page "/x-component"
@attribute [ShowcasePage("FormaX")]
@rendermode InteractiveServer
@inject IStringLocale<FormaXPage> L
```

- Do structure the page as `<h1>` plus `<p class="description">`, followed by one `demo-section` containing `<h2>` and `demo-box` per scenario.
- Do render the component in every state it can enter, not merely name the state: default, filled, required, disabled, invalid, plus whatever the component adds — empty collection, no search matches, over-limit count, indeterminate, loading.
- Do make each state reachable on the page. A validation state needs a model rule and a submit, not a paragraph describing one.
- Do treat the component's translation file as the inventory: if a key in `FormaX.he.json` cannot be displayed in the Showcase, a state is missing.
- Do localize all page copy so the `he` and `es-419` switchers exercise RTL and translation.
- Don't count a state as covered because the parameter exists; count it when the page renders the component standing in it.
- Don't treat the Showcase page as optional or as documentation that can be added later.
