---
name: forma-showcase
description: How to build a Forma Showcase page — the required path and directives, the h1/description/demo-section/demo-box structure, and the rule that every state a component can enter must actually be rendered and reachable rather than merely described, using the translation file as the inventory. Use when adding a Showcase page for a new component, extending one for a new state or parameter, or checking whether a component's states are covered.
---

# Showcase

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

## Running it

```powershell
dotnet run --project src/Forma.Blazor.Showcase
```

- Do stop a running Showcase before rebuilding; it holds its `bin` DLLs open and the copy fails with `MSB3027`.
- Do confirm the binary is newer than the sources you changed before trusting anything you see; the Showcase runs a compiled binary with no hot reload, and a stale process serves old code without saying so.
- Don't pipe the command's output into `Select-String`, `Select-Object`, or another filter; closing the pipeline terminates the server.
