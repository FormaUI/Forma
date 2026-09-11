---
name: forma-authoring-components
description: Forma's component authoring standard — the file set and folder a component lives in, the class shape and member order, when settings travel in an options record and when they stay parameters, the view and interaction contract pair, validation in the lifecycle, parent and child composition and disposal, how a component logs and what it logs at which level, how a service is shaped and registered, and the Showcase page every component ships with, rendering every state it can enter. Use when creating a component, adding or changing a parameter or an options record, splitting a component into sub-components, adding a service a component depends on, adding a log message, or adding or extending a Showcase page.
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
- Do let a consumer read its own strings through the base's `L`, overriding `LocalizationSourceType` where it shares the subject's file.
- Do cascade the two interfaces as two values where the consumers are Blazor children, and prefer a plain `[Parameter]` wherever the child is reachable directly in the subject's markup.
- Don't mirror the subject's parameters onto a separate object, which drifts the moment a parameter is added and no test can catch it.
- Don't pass label factories or other closures over the subject as data; they are indirection for something the consumer can resolve itself.
- Don't reach past the immediate subject to the one behind it.

## Bases

Every component derives from one of three bases in `Forma.Blazor/Components/`, and never from `ComponentBase` or `InputBase<TValue>` directly.

- Do derive a component a host places from `FormaElementBase`, a form input from `FormaInputBase<TValue>`, and a part that only lives inside its owner and takes nothing from a host — an explorer's toolbar, a composer's dialog — from `FormaComponentBase`.
- Do take from the base what every component needs: `Context`, the component's `IComponentContext`, and its four forwards `RuntimeId` (its identity for its lifetime), `L` (its strings), `Logger` (under the component's own type) and `JS` (its JS environment); plus `IsDisposed`, and `DisposeAsyncCore` to override. A base builds the context once, so a thing every component needs is added to the context, never to a base twice.
- Do take from the element and input tiers the host surface: `Id`, `Class`, `Style` and `AdditionalAttributes` are the host's values; `RootClass` and `RootStyle` are what the root writes, built from `BuildRootClass(CssClassBuilder)` and `BuildRootStyle()`; `RootElement` is the root once rendered. On the input tier `RootClass` also carries the field's `valid`/`invalid`/`modified` class.
- Do write the root element's first attributes in this order, whatever else follows:

  ```razor
  <div id="@Id"
       data-rid="@RuntimeId"
       class="@RootClass"
       style="@RootStyle"
       @attributes="AdditionalAttributes"
       @ref="RootElement"
  ```

- Do treat the root as the element the host addresses: for an input, the control inside its `FormaField` (the `<textarea>`, the trigger `<button>`), never the field wrapper; for a composite, the control the host names and styles (a drop-down menu's trigger, a checkbox's `<input>` inside its label). A wrapper the component draws around that control is an internal part, held in a private reference where interop needs it.
- Do forward the host surface when the root is another Forma component — `<FormaButton Id="@Id" Class="@RootClass" Style="@RootStyle" @attributes="AdditionalAttributes" …>` — hold that component in a private property, and override `RootElement` to answer with its root, null-guarded, so a reader gets the same element whichever of the two it holds.
- Do add the component's own classes in `BuildRootClass` with `builder.Add("forma-x").Add("is-open", IsOpen)`, and compose its own style in `BuildRootStyle`; the base appends the host's `Class` and `Style` itself.
- Do derive a generated id as a computed property, `private string ElementId => Id ?? $"prefix-{RuntimeId:N}";`, and a part's id from it by the part's role, `PopoverId => $"{ElementId}-popover"`; the `forma-naming` skill's identifiers rule has the full shape. Never initialize a field from `RuntimeId`: a derived class's initializers run before the base's.
- Do hold a reference to a child component (`@ref`) in a private property, `private FormaButton TriggerButton { get; set; } = default!;`, because every component is disposable and CA2213 would otherwise ask the holder to dispose what the renderer owns.
- Don't declare `Id`, `Class`, `AdditionalAttributes`, an `IJSRuntime`, an `IStringLocale` or an `ILogger` on a component; the base has them, and the compiler reports the shadowing.

## Component authoring

- Do declare components `public sealed partial class FormaX : FormaElementBase` (or the tier's base) and put `@inherits FormaElementBase` in the `.razor`; unseal only when a child must reference the parent type.
- Do use a file-scoped namespace matching the folder path.
- Do order members by role: constants → `[Parameter]`s → `[Inject]`s → fields → computed properties → lifecycle methods → handlers/helpers → `BuildRootClass` and `BuildRootStyle` → `InputBase` overrides → `DisposeAsyncCore`.
- Do keep related parameters together rather than sorting members alphabetically.
- Do put an expression-bodied arrow on the next indented line:

  ```csharp
  private void Close()
      => _isOpen = false;
  ```

- Do suffix asynchronous methods and event handlers with `Async`, such as `HandleAddNewAsync` and `OnJumpKeyDownAsync`.
- Do keep parameter declarations on one line and use `[Parameter, EditorRequired]` for mandatory parameters.
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
- Do derive form inputs from `FormaInputBase<TValue>`, write `class="@RootClass"` on the control so the field's validation class rides with the component's own, and implement `TryParseValueFromString`.
- Do emit `<ValidationMessage For="@ValueExpression" />` from an input that composes `FormaField`; a bare control the library also places outside a form, as the grid places `FormaCheckBox`, leaves the message to the host, since a `ValidationMessage` needs an `EditContext` to exist.
- Do name a field through `ValueExpression` where the library uses an input as a control rather than a form field — a grid's row marker, a multi-select's option — because `InputBase` requires one.
- Do use a plain `Value` / `ValueChanged` pair for non-form components.
- Don't postpone validation until an event handler when lifecycle validation can fail earlier and more clearly.

## Parent/child composition and disposal

- Do compose parent/child pairs such as `FormaTabs` and `FormaTab` through a `CascadingValue` of the parent.
- Do register the child in `OnInitialized`, unregister it in `DisposeAsyncCore`, and keep parent registration methods `internal`.
- Do override `DisposeAsyncCore` to release an owned `CancellationTokenSource`, `Timer`, JS wrapper or service subscription, awaiting the asynchronous ones and ending with `await base.DisposeAsyncCore();`; the base runs it once and sets `IsDisposed`, and on an input it also runs the framework's own `Dispose(true)`, which the renderer never calls on an `IAsyncDisposable`.
- Don't declare `IDisposable` or `IAsyncDisposable`, `Dispose()` or `DisposeAsync()` on a component, and don't keep a disposed flag of your own; `IsDisposed` is the base's.
- Don't dispose dependencies the component does not own.
- Don't leave registrations, subscriptions, timers, or JS references alive after the component is disposed.

## Logging

A component logs through the base's `Logger`, whose category is the component's concrete type, so a host filters by `Forma.Blazor.Components.Layout.FormaFrame` and sees the frame's interop under the same name.

- Do hand the base's `JS` to every interop wrapper the component constructs, `new Frame(JS)`; it is the component's `IJSEnvironment`, the runtime carrying the component's `Logger` and `RuntimeId`, so what the wrapper absorbs lands under the component and names the instance.
- Do give a service that is not a component a required `ILogger<T>` through its constructor, and have its registrar `TryAdd` the `NullLogger<>` fallback beside it, so the service resolves in a host that configures no logging.
- Do write every message through `[LoggerMessage]` on one `internal static partial class {Feature}Log` per feature, named for the event it records, `CircuitDroppedWhileCalling`, so every message a feature can emit is read in one place.
- Do log at `Debug` what the library absorbs by design — a dropped circuit or a thrown script behind a `Try*` call, an asset that would not load — so the silence is visible to whoever turns the category on and nothing changes for a host that does not.
- Don't log above `Debug` without deciding it for that one site; `Warning` and `Error` are never applied in bulk.
- Don't write to the console or through an abstraction of Forma's own; `ILogger` is what every host already configures and filters.

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
