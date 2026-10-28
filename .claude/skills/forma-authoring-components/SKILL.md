---
name: forma-authoring-components
description: The component authoring standard — the file set a component is and the category it lives in, the three bases and what a component takes from them, the root element and its host surface, the class shape and member order, when settings travel in an options record and when they stay parameters, the view and interaction contract pair, validation in the lifecycle, parent and child composition and disposal, how a component logs and what it logs at which level, how a service is shaped and registered, and the showcase page every component ships with, rendering every state it can enter. Use when creating a component, adding or changing a parameter or an options record, splitting a component into sub-components, adding a service a component depends on, adding a log message, or adding or extending a showcase page.
---

# Components

## Component file layout

A component lives in the components assembly under a category, where a category names a UI role such as bars, feedback, inputs or navigation, and is a file set: `FormaX.razor` for markup only, with `@namespace` first; `FormaX.razor.cs` for all component logic; `FormaX.razor.css` for isolated CSS with a banner; and a section in its category's locale file in each language pack, when the component invents user-facing text.

- Do keep markup in `.razor`, logic in `.razor.cs`, and component styling in `.razor.css`.
- Do give a component a category of its own, as an explorer or an editor has, when it is large enough to grow sub-components and supporting types of its own; the category then owns its folder and namespace.
- Do place supporting types such as a size enum or an item record beside the component that owns them.
- Do put a supporting type in the feature assembly instead once the concern has one, because a type a host names in a callback or an options parameter is the feature's vocabulary rather than one component's detail; what stays beside the component is what no caller ever names, such as a stream wrapper driving a progress bar.
- Do put script a component needs where the scripting standard places it: in its product when only that product uses it, otherwise in the interop assembly that serves it, never beside the component.
- Do add the locale sections only when the component invents user-facing text.
- Don't use `@code` blocks in library components.
- Don't file such a component under a category naming its subject matter instead, the way a category named for files would swallow an explorer.
- Don't put component-specific styles in global stylesheets or scatter supporting types into unrelated shared folders.

## View and interaction

A component whose sub-components only work inside it still has to hand them state and let them act on it. Split that contract in two, naming both halves for the **subject** the contract describes rather than for its place in a hierarchy, because the split is worth making wherever reading and acting want separating and the subject is not always a parent.

- Do declare `I{Subject}View` for everything a consumer reads and `I{Subject}Interaction` for everything a consumer invokes.
- Do have the subject implement both itself, so a member returns the field or parameter the subject already owns and there is nothing to copy and nothing to keep in step.
- Do implement the members explicitly, which keeps a public component from growing public surface for an internal contract.
- Do route every member that changes state through one private helper on the subject that changes it and then reports it, so a call site cannot do the one without the other.
- Do put the pair with the feature the subject belongs to rather than beside the component implementing it, so the component folder holds components and the vocabulary they speak lives in the feature assembly.
- Do give the pair the visibility its assembly implies: `internal` while it sits beside its only implementer, `public` once it ships from the feature assembly a component reads it from.
- Do let a consumer talk to its immediate subject alone, and have a subject that is itself a consumer re-expose what its own consumers need.
- Do let a consumer read its own strings through the base's `L`, overriding `LocalizationSourceType` where it shares the subject's file.
- Do cascade the two interfaces as two values where the consumers are Blazor children, and prefer a plain `[Parameter]` wherever the child is reachable directly in the subject's markup.
- Don't mirror the subject's parameters onto a separate object, which drifts the moment a parameter is added and no test can catch it.
- Don't pass label factories or other closures over the subject as data; they are indirection for something the consumer can resolve itself.
- Don't reach past the immediate subject to the one behind it.

## Bases

Every component derives from one of three bases, and never from `ComponentBase` or `InputBase<TValue>` directly.

- Do derive a component a host places from `FormaElementBase`, a form input from `FormaInputBase<TValue>`, and a part that only lives inside its owner and takes nothing from a host — an explorer's toolbar, an editor's dialog — from `FormaComponentBase`.
- Do take from the base what every component needs: `Context`, the component's `IComponentContext`, read as `Context.RuntimeId` (its identity for its lifetime), `Context.Logger` (under the component's own type) and `Context.JS` (its JS environment); `L`, the one forward, for its strings; `IsDisposed`; and `DisposeAsyncCore` to override. A base builds the context once and forwards nothing else, so a thing every component needs is added to the context, never to a base twice.
- Do reserve `Context` for the component context, and name anything else context-like a component holds for its subject alone: the parts of a composite read their cascaded state under the composite's name, `Composer` for a composer's parts, never `Context` or `ComposerContext`, and the environment is `JS`, never `JSEnvironment` or `Environment`.
- Do take from the element and input tiers the host surface: `Id`, `Class`, `Style` and `AdditionalAttributes` are the host's values; `RootClass` and `RootStyle` are what the root writes, built from `BuildRootClass(CssClassBuilder)` and `BuildRootStyle(CssStyleBuilder)`; `RootElement` is the root once rendered. On the input tier `RootClass` also carries the field's `valid`/`invalid`/`modified` class.
- Do write the root element's first attributes in this order, whatever else follows:

  ```razor
  <div id="@Id"
       data-rid="@Context.RuntimeId"
       class="@RootClass"
       style="@RootStyle"
       @attributes="AdditionalAttributes"
       @ref="RootElement"
  ```

- Do treat the root as the element the host addresses: for an input, the control inside its field (the `<textarea>`, the trigger `<button>`), never the field wrapper; for a composite, the control the host names and styles (a drop-down menu's trigger, a checkbox's `<input>` inside its label). A wrapper the component draws around that control is an internal part, held in a private reference where interop needs it.
- Do forward the host surface when the root is another library component — `<FormaX Id="@Id" Class="@RootClass" Style="@RootStyle" @attributes="AdditionalAttributes" …>` — hold that component in a private property, and override `RootElement` to answer with its root, null-guarded, so a reader gets the same element whichever of the two it holds.
- Do add the component's own classes in `BuildRootClass` with `builder.Add("forma-x").Add("is-open", IsOpen)`, and its own declarations in `BuildRootStyle` with `builder.Add("inline-size", Width)`, a `CssLength` rendering in its unit whatever the culture; the base appends the host's `Class` and `Style` itself, so a root's style reads `inline-size: 12ch; color: red;`, every declaration terminated.
- Do take a generated id from the base's `ElementId`, the host's `Id` or one minted from the runtime id, and derive a part's id from it by the part's role, `PopoverId => $"{ElementId}-popover"`; the `forma-naming` skill's identifiers rule has the full shape. Never initialize a field from `Context.RuntimeId`: the context is made on first read, and a derived class's initializers run before the base's.
- Do hold a reference to a child component (`@ref`) in a private property, `private FormaX Trigger { get; set; } = default!;`, because every component is disposable and CA2213 would otherwise ask the holder to dispose what the renderer owns.
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
- Do build a component from the library's own components wherever one exists — a button is the button component, a text entry the text box, a glyph the icon — so the library is its own first consumer and a control's hover, disabled, focus and busy states come from one place; a raw element the library already wraps is a finding: either the component can do the job and replaces it, or it cannot and that gap is a capability to add, never a private element with private styling.
- Do keep focus consistent across every surface: a transient surface — a menu, a popover, an editor's selection bar — holds Tab inside itself, wrapping at either end, and Escape hands focus back to what opened it, so the keyboard never falls through a floating surface into the page behind it; compose the shared focus behavior rather than handling Tab per component. A composite component whose commands sit above its working surface arranges its own Tab order — the surface first, its commands next, then out of the component — by composing the same focus behavior, so Tab from the surface reaches that component's commands rather than the next component on the page.
- Do specialize by composition, not by widening a general component: a context that needs its own kind of control gets a component that composes the base and exposes only what that context means (a toggle button over the button, carrying a toggle's pressed state and the toolbar's look), and the base takes a new parameter only when its meaning is general to every use of the base.
- Don't place component logic in markup.
- Don't use `.ConfigureAwait(false)` in components; leaving the synchronization context means state touched after an await has to be marshalled back through `InvokeAsync`. `CA2007`, the analyzer that asks for it, is off for components, so nothing will flag it for you.
- Don't omit `.ConfigureAwait(false)` from awaited service code in a framework project, which runs off the renderer and where `CA2007` requires it; an interop assembly relaxes that rule as the components assembly does, because its wrappers marshal back through the renderer.

## Options records

- Do group configuration into a `sealed record {Component}Options` when three or more related flags travel together, callers reuse named presets, the concern is expected to grow, or a composing parent forwards the settings.
- Do use `init`-only properties with the same defaults as the parameters they replace.
- Do add a static `Default` property when a bare `new()` is the common case.
- Do name the component's own options parameter `Options`.
- Do prefix forwarded child options by concern, such as `PaginationOptions` on a grid.
- Do sort every parameter into one of four categories, and put only the fourth into the record:
  1. **State the reader changes** (`Placement`, `Maximized` on a window) stays a parameter with its `Changed` twin, so a host can `@bind` it.
  2. **Required configuration** (`Title`) stays a parameter marked `EditorRequired`.
  3. **Optional configuration a consumer derives from itself** (`Icon`) stays a parameter, because a consumer that already carries one would otherwise carry two.
  4. **Optional configuration consumers normally pass through unchanged** (`Closable`, `Resizable`, `MinResizeSize`) goes into the record.
- Do keep the record to what consumers normally forward unchanged, because a consumer exposes the record whole; anything a consumer derives from its own identity or state is a parameter on the component.
- Do keep parameters standalone when each controls an independent feature area or when a lone flag has no siblings or planned growth.
- Do let a record serve forwarding alone: the component that owns the settings keeps its standalone parameters when its own callers set one at a time, as a button does beside its options record.
- Don't split settings of the fourth category within the same concern between the record and standalone parameters once the component takes `Options`; later settings belonging to that concern join the record.
- Do expose only the settings a composed child may safely vary; a close button forwards `Size` and pins the rest, because a caller changing them would stop it being a close button.
- Don't create an options record merely to collect unrelated settings.
- Don't force independent settings such as `EnablePagination`, `EnableCheckboxes`, and `Virtualize` into one record.

## Validation and binding

- Do validate parameters in `OnParametersSet` with throw helpers such as `ArgumentException.ThrowIfNullOrWhiteSpace`, `ArgumentOutOfRangeException.ThrowIfLessThan`, and `ArgumentNullException.ThrowIfNull`.
- Do throw `InvalidOperationException` from `OnInitialized` for structural misuse such as a child outside its required parent.
- Do derive form inputs from `FormaInputBase<TValue>`, write `class="@RootClass"` on the control so the field's validation class rides with the component's own, and implement `TryParseValueFromString`.
- Do hand the field component the input's `FieldIdentifier` through `Field`, so the field renders the form's messages for it beneath the element; a bare control the library also places outside a form, as a grid places a checkbox, leaves the message to the host, since a message needs an `EditContext` to exist.
- Do hand the field the input's `FieldElementIds` through `ElementIds`, so the field's label and message container carry the ids the input's references point at, and write `aria-describedby="@DescribedBy"` on the element its label names, after the host's attributes, so a reader reaching an invalid input hears why; a part of the component's own that describes the element, such as a count or advice, adds its id in `BuildDescribedBy`, and an input that draws no field in some state answers `HasFieldContainer` with that state.
- Do build an input that only specializes another, such as a currency amount over a number, as a component of the element tier that hands its `Value`, `ValueChanged` and `ValueExpression`, its label and its host surface to the one input it draws, overriding `RootElement` to answer with that input's root, so one input component stands under one field with one label, one set of messages and one set of element ids.
- Do name a field through `ValueExpression` where the library uses an input as a control rather than a form field — a grid's row marker, a multi-select's option — because `InputBase` requires one.
- Do keep the text an input parses as a draft from its input events until it commits or is replaced, and parse it in the draft's culture, so an edit begun before a culture switch keeps its meaning; a step, a chosen suggestion and a different bound value replace the draft, the same value bound again keeps it, and a rejected commit keeps its text and culture. The draft holds the text and its culture alone; the input keeps its own rules for committing.
- Do use a plain `Value` / `ValueChanged` pair for non-form components.
- Don't postpone validation until an event handler when lifecycle validation can fail earlier and more clearly.

## Parent/child composition and disposal

- Do compose parent/child pairs such as tabs and a tab through a `CascadingValue` of the parent.
- Do register the child in `OnInitialized`, unregister it in `DisposeAsyncCore`, and keep parent registration methods `internal`.
- Do override `DisposeAsyncCore` to release an owned `CancellationTokenSource`, `Timer`, JS wrapper or service subscription, awaiting the asynchronous ones and ending with `await base.DisposeAsyncCore();`; the base runs it once and sets `IsDisposed`, and on an input it also runs the framework's own `Dispose(true)`, which the renderer never calls on an `IAsyncDisposable`.
- Don't declare `IDisposable` or `IAsyncDisposable`, `Dispose()` or `DisposeAsync()` on a component, and don't keep a disposed flag of your own; `IsDisposed` is the base's.
- Don't dispose dependencies the component does not own.
- Don't leave registrations, subscriptions, timers, or JS references alive after the component is disposed.

## Logging

A component logs through the base's `Logger`, whose category is the component's concrete type, so a host filters by the component's full type name and sees the component's interop under the same name.

- Do hand the context's `JS` to every interop wrapper the component constructs, `new Wrapper(Context.JS)`; it is the component's `IJSEnvironment`, the runtime carrying the component's `Logger` and `RuntimeId`, so what the wrapper absorbs lands under the component and names the instance.
- Do give a service that is not a component a required `ILogger<T>` through its constructor, and have its registrar `TryAdd` the `NullLogger<>` fallback beside it, so the service resolves in a host that configures no logging.
- Do write every message through `[LoggerMessage]` on one `internal static partial class {Feature}Log` per feature, named for the event it records, `CircuitDroppedWhileCalling`, so every message a feature can emit is read in one place.
- Do log at `Debug` what the library absorbs by design — a dropped circuit or a thrown script behind a `Try*` call, an asset that would not load, a plural argument that is not a count — so the silence is visible to whoever turns the category on and nothing changes for a host that does not.
- Do leave normal behavior silent because it is normal: a superseded delay, resolve or fetch, a disposal race, a failure the component already hands to its host as a domain object. Silence follows from the behavior being expected, never from a wish to keep every absorbed failure at `Debug`.
- Do name a message for the event it records and keep application and user data out of it: a plural argument is reported by its name and type, never its value.
- Don't log above `Debug` without deciding it for that one site; `Warning` and `Error` are never applied in bulk. A plugin assembly that loads only partially is a `Warning`, because it can leave plugins unavailable.
- Don't log at a call site what the wrapper it calls already reports; a discarded `Try*` result is covered by the wrapper's own message.
- Don't write to the console or through an abstraction of the library's own; `ILogger` is what every host already configures and filters.

## Services

- Do place a contract and the sealed implementation shipped with it in the feature assembly, `IXService.cs` beside `XService.cs`.
- Do extend `INotifyStateChanged` when a component must rerender after service state changes.
- Do compose `StateNotifier` for `Func<Task>` subscribers, validate arguments, and put an optional `CancellationToken` last.
- Do subscribe from the component in `OnInitialized` and unsubscribe in `DisposeAsyncCore`.
- Do let a host replace any service by registering its own before the registrar runs, which is what `TryAdd` is there to allow.
- Do register a service the whole application shares, and construct one per component instance where each component owns its own state, as an editor owns its own undo history.
- Don't ship a second registrar from the components assembly; the aggregate registrar is the library's only one, and a feature's own belongs at that feature's root.

## Showcase page

Every component has a page in the showcase, in the category folder matching the component's, with its own locale files. The showcase exists to be looked at, so a state it never renders is a state nobody has ever reviewed.

- Do open the page as the showcase's other pages open, and structure it as a heading and a description followed by one section per scenario, each with a heading and a box.
- Do render the component in every state it can enter, not merely name the state: default, filled, required, disabled, invalid, plus whatever the component adds — empty collection, no search matches, over-limit count, indeterminate, loading.
- Do make each state reachable on the page. A validation state needs a model rule and a submit, not a paragraph describing one.
- Do treat the component's locale section as the inventory: if a key in it cannot be displayed in the showcase, a state is missing.
- Do localize all page copy so every culture switcher exercises direction and translation.
- Do let the page follow the culture state, which the showcase's shared page base does for every page beneath the pages' folder, so a switch made in place renders the page's own copy again in the new language; a showcase component elsewhere that shows text in the culture subscribes to the state itself.
- Don't count a state as covered because the parameter exists; count it when the page renders the component standing in it.
- Don't treat the showcase page as optional or as documentation that can be added later.
