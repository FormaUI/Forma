# Forma.Blazor

A reusable Blazor **component library**. **.NET 11 · C# latest · Razor Class Library · Interactive Server · JS-free by default.**

## Task guides

Detailed standards live in `.claude/skills/` and load on demand. Read the matching guide before starting that kind of work — each is authoritative for its area.

| Skill | Read it before |
|---|---|
| `forma-scripting` | Writing or changing anything under `wwwroot/scripts/` or in an interop assembly |
| `forma-checking-a11y-touch-rtl` | Finishing or reviewing a component — the three gates it must pass |
| `forma-localizing` | Adding user-facing strings or `.he.json` / `.es.json` files |
| `forma-testing` | Writing bUnit tests or `wwwroot/scripts/` Vitest tests |
| `forma-authoring-components` | Creating or changing a component — its files, parameters, options record, contracts, composition, disposal, or a service it uses |
| `forma-styling` | Writing or changing a stylesheet or a token — regions on backdrops, spacing, type, radii, targets, and fitting a width |
| `forma-verifying` | Building, testing, formatting, running the Showcase, checking in a browser, or adding a task script |
| `forma-naming` | Naming anything — a type, member, variable, parameter, file, CSS class, script or test |
| `forma-documenting` | Writing or changing a summary, comment, banner, README passage or inventory row |

## Source of truth

- Do use [COMPONENTS.md](COMPONENTS.md) for the component inventory, build order, and completion status.
- Do use [README.md](README.md) for host consumption — install, service registration, stylesheets, and localization from the consumer's side.
- Do use [docs/composer-design.md](docs/composer-design.md) for `FormaComposer`'s architecture, plugin model, assemblies, and phases.
- Do use [docs/input-routing.md](docs/input-routing.md) for how input is owned and routed between nested components — scopes, routes, active ordering, and the native table — before adding or changing any key, pointer, wheel, drag, or dismissal handling.
- Do use [docs/window-design.md](docs/window-design.md) for window management — identity, origin, parent–child windows, the selector, the manager, activation, stacking, minimization, modality, bounds, and the rest area — before adding or changing anything under `Components/Windows`.
- Do use [docs/component-base-design.md](docs/component-base-design.md) for the component bases and the services they carry — `FormaComponentBase`, `FormaElementBase`, `FormaInputBase<TValue>`, the null objects, logging and localization from the base — before adding or changing a base class, a fallback registration, or a log message.
- Do put a component's own design document in `docs/` when it is large enough to own a plan, and treat it as authoritative for that component over the inventory's summary row.
- Do read `.editorconfig` before writing C#; it is the authority on style, and the root file and `src/Forma.Blazor/.editorconfig` differ deliberately.
- Do use `EpicaDeportiva/docs/forma-components.md` for Épica Deportiva's required and optional subset and remaining work.
- Do treat **Coi** (`c:\Users\eyal\Development\Projects\Coi`) and **Épica Deportiva** (`c:\Users\eyal\Development\Projects\EpicaDeportiva`) as the current consumers.
- Don't invent scope, status, packaging, or style rules when an authoritative file defines them.

## Solution structure

- Do name every project family + concept, with the family one of `Framework`, `AddOns`, or nothing for the product: `Forma.Framework.Icons`, `Forma.Framework.Core.Interop`, `Forma.AddOns.Emojis.Twemoji`, `Forma.Blazor`, `Forma.Framework.Composer`.
- Do put in `Forma.Framework.*` what other Forma assemblies build on — contracts, defaults, base shells, design vocabulary, horizontal concerns — whether or not it is visual; a Framework assembly references only Framework. A concept's browser side, its scripts and the wrappers that reach them, is a `Forma.Framework.{Concept}.Interop` assembly built on `Forma.Framework.Core.Interop`, which holds the interop base and the scripts every other script builds on; the behaviours and operations every component may attach are `Forma.Framework.Common.Interop`.
- Do keep the product to `Forma.Blazor`; what its components cannot run without and a host does not choose is a Framework assembly, either a default shipped beside its contract and named for the contract plus what it supplies, `Forma.Framework.{Concept}.{Supply}`, such as Unicode emoji or the library's own artwork, or a concept of its own, such as culture routing.
- Do make a `Forma.AddOns.*` project a host opt-in extending something: the host references and registers it itself, and it ships as its own NuGet package rather than inside the product's.
- Do answer "required or optional?" by family alone: Framework is required, AddOns are optional.
- Do decide by ownership, not by how many assemblies consume it: a contract a single feature owns stays with that feature no matter how many projects reference it, such as `IToastService` in `Feedback`.
- Do keep a folder and its namespace in step, so `Forma.Blazor/Components/Symbols/` holds `Forma.Blazor.Components.Symbols` and `Forma.Framework.Media/Providers/` holds `Forma.Framework.Media.Providers` — the namespace is the root namespace plus the folder path.
- Don't reference an AddOn from anywhere inside Forma — the Showcase and tests demonstrate AddOns, nothing else touches them — and don't let a dependency point upward: the product references Framework and the product, an AddOn references Framework and whatever it extends.

- Do make components depend on abstractions such as `IToastService`; let the host choose implementations through DI.
- Do represent colours and other CSS values with typed values such as `CssColor.FromRgba(...)` and `CssColor.FromHex(...)`.
- Do give every assembly one root namespace unique to it: its project name, so `Forma.Blazor` holds `Forma.Blazor.*` and `Forma.Framework.Icons` holds `Forma.Framework.Icons.*`.
- Do map an AddOn's namespace to what it extends — an AddOn owns no namespace of its own — so `Forma.AddOns.Share.Targets` compiles into `Forma.Framework.Share.Targets` and `AddOns` never appears in code; an AddOn whose namespace has nothing to map to extends nothing and is mis-filed.
- Do keep these identities distinct: assembly and namespace `Forma.Blazor`, package ID `2Site.Forma.Blazor`, and one static asset base path per assembly that ships assets, `_content/{assembly name}`.
- Do let `Forma.Blazor` reference the feature assemblies its components need and no more; anything a component works without belongs behind a package a host opts into.
- Do ship a feature as a package of its own only when it is optional — interchangeable content such as an artwork set, which a host adds deliberately — carrying its own assets, with the contracts it needs travelling inside it through `PrivateAssets="all"`.
- Do let the components' own package carry the features they depend on, so a host installing the library needs nothing else to render.
- Do give a feature with services to register a `ServiceCollectionExtensions` at its project root, registering with `TryAdd` so a host that registered its own first still wins, and name the method for what lands in the container — an artwork package adds a pack named for its source, `AddFormaTwemojiPack()`, while a feature whose defaults are services is `AddFormaFeedbackServices()`.
- Do ship exactly one aggregate registrar from `Forma.Blazor`, `AddFormaUI()`, fanning out to every default a component needs, so a host makes one call and still overrides any single service by registering it first.
- Do let a component whose optional contract is unregistered fall back to a default the library carries rather than failing, as `FormaIcon` does with the library's own glyph artwork; a contract carrying data nobody can synthesise, such as an emoji catalog, instead leaves the component in its empty state.
- Don't represent typed CSS values as arbitrary strings.
- Don't set `AssemblyName` where it would only repeat the project name, and don't give a project a `PackageId` unless a host is meant to install it by name; everything else ships inside whichever package needs it.
- Don't rename or conflate these identities; a static asset base path is hardcoded by markup and JS, so it follows its assembly's name exactly and never a namespace.

## Dependencies

A project's references are part of its contract, because every consumer inherits them.

- Do reference the narrowest package that supplies what the code uses.
- Do confine `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to projects that genuinely compile against ASP.NET Core.
- Do give every project its own reference for what it uses directly, test projects included, rather than relying on one arriving transitively.
- Do let the build settle it: NU1510 marks a package the framework already supplies, and an unresolved type marks one that is genuinely missing.
- Don't reach for a shared framework to obtain a handful of types; `Forma.Framework.Localization` needs `Microsoft.Extensions.Localization.Abstractions` alone.
- Don't keep a reference no code uses.

## Legacy Coi

Every component derives from the legacy Coi admin at `c:\Users\eyal\Development\Projects\2Site\Coi\Coi\`.

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` and `admin/pops/*.aspx` |
| Behaviour and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

- Do study the matching legacy markup, code-behind, styles, and widgets before designing a replacement.
- Do search that folder from the terminal, because it sits outside the workspace: `Select-String -Path 'c:\Users\eyal\Development\Projects\2Site\Coi\Coi\admin\*.aspx' -Pattern '...'`.
- Do preserve Coi's real states, vocabulary, edge cases, and required behaviour.
- Do reimplement visuals with Forma's design tokens and improve accessibility, affordances, and edge-case handling where useful.
- Do judge a component by whether it is good, not by whether it is pixel-identical to Coi.
- Don't treat Coi as a complete specification or an upper limit.
- Don't reproduce outdated visuals merely because Coi used them.
- Don't omit a worthwhile capability because Coi did not have it.

Coi's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of fidelity.

## Vocabulary

A page is a canvas with backdrops laid on it, the way VS Code's sections stand on its workbench. A backdrop is told apart from the backdrop beside it by tone or by the seam between them, never by a line drawn across it. The words, used the same way in code, tokens, docs and the Showcase:

- A **backdrop** is a region of the page, `FormaBackdrop`.
- A **variant** is the look a backdrop takes, `BackdropVariant`, and every backdrop has exactly one: **Flat** lies in its parent and is told apart by tone alone; **Raised** stands off the canvas as a card with a corner and a small shadow; **Inset** is set back a tone, as a header strip or a toolbar band; **Canvas** is the backdrop other backdrops are laid on.
- The **canvas** is a backdrop in the Canvas variant. It is one variant among four, not a different kind of thing.
- A **seam** is the one spacing step of the parent, `--space-1`, showing between two backdrops.
- A **stack** is `FormaStackLayout`: backdrops along one axis, a seam apart, vertical or horizontal.
- A **scrim** is the translucent dark layer a modal stands on, dimming and blocking what lies beneath it: `--scrim-color`, the window's `forma-window-scrim`, and what a native dialog draws as its `::backdrop`. The word is the theatre's, for the gauze that dims the scene behind it, and Material Design's for this layer; it is the library's only word for it, so that "backdrop" means the surface alone.
- A **frame** is `FormaFrame`: a raised backdrop with a box of its own. In a layout it takes the room it is given; placed, it stands in the viewport, moved by the drag region its host designates, resized at its edges and maximized to its bounds. Presented by a parent that says so, it drives that parent's box instead. It can be hidden or disabled, and it knows no workspace.
- A **chrome** is `FormaChrome`: a frame whose drag region is a title bar carrying the icon, the title and the host's commands, with an optional header band, the body and an optional footer band inside the frame.
- A **window** is `FormaWindow`: a chrome floated into the top layer and a member of the workspace, whose frame drives the window's box. It is activated, stacked, minimized, locked and closed; a frame and a chrome are none of those. A window never embeds. A dialog is a modal window.
- A **workspace** is the set of windows one `WindowManager` coordinates, the way a desktop holds the windows on it: which is current, their stacking order, which window opened which, what a minimize or a close takes along, and what the rest area lists. A circuit has one by default; a host that cascades another manager makes a nested one.

## Build and verification

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
npm test --prefix tests/Forma.Blazor.Tests.Scripts
dotnet run --project src/Forma.Blazor.Showcase
```

Every compiler warning is a build failure: `TreatWarningsAsErrors` is on for
src and tests alike, the `.editorconfig` severity table decides what the
compiler reports, and the answer to an unwanted diagnostic is that table,
never a local suppression.

## Working agreement

- Do build, review, and commit one component at a time.
- Do pick the next component from the inventory and Épica's remaining set.
- Do derive parameters, validation, disposal, CSS, ARIA, localization, and tests from established repository patterns.
- Do deliver the complete component set: `.razor`, `.razor.cs`, `.razor.css`, warranted translation or JS files, bUnit tests, and Showcase page.
- Do verify both directions, colour-independent states, accessible names and focus, and 24×24 touch targets before review — the `forma-checking-a11y-touch-rtl` skill carries the full standard.
- Do present the component for review and stop; iterate on that same component until the maintainer declares it complete.
- Do mark completion in both documents after approval: add `✅` to the component heading and build-order entry in `Forma/COMPONENTS.md`, then change `☐` to `✅` and update status in `EpicaDeportiva/docs/forma-components.md`.
- Do finish a piece of work with it unstaged, report what changed and what the gate said, and propose a commit message for the maintainer to run themselves.
- Do present a wholesale rename as a ledger of `current -> proposed` lines with a per-member note on what each member does, and apply only what the maintainer's review of that ledger decides; members it leaves unlisted stay unchanged.
- Do keep changes minimal and on-scope.
- Don't move to the next component before the current one is approved.
- Don't mark a component complete on your own judgement.
- Don't run a state-changing Git command — `add`, `commit`, `push`, `reset`, `restore`, `stash`, a path-scoped `checkout` — without permission for that command, asked for once the work exists and the maintainer can see what is in it. An instruction given up front about work not yet done grants nothing, and neither does permission for an earlier commit; read-only Git needs no permission at all.
- Don't retrofit new conventions onto existing components unless asked.

## Commit messages

A commit message describes the durable change the commit introduces — the
repository after the commit — never the development session that produced it.

- Do make the title summarize the entire commit and its body; the body may expand the title but never tell a broader story.
- Do split a commit whose body one honest title cannot cover, so each commit reads from its title alone.
- Do add a body only where the title alone is not enough, and keep every part of it inside the one change the title names.
- Do state rationale only where the change cannot be understood correctly without it.
- Don't describe how the change was implemented unless the implementation is itself the subject of the commit.
- Don't narrate attempts, discoveries, or discussion, enumerate files or members, or report test counts, build output, or other routine verification.
- Don't append an attribution trailer such as `Co-Authored-By`.
