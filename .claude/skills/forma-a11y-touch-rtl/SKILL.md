---
name: forma-a11y-touch-rtl
description: The three gates a Forma component must pass before review — accessibility (colour-independent state, contrast, ARIA, focus and keyboard), touch and pointer (24x24 and 44x44 targets, hover media queries, Pointer Events gestures), and RTL/LTR parity (logical properties, mirroring, arrow-key direction). Includes how to verify each in a real browser rather than in bUnit. Use when finishing, reviewing, or auditing a component, or when writing CSS that positions, mirrors, or communicates state.
---

# The three gates

A component is not finished until it works without colour, without a mouse, and without sight — and until it mirrors cleanly. For each gate below, assume no legitimate exception exists; if you believe you have found one, raise it rather than shipping without.

# Accessibility

## State and contrast

- Do pair colour with a second channel for every state: a glyph, text, weight, underline, position, shape, or another persistent visual difference.
- Do treat existing text that states the condition as a valid second channel.
- Do meet 4.5:1 text contrast, 3:1 for large text, and 3:1 for meaningful non-text indicators.
- Do expose both sides of a state indicator as tokens so a host can retune contrast without forking CSS.
- Do take a text tone from its role and nowhere else: content and every enabled control's label or glyph from `--text-color` or `--control-quiet-foreground-color`, supporting text — a caption, a count, a placeholder, an empty state, a hint — from `--text-secondary-color`, and a disabled control alone from `--control-disabled-foreground-color`.
- Don't paint an enabled control in a grey near the disabled tone, and don't invent a fourth grey; an enabled control that reads as disabled is a defect, whatever each tone measures on its own.
- Don't rely on colour or opacity alone to communicate on/off, selected, current, validity, severity, disabled, sorted, or dragging states.

Opacity changes lightness while preserving shape, so it is too weak for anything more important than a disabled affordance. White on a mid-grey track measures roughly 2:1 and fails; darken the glyph instead of lightening the track.

## ARIA

- Do give every interactive element a useful accessible name.
- Do localize labels for icon-only and glyph-only controls; a day button should announce the full date, not only `15`.
- Do prefer native semantic elements and add the roles and states required by the pattern, including `aria-selected`, `aria-expanded`, `aria-controls`, `aria-current`, `aria-pressed`, and `aria-disabled`.
- Do place live regions in the DOM before inserting unsolicited updates such as toasts, validation summaries, and reorder announcements.
- Do use `role="status"` or `aria-live="polite"` for ordinary updates and reserve assertive announcements for genuine interruptions.
- Do hide decorative glyphs, swatches, and redundant state marks with `aria-hidden="true"`.
- Don't nest an alert inside a polite live region and cause duplicate announcements.
- Don't set `aria-label` when a real associated `<label>` already names the element; it overrides that label.

## Focus and keyboard

- Do give every focusable element a visible `:focus-visible` indicator, preferably a real outline that survives forced-colours mode.
- Do make every operation keyboard reachable in a sensible order.
- Don't remove the outline unless an equally visible replacement exists.
- Don't rely on `box-shadow` alone for focus indication.

## Verification

- Do assert roles, `aria-*`, rendered state marks, and decorative hiding in bUnit alongside behavioural tests.
- Do verify stylesheet-dependent states in a browser because bUnit cannot see CSS.
- Do run that browser **visible** rather than headless, so the maintainer can watch what is being exercised.
- Do inspect computed styles and screenshots in `he` and `en` or `es-419`.
- Do desaturate or squint at the component; if two states become identical, add a second channel.
- Don't consider accessibility complete based only on markup tests.

# Touch and pointer

Every component must remain fully usable with a finger.

## Target size and tapping

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

## Hover

- Do place every `:hover` rule inside `@media (hover: hover)`.
- Do keep pointer-only previews optional while ensuring the task can still be completed by tapping.
- Don't reveal required controls or information only through hover.
- Don't let hover control `display`, `visibility`, opacity, or pointer events for something a touch user must reach.

## Gestures

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

## Small viewports and verification

- Do clamp popups to the viewport and let wide layouts wrap or stack, for example with `max-inline-size: calc(100vw - 1rem)` and `flex-wrap: wrap`.
- Do verify touch in a real browser with a coarse pointer because bUnit cannot produce meaningful pointer behaviour.
- Do measure every `button`, `input`, `select`, and `textarea` and fail targets below 24×24.
- Do drive gestures through CDP `Input.dispatchTouchEvent`, scroll targets into view before computing coordinates, and assert that a drag survives movement.
- Do verify that a quick swipe leaves order unchanged.
- Don't accept an HTML5 drag helper as proof of Pointer Events behaviour; it emits no pointer events at all.
- Don't infer a broken gesture from an empty event log before checking whether the coordinates actually hit the target.

# RTL / LTR parity

Every component must behave and look the same in both directions — mirrored, not different.

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
