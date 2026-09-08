---
name: forma-styling
description: Forma's stylesheet standard — CSS isolation and the token files, logical properties, colour and measurement tokens with no literal fallbacks, composing regions from backdrops on a canvas, the one spacing, type, radius and target scale every component draws from, and responsive behaviour driven by the width a component is given. Use when writing or changing a .razor.css file, adding or renaming a token, laying out regions, or fixing how a component fits a narrow width.
---

# CSS

- Do use CSS isolation and begin each `.razor.css` file with a banner naming the component and its approach.
- Do use logical properties such as `margin-block-end`, `inset-inline-start`, `padding-inline`, and `text-align: start` so RTL works without a second stylesheet.
- Do read colours, fonts, and shadows from `src/Forma.Blazor/wwwroot/forma-tokens.css`.
- Do end every custom property containing a colour with `-color`.
- Do make component-local colour knobs fall back to a token: `var(--forma-toggle-switch-on-bg-color, var(--primary-color))`.
- Do reuse existing tokens such as `--primary-color` and `--focus-color`; a role's `-emphasis`, `-subtle` and `-fg` members and `--focus-color` derive from its solid, and every gray points at a step of the scale.
- Do keep `forma-tokens.css` value-only, `forma-reset.css` limited to cross-browser normalization, and `forma-base.css` limited to the opt-in element-level look.
- Do keep component rules in the component's isolated stylesheet.
- Do rely on the reset's `font: inherit` for form elements, set only the needed `font-size`, and use `var(--font-family-mono)` instead of bare `monospace`.
- Do animate decoration only; every state must remain understandable without motion.
- Don't use physical direction properties such as `left`, `right`, `margin-left`, or `padding-right`.
- Don't hardcode a colour or a measurement as a `var()` fallback, or invent aliases for existing tokens; a global token is defined once and needs no fallback at all, and a component's own hook falls back to a global token, because a literal fallback restates the value at every call site where a theme override cannot reach it.
- Don't add component rules to any global stylesheet.
- Don't set `font-family` on form elements.
- Don't use motion as the only channel communicating state.

## Backdrops on a canvas

A page is a canvas with backdrops laid on it, the way VS Code's sections stand on its workbench. A backdrop is told apart from the backdrop beside it by tone or by the seam between them, never by a line drawn across it. The vocabulary — backdrop, variant, canvas, seam, stack, scrim, frame, chrome, window, workspace — is defined in `CLAUDE.md`.

- Do compose a region from `FormaBackdrop` and `FormaStackLayout` rather than drawing a panel's fill, corner, shadow or gap in a component's own stylesheet; a component that hand-draws a card is a component the backdrop model cannot theme.
- Do give each variant its own token, `--backdrop-{variant}-bg-color`, so a theme restyles every backdrop by redefining that set.
- Do tell two backdrops apart by tone, or by the seam between them, and keep the seam one step wide everywhere.
- Do place a `FormaSplitter` between two backdrops of a stack so it stands in the seam, invisible at rest and taking no room of its own.
- Don't draw a border, a rule or a hairline inside a backdrop, and don't give one host a wider or narrower seam than another.

## One scale for size and space

Every measurement a component makes comes from one scale, so a child agrees with its parent and one component agrees with the next. The scale is the 4px grid that Material, Apple's guidelines and Bootstrap all build on, expressed in rem so it follows the reader's font size; nothing about it is Forma's invention, and nothing outside it is a component's to invent.

- Do take every gap, padding and margin from the spacing tokens, and give each step one job: `--space-1` (0.25rem) between a glyph and its label inside one control and between the backdrops laid on a canvas, `--space-2` (0.5rem) between controls in a row and as a control's block padding, `--space-3` (0.75rem) as a control's inline padding and between groups in a row, `--space-4` (1rem) between blocks and as a panel's padding, `--space-5` (1.5rem) and `--space-6` (2rem) between sections.
- Do take every text size from the type tokens — `--font-size-xs` for a caption or a badge, `--font-size-sm` for a target's label and dense data, `--font-size-base` for prose and inputs, `--font-size-lg` for a section or dialog heading, `--font-size-xl` for a page title, `--font-size-2xl` for a display figure — and line height from `--line-height-prose` (1.5) or `--line-height-target` (1.25).
- Do take every corner from the radius tokens: `--radius-sm` on a control, `--radius-md` on a card, popover or panel, `--radius-lg` on a dialog or toast, `--radius-pill` on a badge or a chip; `50%` stays for a genuine circle.
- Do keep `em` for what must scale with the text it sits in — a glyph inline with text, sub- and superscript, the typography of authored content — and `px` for a hairline, a focus ring, a shadow offset and a 1–2px optical nudge against a border; everything else is a token.
- Do leave a glyph inline with text at its default `em` size and give a standalone one its `IconSize` on the `FormaIcon` itself — `Small` on a button, in a toolbar, in a menu or in a dense row, `Medium` leading a panel or a cell, `Large` as a section's figure, `Display` as an empty state's or a drop zone's figure — because each size names both an icon token and the artwork grid whose stroke is crisp at it; a stylesheet that sizes an icon from outside defeats that pairing and is kept only as a fallback for a glyph a caller left unsized. Keep a control's target at the 24×24 floor and 44×44 under a coarse pointer, which is what the standard and both platform guidelines specify.
- Do take the box a button, an input or a menu item is pressed or focused on from the target tokens — `--target-size-sm` for an icon button, a compact toolbar cell or a dense row, `--target-size-md` for a button, an input, a toolbar button or a menu item, `--target-size-lg` for a prominent action, `--target-size-xl` for a field that stacks a label over its value, `--target-size-touch` for any of them under a coarse pointer — so a target's size is one decision made once, and an input stands exactly as tall as the button beside it; a box that is not a target, such as a thumbnail or a figure, takes an icon token instead.
- Do derive a control's padding from the step it stands on rather than the other way round: the step is fixed, the text line is known, and the padding is what remains.
- Do derive a width threshold from the content it guards and say so beside it — the widths of the items that must fit, added up — rather than from a device; a tier is arithmetic, not a guess.
- Don't write a bare `rem`, `em` or `px` for spacing, type, radius or a control's box in a component; a value not on the scale is either a token that is missing, which is added, or a mistake.
- Don't reach for `overflow: auto` to make a row or a table fit; sideways scrolling inside a page is the last resort after packing, hiding by priority and stacking, and a table drops its least important columns before it scrolls.

## Responsive by design

A component is handed a width, never a device: it sits in a phone's full column, a tablet's half, a desktop's dialog or a sidebar's sliver, and must fit whatever it is given rather than a viewport it assumed.

- Do size in `rem` for spacing, radii and type so a reader's own font size carries through, and keep `px` for a hairline, a focus ring, a shadow, and the 24×24 and 44×44 target minimums the standard defines in CSS pixels.
- Do adapt to the component's own width with a `@container` query, and reserve `@media` for facts about the reader — hover, pointer, motion, forced colours — or the viewport as a whole; a component inside a narrow pane is narrow whatever the window is.
- Do choose how a row gives way when it cannot fit, and make it the design rather than the accident: a bar of commands scrolls behind arrows, a pager hides the pages it cannot show, a two-column layout stacks. Wrapping onto a second line is a choice too, and rarely the right one for a control row.
- Do pack before you stack: a grid of tiles or cells shrinks them to keep several across a narrow column, and a pager keeps as many pages as fit, because one column of full-size items is a long scroll that shows less, not a layout that adapted.
- Do let a flex item shrink: an input has an intrinsic width, and a flex child defaults to `min-inline-size: auto`, so either pushes its parent wider than the page unless told `min-inline-size: 0`.
- Do give a wheel or a gesture to the nearest thing that can answer it; a popover inside a scrolling bar takes the wheel before the bar does.
- Do check every component at a phone's width and a tablet's before review, in a real browser and by looking, because a page can pass a width measurement while wrapping, clipping, or hiding what a reader needs.
- Don't size a component against `100vw`; the viewport is not the space it was given.
- Don't set a fixed `inline-size` on a control row or a floor wider than a phone; `min(…, 100%)` keeps a floor without pushing the page.
