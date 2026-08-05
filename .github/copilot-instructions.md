# Forma.Blazor — Copilot Instructions

A reusable Blazor **admin component library**. **.NET 11 · C# latest · Razor Class Library ·
Interactive Server · JS-free by default.**

## Source of truth

- [COMPONENTS.md](../COMPONENTS.md) — the component inventory (31 components,
  tiers, old-Coi pattern each one replaces, build order). `✅` on a heading = built.
- [README.md](../README.md) — solution layout, packaging identities, how hosts consume the library.
- `.editorconfig` — style is authoritative there; resolve it with `read_editorconfig` before
  writing C#.

Consumers: **Coi** (`c:\Users\eyal\Development\Projects\Coi`) and **Épica Deportiva**
(`c:\Users\eyal\Development\Projects\EpicaDeportiva`). Épica's subset of the inventory —
required / optional, and what's left to build — lives in `EpicaDeportiva/docs/forma-components.md`.

## The original — read it before designing anything

Every component in this library is derived from the legacy Coi admin at
**`c:\Users\eyal\Development\Projects\2Site\Coi\Coi\`**. Study the original before designing a
replacement; it is where the real state vocabulary, edge cases, and usage live.

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` (orders, leads, products, pages, users, …) and `admin/pops/*.aspx` |
| Behaviour / rendering helpers | the matching `*.aspx.cs` |
| jQuery widgets being replaced | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

That folder is **outside the workspace**, so workspace search tools do not reach it — use
`Select-String` from the terminal.

**Fidelity is pattern-level, not pixel-level.** Take the states, vocabulary, and behaviour from
Coi; re-implement the visuals JS-free with the design tokens. Coi's toggle is a boxy gradient
slider, but `FormaToggleSwitch` is a token-driven iOS-style pill — that is the intended level of
faithfulness.

**Coi is the floor, not the ceiling.** It shows what the admin needs to do and the vocabulary it
does it in — it is not a specification to reproduce. Where Forma can do better, it should: richer
affordances, better accessibility, sturdier edge-case handling. A capability Coi never had is
welcome on its own merits, and "Coi didn't have it" is never a reason to leave one out. Judge
every component on whether it is *good*, not on how closely it tracks the original.

## Solution structure (reference rules)

| Project | Kind | May reference |
|---------|------|---------------|
| `src/Forma.Web.Css` | Class library | *(nothing)* — framework-agnostic CSS value types (`CssColor`) |
| `src/Forma.Blazor.Abstractions` | Class library | Forma.Web.Css — contracts, message/enum types |
| `src/Forma.Blazor.Core` | Class library | Abstractions — service implementations |
| `src/Forma.Blazor` | Razor Class Library | Abstractions **only** — the packaged components |
| `tests/Forma.Blazor.Tests` | Test project | Forma.Blazor, Forma.Blazor.Core |
| `tests/Forma.Blazor.Showcase` | Web app | all of the above — reference host + gallery |

`Forma.Blazor` must **never** reference `Forma.Blazor.Core`. Components depend on the
*interface* (`IToastService`); the **host** picks the implementation via DI.

Colours and other CSS values are typed, never strings: `CssColor.FromRgba(98, 105, 118, 0.12)`,
`CssColor.FromHex(0x2c384a)`. `Forma.Web.*` is the family for framework-agnostic web value
types; keep infrastructure (middleware, DI, hosting) out of it.

Three identities are distinct — never conflate them: assembly/namespace `Forma.Blazor`,
package id `2Site.Forma.Blazor`, static asset base path `_content/Forma.Blazor` (pinned via
`StaticWebAssetBasePath`; markup and JS hardcode these URLs — do not change it).

## Naming

- **Component tags** carry the `Forma` prefix: `<FormaModal>`, `<FormaDataGrid>`.
- **CSS classes** carry the `forma-` prefix: `.forma-toggle-track`.
- **C# types** (services, enums, records) carry **no** prefix — namespaces disambiguate
  (`ToastService`, `ModalSize`, `BreadcrumbItem`).

## File layout per component

Components live in `src/Forma.Blazor/Components/{Category}/`, where `{Category}` is one
of `Layout`, `Feedback`, `Input`, `Data`, `Interactive`, `Editors`, `Specialized`.

```
Components/Input/
├── FormaX.razor          # markup only — @namespace first line
├── FormaX.razor.cs       # code-behind — all logic lives here, never in @code
├── FormaX.razor.css      # CSS isolation, banner comment at the top
├── FormaX.razor.js       # ONLY if there is no HTML/CSS alternative
├── FormaX.resx           # neutral strings, only if the component renders text
├── FormaX.he.resx
└── FormaX.es.resx
```

Supporting types (`ModalSize`, `BreadcrumbItem`, `FormaIcon`) are plain `.cs` files beside
the component that owns them.

## Component authoring rules

- `public sealed partial class FormaX : ComponentBase` — **sealed by default**. Only unseal
  when a child component must reference the parent type (a couple of early components are
  unsealed; new work seals).
- **Code-behind only.** `.razor` holds markup; no `@code` blocks in library components.
  File-scoped namespace, matching the folder path.
- **Members are ordered by role, not alphabetically:** constants → `[Parameter]`s →
  `[Inject]`s → private fields → computed properties → lifecycle overrides
  (`OnInitialized`/`OnParametersSet`/`OnAfterRenderAsync`) → event handlers and private
  helpers → `InputBase` overrides and `Dispose`/`DisposeAsync` last. Parameters stay grouped
  by the feature they belong to so related ones read together.
- **An expression-body arrow starts its own line**, never trailing the signature:
  `private void Close()` then `=> _isOpen = false;` on the next line, indented one level.
- **Async methods carry the `Async` suffix**, event handlers included
  (`HandleAddNewAsync`, `OnJumpKeyDownAsync`).
- **Parameters** are single-line: `[Parameter] public string? Label { get; set; }`. Use
  `[Parameter, EditorRequired]` for mandatory ones. Add
  `[Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? AdditionalAttributes`
  when the root element should accept pass-through attributes, and splat with `@attributes`.
- **Group into an `Options` record** (`FormaPaginationOptions`, `FormaModalOptions`) when any
  of these hold:
  - Three or more `bool`/config parameters share one concern nameable in a single sentence
    ("which parts of the pager are shown", "default close and footer-action behavior") and
    travel together — callers reach for one named combination at a time and multiple call
    sites reuse the same combination as a preset.
  - The concern is one a future phase is expected to grow (roadmap-tracked settings, a
    feature area still being fleshed out) — starting grouped avoids a later breaking
    rename from a bare parameter to a record.
  - The component is designed to be forwarded through a composing parent (`FormaDataGrid`
    passing `PaginationOptions` to `FormaPagination`) — a record is one parameter to thread
    through, not N.

  Keep parameters standalone when each gates an independent feature area with its own
  dependents (`FormaDataGrid`'s `EnablePagination`, `EnableCheckboxes`, `Virtualize` +
  `ItemSize` + `OverscanCount`), or when there's a single flag with no siblings and no
  planned growth. The record is a `sealed record {Component}Options` beside the component,
  `init`-only properties matching the prior defaults, and a static `Default` property when a
  bare `new()` is the common case. Name the component's own parameter `Options`; a composing
  parent that forwards a child's options through prefixes it with the child's concern
  instead (`PaginationOptions` on `FormaDataGrid`) so it doesn't collide with the parent's
  own `Options`, if it has one.
- **XML `<summary>` on every public parameter and public type.** One line, describing *what
  it is* — no history, no cross-references, no "why it lives here".
- **Validate in `OnParametersSet`** with the throw-helpers:
  `ArgumentException.ThrowIfNullOrWhiteSpace(Title)`,
  `ArgumentOutOfRangeException.ThrowIfLessThan(SearchDelay, TimeSpan.Zero)`,
  `ArgumentNullException.ThrowIfNull(...)`. A structural misuse (child outside its parent)
  throws `InvalidOperationException` from `OnInitialized`.
- **Two-way binding:** form inputs derive from `InputBase<TValue>` so they integrate with
  `EditForm` — render `@CssClass` on the input, emit `<ValidationMessage For="@ValueExpression" />`,
  and implement `TryParseValueFromString`. Non-form components use the plain
  `Value` / `ValueChanged` pair.
- **Parent/child composition** (`FormaTabs`/`FormaTab`) uses a `CascadingValue` of the parent;
  the child registers in `OnInitialized` and unregisters in `Dispose`. Registration methods
  on the parent are `internal`.
- **Dispose what you own.** Implement `IDisposable` / `IAsyncDisposable` for
  `CancellationTokenSource`, `Timer`, JS module references, and service subscriptions.
- **Never `.ConfigureAwait(false)` in a component.** `Forma.Blazor` disables CA2007 in its own
  `.editorconfig`: leaving the synchronization context means any state touched after the await
  has to be marshalled back through `ComponentBase.InvokeAsync`. Service code in
  `Forma.Blazor.Core` is off the renderer and does use `.ConfigureAwait(false)` on every await.
- **Generated element ids** use `$"prefix-{Guid.NewGuid():N}"` so multiple instances coexist.

## JavaScript

JS-free is the *default*, not a prohibition. Prefer an HTML/CSS/Blazor mechanism when one
exists — but where interop genuinely makes the component better (behaviour the platform only
exposes to script, a rough edge that cannot be smoothed declaratively), use it. Never contort a
component or ship a worse experience just to stay script-free.

**How much script a fix costs is not a factor.** Where a declarative mechanism exists, use it;
where none does and interop improves the experience, write however much JS the job needs. "It
would take another function" is never a reason to leave a rough edge in the component.

The constraints on interop are what it's made of, not whether it's allowed:

- **No external libraries.** Native, modern browser APIs only — no jQuery, no polyfills, no npm.
- **Small and colocated.** A `FormaX.razor.js` ES module next to the component, doing one thing.
  Behaviour stays in C#; JS handles only what the DOM won't do otherwise.
- **Name parameters for what they are, never for their type** — `input`, `dialog`, `label`, not
  `inputEl` / `dialogElement` / `el`. The JSDoc `@param {HTMLInputElement}` already states the
  type; repeating it in the name is noise.
- Import it in `OnAfterRenderAsync(firstRender)` from
  `./_content/Forma.Blazor/Components/{Category}/FormaX.razor.js`.
- Wrap every `JS` call in `try { … } catch (JSException) { }` — the circuit can drop mid-call —
  and dispose the module in `DisposeAsync`, catching `JSDisconnectedException`.

## CSS

- CSS isolation (`.razor.css`) per component, opened by a banner comment naming the component
  and its approach.
- **Logical properties everywhere** (`margin-block-end`, `inset-inline-start`, `padding-inline`)
  so RTL works without a second stylesheet. Never `left`/`right`/`margin-left`.
- Colors, fonts, shadows come from the design tokens in `src/Forma.Blazor/wwwroot/forma.css`
  via `var(--main)`, `var(--border-color)`, … Component-local knobs use a fallback:
  `var(--forma-toggle-on-bg, #0d6efd)`. Never hardcode a raw color that a token covers.
- Accessibility is part of "done" — see the section below.

## Accessibility (non-negotiable)

**A component is not finished until it works for someone who cannot see colour, cannot use a
mouse, and cannot see the screen at all.** Like RTL parity, this is a correctness requirement,
not polish. Assume there is no legitimate exception; if you think you've found one, raise it
rather than shipping without.

### Colour is never the only signal

Every state a component can be in — on/off, selected, current, valid/invalid, severity,
disabled, sorted, dragging — must be distinguishable **with the colour removed**. Pair the
colour with a second channel:

- a **glyph** (`FormaToggleSwitch` puts ✓/✕ on the track, `FormaToastContainer` gives each
  severity ✓/✕/⚠/ℹ),
- **weight** or an **underline** (`FormaPagination`'s current page, `FormaDatePicker`'s
  in-range days),
- **position** or **shape** (a knob that slides, a drop indicator that moves, a checkbox that
  switches from tick to dash).

Opacity alone does not count — it changes lightness but keeps the same form, so it is weak for
anything more important than a disabled affordance. Where the component already renders text
that states the condition, the text *is* the second channel and a glyph is optional polish.

### Contrast

- Text: **4.5:1** against its background (3:1 for large text).
- Non-text indicators — glyphs, focus rings, state marks, borders that carry meaning:
  **3:1** (WCAG 1.4.11). White on a mid-grey track is roughly 2:1 and fails; darken the glyph
  instead. Expose both sides as tokens (`--forma-toggle-off-mark`, `--forma-toggle-on-mark`) so
  a host can retune without forking the CSS.

### ARIA

- **Every interactive element has an accessible name.** An icon-only or glyph-only button
  (`❮`, `×`, `⠿`) has none until you add `aria-label` — and that label is user-facing text, so
  it comes from the localizer, never a literal.
- **Name things usefully, not minimally.** A calendar day button named `"15"` is technically
  named and practically useless; name it with the full date.
- Use the roles a pattern requires (`switch`, `tablist`/`tab`/`tabpanel`, `dialog`, `list`) and
  the state that goes with them (`aria-selected`, `aria-expanded`, `aria-controls`,
  `aria-current`, `aria-pressed`, `aria-disabled`). Prefer a native element whose semantics are
  already correct over a div plus a role.
- **Anything that appears without the user asking for it needs a live region** — toasts,
  validation summaries, reorder announcements. The region must exist in the DOM *before* the
  content is inserted, or nothing is announced. `role="status"` / `aria-live="polite"` for
  ordinary updates; reserve `assertive` for genuine interruptions, and never nest an `alert`
  inside a polite region or it speaks twice.
- **Decorative content is hidden**: `aria-hidden="true"` on glyphs, swatches and state marks
  whose meaning is already carried by a label or the element's own state. A mark inside an
  already-hidden subtree needs nothing extra.
- **Don't let `aria-label` shadow a real `<label>`.** If the element has an associated label,
  `aria-label` silently overrides it — set it only when no label exists.

### Focus and keyboard

- Every focusable element has a visible `:focus-visible` ring. `outline: none`, `outline: 0`
  and `outline: 2px solid transparent` are only acceptable when something equally visible
  replaces them — and a `box-shadow` alone disappears in forced-colours mode, so prefer a real
  outline.
- Everything reachable and operable by keyboard, in a sensible order.

### Verifying

- bUnit asserts the **attributes**: roles, `aria-*`, that a mark renders and is hidden. Add
  those tests alongside the behavioural ones.
- bUnit cannot see CSS, so a state conveyed by a stylesheet has to be checked in the browser —
  read the computed style, and screenshot the element in `he` *and* `en`/`es-419`.
- Squint at it, or desaturate the screenshot: if two states become identical, the second
  channel is missing.

## RTL / LTR parity (non-negotiable)

**Every component behaves and looks identical in RTL and LTR — mirrored, not different.** This
is a correctness requirement, not a polish item. Assume there is no legitimate exception; if
you think you've found one, stop and raise it rather than shipping a direction-specific hack.

- **Never** write physical CSS: no `left`/`right`, `margin-left`/`padding-right`,
  `border-left`, `text-align: left/right`, `float`, `transform: translateX(+n)` for offsets, or
  physical `background-position`. Use the logical equivalents — `inset-inline-start/end`,
  `margin-inline-start/end`, `padding-inline`, `border-inline-start`, `text-align: start/end`,
  `inset-inline`.
- **No `[dir="rtl"]` override blocks** and no direction-conditional CSS. A rule that needs an
  RTL counterpart is a rule written with the wrong property. The one legitimate use of
  direction in markup is setting `dir` on a subtree whose *content* has fixed directionality
  (e.g. a numeric/latin field inside an RTL page).
- **Directional glyphs.** Chevrons, arrows, and "next/previous" affordances must point
  *forward in reading order*. Note that browsers already mirror characters with the Unicode
  `Bidi_Mirrored` property — `❮` U+276E, `❯` U+276F, `‹`, `›`, `<`, `>` — so the *same* source
  character renders `❮` in LTR and `❯` in RTL automatically. **Do not flip these in code**
  from `IsRightToLeft`; that double-flips and points them inward. Only glyphs that are *not*
  bidi-mirrored (most icon fonts and SVGs) need explicit mirroring.
- **Keyboard semantics mirror too.** `ArrowLeft`/`ArrowRight` in a tablist, slider, or
  calendar mean *previous/next in reading order*, so their meaning swaps under RTL. Resolve
  them against `CultureInfo.CurrentCulture.TextInfo.IsRightToLeft`, never against a fixed axis.
  `ArrowUp`/`ArrowDown`, `Home`/`End`, `Enter`, `Escape` are unaffected.
- **Popup/menu alignment** anchors to inline-start/inline-end, so a dropdown that hangs off the
  start edge in LTR hangs off the start edge in RTL — the mirror image, not the same pixel side.
- Numbers, dates, and currency format through `CultureInfo.CurrentCulture`. A digit run such as
  `29.7.2026` already renders correctly inside an RTL box, and a `start - end` pair already
  orders start-first in reading order — verify before adding `<bdi>` or isolate characters.
- **Verify both — with your eyes, at readable size.** Every showcase page must be checked in
  `he` (RTL) *and* `en`/`es-419` (LTR). `textContent` and element coordinates show the *source*
  character and cannot reveal bidi mirroring; screenshot the individual element. In RTL,
  "first" means rightmost — read right-to-left before concluding something is reversed. Where
  direction affects logic (arrow keys, popup side), add a bUnit test that sets an RTL
  `CultureInfo` and asserts the mirrored outcome.

## Localization

- **No hardcoded user-facing strings** in a component. Inject
  `[Inject] private IStringLocalizer<FormaX> L { get; set; } = default!;` and read `L["Key"]`.
- Every localized component ships the resx trio: `FormaX.resx` (neutral/en), `FormaX.he.resx`,
  `FormaX.es.resx` — all three keys in sync.
- Caller-supplied text (`Title`, `Label`, `ConfirmText`) is the host's job to localize; the
  library only localizes strings it invents itself.
- Format dates/numbers against `CultureInfo.CurrentCulture`; respect
  `TextInfo.IsRightToLeft` when direction matters.

## Services (Abstractions + Core)

Service-driven components (toast, confirm dialog) follow one shape:

- Contract in `Forma.Blazor.Abstractions/Components/{Category}/IXService.cs`, extending
  `INotifyStateChanged` when components must re-render on change.
- Implementation in `Forma.Blazor.Core/Components/{Category}/XService.cs` — `sealed`,
  composes `StateNotifier` for `Func<Task>` subscribers, validates its arguments, and takes an
  optional trailing `CancellationToken`.
- The component subscribes in `OnInitialized` and unsubscribes in `Dispose`.
- Registration is the **host's** job (`AddScoped<IXService, XService>()`), documented in the
  README — the library ships no `AddForma…` extension.

## Tests (bUnit + xUnit v3)

Mirror the source path: `tests/Forma.Blazor.Tests/Components/{Category}/FormaXTests.razor`
plus `.razor.cs`.

- `.razor.cs` is one line: `public sealed partial class FormaXTests;`
- `.razor` starts `@inherits BunitContext` and holds a nested `public sealed class Component`
  containing the `[Fact]` methods.
- Test names read `Should_render_checked_when_value_is_true`; bodies use explicit
  `// Arrange` / `// Act` / `// Assert` comments.
- Instantiate the context as `using var ctx = new FormaXTests();` and render with
  `ctx.Render(@<FormaX … />)`.
- `InputBase<T>` components render inside `FakeFormHost` with a `FakeModel` and an `EditContext`.
- Assert on rendered markup (`cut.Find(".forma-x")`, attribute values) and on callback
  invocations — not on private state.
- Services get plain xUnit tests under `tests/Forma.Blazor.Tests/Services/{Category}/`.
- Test only the **public** surface. Shared usings live in `_Imports.cs` / `_Imports.razor`.

## Showcase page

Every component gets a gallery page — it is the visual review surface, not optional:
`tests/Forma.Blazor.Showcase/Components/Pages/{Category}/FormaXPage.razor` + its resx trio.

```razor
@page "/x-component"
@attribute [ShowcasePage("FormaX")]
@rendermode InteractiveServer
@inject IStringLocalizer<FormaXPage> L
```

Structure it as `<h1>` + `<p class="desc">`, then one `demo-section` (`<h2>` + `demo-box`) per
scenario: basic, with labels, states (default/disabled/error), edge cases. All page copy comes
from the localizer so the `he` / `es-419` switchers exercise RTL and translation.

## Build & verify

```powershell
dotnet build Forma.slnx
dotnet test
```

Both must be green before a component is offered for review. Run the Showcase
(`dotnet run --project tests/Forma.Blazor.Showcase`) to eyeball the component in all three
cultures.

## Working agreement — one component at a time

Components are built, reviewed, and committed **individually**:

1. Pick the next component from the inventory (Épica's remaining set is listed in
   `EpicaDeportiva/docs/forma-components.md`).
2. Derive its design from the components already in the repo — parameters, validation,
   disposal, CSS, ARIA, localization all follow the patterns above rather than inventing new ones.
3. Deliver the full set: `.razor` + `.razor.cs` + `.razor.css` (+ resx / `.js` if warranted),
   bUnit tests, showcase page. Build + tests green, the component verified in **both
   directions** (`he` RTL and `en`/`es-419` LTR), and every state checked against the
   accessibility rules — named, focusable, and readable with the colour removed.
4. **Present it for review and stop.** Iterate on the same component until the maintainer says
   it is complete — do not move to the next one.
5. When the maintainer declares it complete, mark it **in both documents**:
   - `Forma/COMPONENTS.md` — add `✅` to the component heading and to its entry in the
     *Suggested Build Order* table.
   - `EpicaDeportiva/docs/forma-components.md` — flip `☐` → `✅` in the "Built in Forma?"
     column and update the **Status** line (count + remaining list).
6. Commits are per-component. **Never run a state-changing `git` command** (`add`, `commit`,
   `push`, `reset`, …) without explicit permission — present the proposed message and stop.
   Read-only git is fine.

Keep changes minimal and on-scope. Don't retrofit new conventions onto components that already
exist unless asked.
