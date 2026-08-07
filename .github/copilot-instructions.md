# Forma.Blazor — Copilot Instructions

A reusable Blazor **component library**. **.NET 11 · C# latest · Razor Class Library · Interactive Server · JS-free by default.**

## Source of truth

- Do use [COMPONENTS.md](../COMPONENTS.md) for the 36-component inventory, tiers, replaced Coi patterns, build order, and completion status (`✅`).
- Do use [README.md](../README.md) for solution layout, packaging identities, and host consumption.
- Do resolve C# style from `.editorconfig` with `read_editorconfig` before writing C#.
- Do use `EpicaDeportiva/docs/forma-components.md` for Épica Deportiva's required and optional subset and remaining work.
- Do treat **Coi** (`c:\Users\eyal\Development\Projects\Coi`) and **Épica Deportiva** (`c:\Users\eyal\Development\Projects\EpicaDeportiva`) as the current consumers.
- Don't invent scope, status, packaging, or style rules when an authoritative file defines them.

## Study the original before designing

Every component derives from the legacy Coi admin at `c:\Users\eyal\Development\Projects\2Site\Coi\Coi\`.

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` and `admin/pops/*.aspx` |
| Behaviour and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

- Do study the matching legacy markup, code-behind, styles, and widgets before designing a replacement.
- Do use `Select-String` from the terminal because the legacy folder is outside the workspace.
- Do preserve Coi's real states, vocabulary, edge cases, and required behaviour.
- Do reimplement visuals with Forma's design tokens and improve accessibility, affordances, and edge-case handling where useful.
- Do judge a component by whether it is good, not by whether it is pixel-identical to Coi.
- Don't treat Coi as a complete specification or an upper limit.
- Don't reproduce outdated visuals merely because Coi used them.
- Don't omit a worthwhile capability because Coi did not have it.

Coi's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of fidelity.

## Solution structure

| Project | Kind | May reference |
|---------|------|---------------|
| `src/Forma.Web.Css` | Class library | Nothing — framework-agnostic CSS value types |
| `src/Forma.Blazor.Abstractions` | Class library | `Forma.Web.Css` — contracts, messages, enums |
| `src/Forma.Blazor.Core` | Class library | `Forma.Blazor.Abstractions` — implementations |
| `src/Forma.Blazor` | Razor Class Library | `Forma.Blazor.Abstractions` only — packaged components |
| `tests/Forma.Web.Css.Tests` | Test project | `Forma.Web.Css` |
| `tests/Forma.Blazor.Tests` | Test project | `Forma.Blazor`, `Forma.Blazor.Core` |
| `tests/Forma.Blazor.Showcase` | Web app | `Forma.Blazor`, `Forma.Blazor.Core` — reference host and gallery |

- Do make components depend on abstractions such as `IToastService`; let the host choose implementations through DI.
- Do represent colours and other CSS values with typed values such as `CssColor.FromRgba(...)` and `CssColor.FromHex(...)`.
- Do reserve `Forma.Web.*` for framework-agnostic web value types.
- Do keep these identities distinct: assembly/namespace `Forma.Blazor`, package ID `2Site.Forma.Blazor`, static asset base path `_content/Forma.Blazor`.
- Don't reference `Forma.Blazor.Core` from `Forma.Blazor`.
- Don't put middleware, DI, hosting, or other infrastructure in `Forma.Web.*`.
- Don't represent typed CSS values as arbitrary strings.
- Don't rename or conflate the three public identities; `_content/Forma.Blazor` is pinned and hardcoded by markup and JS.

## Naming

- Do prefix component tags with `Forma`: `<FormaModal>`, `<FormaDataGrid>`.
- Do prefix CSS classes with `forma-`: `.forma-toggle-track`.
- Do leave C# services, enums, and records unprefixed because namespaces disambiguate them: `ToastService`, `ModalSize`, `BreadcrumbItem`.
- Do call a thing Forma ships a **component**, and raw HTML an **element** — one word per concept. "Control" is Web Forms vocabulary inherited from Coi; keep it only in `aria-controls` and in descriptions of Coi's own markup.
- Don't apply one naming convention indiscriminately across Razor tags, CSS classes, and C# types.

## Comments and documentation

These rules govern prose that *describes a thing*. They do not apply to this file, or to documents whose purpose is to teach judgment or record decisions.

- Do state only what a thing is or does in XML docs, CSS banners, inline comments, and README files.
- Do comment only when the code cannot express an important constraint, such as a necessary `!important`, a non-obvious `-webkit-` property, or an off-by-one invariant.
- Do keep comments to one line whenever possible.
- Do add a one-line XML `<summary>` describing what every public parameter and public type is.
- Do keep historical origin and usage mapping in the intentional *Old pattern* and *Used in* rows of `COMPONENTS.md`.
- Don't document history, rejected alternatives, decision arguments, roadmap position, call sites, mirrors, or other cross-references.
- Don't restate obvious code or pad files with unnecessary prose.

## Component file layout

Components live in `src/Forma.Blazor/Components/{Category}/`, where `{Category}` is `Layout`, `Feedback`, `Actions`, `Input`, `Data`, `Indicators`, `Interactive`, `Files`, `Media`, or `Editors`.

```text
Components/{Category}/
├── FormaX.razor          # markup only; @namespace first
├── FormaX.razor.cs       # all component logic
├── FormaX.razor.css      # isolated CSS with banner
├── FormaX.razor.js       # only when interop is warranted
├── FormaX.he.json        # translations, when needed
└── FormaX.es.json
```

- Do keep markup in `.razor`, logic in `.razor.cs`, and component styling in `.razor.css`.
- Do place supporting types such as `ModalSize`, `BreadcrumbItem`, and `FormaIcon` beside the component that owns them.
- Do add a colocated `.razor.js` only when HTML, CSS, and Blazor cannot provide the required behaviour.
- Do add the translation pair only when the component invents user-facing text.
- Don't use `@code` blocks in library components.
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
- Don't use `.ConfigureAwait(false)` in `Forma.Blazor` components; leaving the synchronization context means state touched after an await has to be marshalled back through `InvokeAsync`. `src/Forma.Blazor/.editorconfig` sets `CA2007` to `none`, so nothing will flag it for you.
- Don't omit `.ConfigureAwait(false)` from awaited service code in `Forma.Blazor.Core`, which runs off the renderer and where the root `.editorconfig` sets `CA2007` to `error`.

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

## JavaScript

JS-free is the default, not a prohibition.

- Do prefer HTML, CSS, or Blazor when they provide the behaviour cleanly.
- Do use interop when the platform exposes the needed behaviour only to script or when declarative techniques leave a real usability problem.
- Do use only native modern browser APIs; keep each module small, colocated as `FormaX.razor.js`, and focused on one DOM responsibility.
- Do keep behaviour in C# and use JS only for what the DOM cannot otherwise provide.
- Do name JS parameters by role (`input`, `dialog`, `label`), not type (`inputEl`, `dialogElement`, `el`).
- Do import the module on first render from `./_content/Forma.Blazor/Components/{Category}/FormaX.razor.js`.
- Do catch `JSException` around every JS call because the circuit can drop mid-call.
- Do dispose the module in `DisposeAsync` and catch `JSDisconnectedException`.
- Don't use jQuery, npm packages, polyfills, or other external JS libraries. The sole exception is `FormaRichTextEditor`'s Markdown renderer (#36) — a parser is a solved problem, and it renders only; it never owns an editing surface, a selection, an undo stack or a toolbar. Adding a second exception is a decision for the maintainer, not a precedent already set.
- Don't contort a component or ship a worse experience merely to remain script-free.
- Don't treat the amount of JS required as a reason to leave a rough edge unresolved.

## CSS

- Do use CSS isolation and begin each `.razor.css` file with a banner naming the component and its approach.
- Do use logical properties such as `margin-block-end`, `inset-inline-start`, `padding-inline`, and `text-align: start` so RTL works without a second stylesheet.
- Do read colours, fonts, and shadows from `src/Forma.Blazor/wwwroot/forma-tokens.css`.
- Do end every custom property containing a colour with `-color`.
- Do make component-local colour knobs fall back to a token: `var(--forma-toggle-on-bg-color, var(--main-color))`.
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

## Accessibility

A component is not finished until it works without colour, without a mouse, and without sight. Assume no legitimate exception exists; if you believe you have found one, raise it rather than shipping without.

### State and contrast

- Do pair colour with a second channel for every state: a glyph, text, weight, underline, position, shape, or another persistent visual difference.
- Do treat existing text that states the condition as a valid second channel.
- Do meet 4.5:1 text contrast, 3:1 for large text, and 3:1 for meaningful non-text indicators.
- Do expose both sides of a state indicator as tokens so a host can retune contrast without forking CSS.
- Don't rely on colour or opacity alone to communicate on/off, selected, current, validity, severity, disabled, sorted, or dragging states.

Opacity changes lightness while preserving shape, so it is too weak for anything more important than a disabled affordance. White on a mid-grey track measures roughly 2:1 and fails; darken the glyph instead of lightening the track.

### ARIA

- Do give every interactive element a useful accessible name.
- Do localize labels for icon-only and glyph-only controls; a day button should announce the full date, not only `15`.
- Do prefer native semantic elements and add the roles and states required by the pattern, including `aria-selected`, `aria-expanded`, `aria-controls`, `aria-current`, `aria-pressed`, and `aria-disabled`.
- Do place live regions in the DOM before inserting unsolicited updates such as toasts, validation summaries, and reorder announcements.
- Do use `role="status"` or `aria-live="polite"` for ordinary updates and reserve assertive announcements for genuine interruptions.
- Do hide decorative glyphs, swatches, and redundant state marks with `aria-hidden="true"`.
- Don't nest an alert inside a polite live region and cause duplicate announcements.
- Don't set `aria-label` when a real associated `<label>` already names the element; it overrides that label.

### Focus and keyboard

- Do give every focusable element a visible `:focus-visible` indicator, preferably a real outline that survives forced-colours mode.
- Do make every operation keyboard reachable in a sensible order.
- Don't remove the outline unless an equally visible replacement exists.
- Don't rely on `box-shadow` alone for focus indication.

### Verification

- Do assert roles, `aria-*`, rendered state marks, and decorative hiding in bUnit alongside behavioural tests.
- Do verify stylesheet-dependent states in a browser because bUnit cannot see CSS.
- Do inspect computed styles and screenshots in `he` and `en` or `es-419`.
- Do desaturate or squint at the component; if two states become identical, add a second channel.
- Don't consider accessibility complete based only on markup tests.

## Touch and pointer

Every component must remain fully usable with a finger. Assume no legitimate exception exists; if you believe you have found one, raise it rather than shipping without.

### Target size and tapping

- Do make every target at least 24×24 CSS px.
- Do use 44×44 targets under `@media (pointer: coarse)` without inflating desktop density:

  ```css
  @media (pointer: coarse) {
      .forma-x { min-block-size: 2.75rem; }
  }
  ```

- Do enlarge the hit area with padding or a wrapper rather than always enlarging the artwork.
- Do measure rendered boxes with `getBoundingClientRect` and use `flex-shrink: 0` where layout could squeeze them.
- Do set `touch-action: manipulation` in the base rule of tappable controls.
- Don't treat WCAG spacing or inline-link exceptions as the default.
- Don't use `touch-action: none` except on a surface that owns a drag gesture.

### Hover

- Do place every `:hover` rule inside `@media (hover: hover)`.
- Do keep pointer-only previews optional while ensuring the task can still be completed by tapping.
- Don't reveal required controls or information only through hover.
- Don't let hover control `display`, `visibility`, opacity, or pointer events for something a touch user must reach.

### Gestures

- Do use Pointer Events rather than mouse events for continuous gestures.
- Do provide a touch-reachable equivalent for modifier-key interactions such as shift-click range selection.
- Do require press-and-hold before touch dragging so ordinary swipes can still scroll; let mouse dragging begin immediately.
- Do keep a drag alive across scroll arbitration with a guarded non-passive `touchmove`, because pointer capture does not stop the compositor claiming the touch and cancelling the pointer:

  ```js
  element.addEventListener('touchmove', event => {
      if (isDragging) { event.preventDefault(); }
  }, { signal, passive: false });
  ```

- Don't leave that listener unguarded; an unconditional `preventDefault()` stops ordinary swipes from scrolling.
- Don't assume pointer capture alone prevents `pointercancel` during touch scrolling.

### Small viewports and verification

- Do clamp popups to the viewport and let wide layouts wrap or stack, for example with `max-inline-size: calc(100vw - 1rem)` and `flex-wrap: wrap`.
- Do verify touch in a real browser with a coarse pointer because bUnit cannot produce meaningful pointer behaviour.
- Do measure every `button`, `input`, `select`, and `textarea` and fail targets below 24×24.
- Do drive gestures with CDP `Input.dispatchTouchEvent`, scroll targets into view before computing coordinates, and assert that a drag survives movement.
- Do verify that a quick swipe leaves order unchanged.
- Don't use the browser tool's HTML5 `drag_element` as proof of Pointer Events behaviour; it emits no pointer events.
- Don't infer a broken gesture from an empty event log before checking whether the coordinates actually hit the target.

## RTL / LTR parity

Every component must behave and look the same in both directions—mirrored, not different. Assume no legitimate exception exists; if you believe you have found one, raise it rather than shipping a direction-specific hack.

- Do use logical CSS properties for offsets, spacing, borders, alignment, and positioning.
- Do reach for a logical property that mirrors on its own when a transform would otherwise need a second keyframe; offsetting a relatively positioned element through `inset-inline-start` serves both directions with one animation.
- Do check which edge a logical property resolves to before using it; a divider between a sidebar and its content is `inline-end` in both directions, not `inline-start`.
- Do use `dir` only on content with fixed directionality, such as a Latin or numeric field inside an RTL page.
- Do let Unicode bidi-mirrored characters such as `❮`, `❯`, `‹`, `›`, `<`, and `>` mirror naturally.
- Do explicitly mirror only glyphs that are not bidi-mirrored, such as most icon-font and SVG arrows.
- Do interpret horizontal arrow keys as previous/next in reading order and resolve them through `CultureInfo.CurrentCulture.TextInfo.IsRightToLeft`.
- Do anchor popups and menus to inline-start or inline-end.
- Do format numbers, dates, and currency through `CultureInfo.CurrentCulture` and verify native bidi behaviour before adding `<bdi>` or isolates.
- Do visually inspect every showcase page in `he` and `en` or `es-419`, and add bUnit tests wherever direction affects logic.
- Don't write physical CSS or `[dir="rtl"]` override blocks.
- Don't flip bidi-mirrored characters in code; that double-flips them.
- Don't swap unaffected keys such as `ArrowUp`, `ArrowDown`, `Home`, `End`, `Enter`, or `Escape`.
- Don't rely on `textContent` or coordinates to verify glyph mirroring; screenshot the actual element.

## Localization

- Do inject `[Inject] private IStringLocalizer<FormaX> L { get; set; } = default!;` and read library-owned strings through `L["The English sentence"]`.
- Do ship `FormaX.he.json` and `FormaX.es.json` beside the component, keyed by that English source text; there is no neutral file, because an absent translation falls back to the key itself.
- Do keep the file's name and folder in step with the type's, since the resource is resolved from the type's full name and the JSON's path must match it.
- Do leave caller-supplied text such as `Title`, `Label`, and `ConfirmText` for the host to localize.
- Do format dates and numbers with `CultureInfo.CurrentCulture` and use `TextInfo.IsRightToLeft` when direction changes behaviour.
- Don't hardcode user-facing component strings.
- Don't localize text supplied by the caller a second time inside the library.

## Services

- Do place contracts in `Forma.Blazor.Abstractions/Components/{Category}/IXService.cs`.
- Do extend `INotifyStateChanged` when a component must rerender after service state changes.
- Do place sealed implementations in `Forma.Blazor.Core/Components/{Category}/XService.cs`.
- Do compose `StateNotifier` for `Func<Task>` subscribers, validate arguments, and put an optional `CancellationToken` last.
- Do subscribe from the component in `OnInitialized` and unsubscribe in `Dispose`.
- Do let the host register services explicitly, such as `AddScoped<IXService, XService>()`.
- Don't ship an `AddForma...` registration extension from the library.
- Don't make the packaged component project depend on service implementations.

## Tests (bUnit + xUnit v3)

Mirror source paths under `tests/Forma.Blazor.Tests/Components/{Category}/` with `FormaXTests.razor` and `FormaXTests.razor.cs`.

- Do keep `.razor.cs` to `public sealed partial class FormaXTests;`.
- Do start `.razor` with `@inherits BunitContext` and place `[Fact]` methods in a nested `public sealed class Component`.
- Do name tests like `Should_render_checked_when_value_is_true` and use explicit `// Arrange`, `// Act`, and `// Assert` comments.
- Do create the context with `using var ctx = new FormaXTests();` and render with `ctx.Render(@<FormaX ... />)`.
- Do render `InputBase<T>` components inside `FakeFormHost` with a `FakeModel` and `EditContext`.
- Do assert rendered markup, attributes, and callback invocations rather than private state.
- Do place plain service tests under `tests/Forma.Blazor.Tests/Services/{Category}/`.
- Do test only the public surface and keep shared usings in `_Imports.cs` or `_Imports.razor`.
- Don't couple tests to implementation details or private fields.

## Showcase

Every component requires `tests/Forma.Blazor.Showcase/Components/Pages/{Category}/FormaXPage.razor` and its translation pair.

```razor
@page "/x-component"
@attribute [ShowcasePage("FormaX")]
@rendermode InteractiveServer
@inject IStringLocalizer<FormaXPage> L
```

- Do structure the page as `<h1>` plus `<p class="desc">`, followed by one `demo-section` containing `<h2>` and `demo-box` per scenario.
- Do cover basic usage, labels, default/disabled/error states, and edge cases.
- Do localize all page copy so the `he` and `es-419` switchers exercise RTL and translation.
- Don't treat the gallery page as optional or as documentation that can be added later.

## Build and verification

```powershell
dotnet build Forma.slnx
dotnet test
dotnet run --project tests/Forma.Blazor.Showcase
```

- Do require build and tests to pass before offering a component for review.
- Do inspect the component in all three cultures through the Showcase.
- Don't pipe the Showcase command's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
- Don't present a component as complete while build, tests, accessibility, touch, or direction checks remain unresolved.

## Working agreement

- Do build, review, and commit one component at a time.
- Do pick the next component from the inventory and Épica's remaining set.
- Do derive parameters, validation, disposal, CSS, ARIA, localization, and tests from established repository patterns.
- Do deliver the complete component set: `.razor`, `.razor.cs`, `.razor.css`, warranted translation or JS files, bUnit tests, and Showcase page.
- Do verify both directions, colour-independent states, accessible names and focus, and 24×24 touch targets before review.
- Do present the component for review and stop; iterate on that same component until the maintainer declares it complete.
- Do mark completion in both documents after approval: add `✅` to the component heading and build-order entry in `Forma/COMPONENTS.md`, then change `☐` to `✅` and update status in `EpicaDeportiva/docs/forma-components.md`.
- Do propose a per-component commit message and wait for explicit permission before changing Git state.
- Do keep changes minimal and on-scope.
- Don't move to the next component before the current one is approved.
- Don't mark a component complete on your own judgement.
- Don't run state-changing Git commands such as `add`, `commit`, `push`, or `reset` without explicit permission; read-only Git is allowed.
- Don't retrofit new conventions onto existing components unless asked.
