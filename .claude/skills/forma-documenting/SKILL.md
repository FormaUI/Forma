---
name: forma-documenting
description: Forma's standard for prose that describes code — XML summaries, inline comments, CSS banners, README and inventory rows — stating what a thing is, explaining a mechanism only where the code cannot show it, US English with the one spelling exception, inheritdoc on overrides, and keeping every description true when the code it describes widens or changes. Use when writing or changing a summary, comment, banner, README passage or COMPONENTS.md row, and when reviewing prose around code you touched.
---

# Comments and documentation

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

## Keeping prose true as code changes

A description is wrong the moment the thing it describes widens, and nothing in the build will say so. Re-read the prose around whatever you touch.

- Do describe a member at the full breadth of its type, so a parameter typed `IReadOnlyList<FormaToolbarItem>` is "the items the toolbar renders", never "the action buttons" — that hierarchy also holds groups, menus, toggles, readouts, and custom items.
- Do re-check a class-level summary naming what a component is built on whenever its markup changes; "built on `FormaCommandArea` and `FormaCommandBar`" survived long after the markup used only one of them.
- Do drop a "no JavaScript" or "pure CSS" banner the moment the component gains interop, and keep it where it is still true.
- Do rewrite a `COMPONENTS.md` **Parameters** row when a parameter is added, removed, renamed, or changes type or required-ness.
- Do rewrite a **Blazor approach** row once the component is built, so it records what shipped rather than the plan; a row still reading "Use Blazor `InputDate`" for a bespoke calendar is worse than no row.
- Do update a JS module path in prose when the script moves between assemblies, or between `behaviours/`, `operations/`, and `components/`.
- Do delete a sentence saying another component *should* adopt something once it has.
- Don't trust a search for renamed identifiers to find this; the drift is in wording that still compiles and still reads plausibly.
