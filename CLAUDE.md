# Forma.Blazor

A reusable Blazor component library. These are the working rules for this repository; the repository itself, its structure, build, origins and consumers, is described in [docs/repository-structure.md](docs/repository-structure.md), and its words in [docs/repository-vocabulary.md](docs/repository-vocabulary.md).

## Task guides

Detailed standards live in `.claude/skills/` and load on demand. Read the matching guide before starting that kind of work; each is authoritative for its area.

| Skill | Read it before |
|---|---|
| `forma-scripting` | Writing or changing a script or an interop wrapper |
| `forma-checking-a11y-touch-rtl` | Finishing or reviewing a component: the three gates it must pass |
| `forma-localizing` | Adding user-facing strings or translation files |
| `forma-testing` | Writing component tests or script tests |
| `forma-authoring-components` | Creating or changing a component: its files, parameters, options record, contracts, composition, disposal, logging, or a service it uses |
| `forma-styling` | Writing or changing a stylesheet or a token: regions on backdrops, spacing, type, radii, targets, and fitting a width |
| `forma-verifying` | Building, testing, formatting, running the Showcase, checking in a browser, or adding a task script |
| `forma-naming` | Naming anything: a type, member, variable, parameter, file, CSS class, script or test |
| `forma-documenting` | Writing or changing a summary, comment, remarks block, banner, README passage, design document, implementation plan, catalog or roadmap entry, or deciding whether a code change touches prose at all |

## Source of truth

- Do use [docs/repository-structure.md](docs/repository-structure.md) for what the repository is made of: the solution structure, where components, scripts, stylesheets and tests live, the build and verification commands, where components' designs derive from, and who consumes the library.
- Do use [docs/repository-vocabulary.md](docs/repository-vocabulary.md) for the words the library uses for its concepts, and use them the same way in code, tokens, documents and the Showcase.
- Do use [docs/component-catalog.md](docs/component-catalog.md) for the components that exist, and [docs/component-roadmap.md](docs/component-roadmap.md) for the components still to build and the order they are built in.
- Do use [README.md](README.md) for host consumption: install, service registration, stylesheets, and localization from the consumer's side.
- Do read the design document for an area before changing it, `docs/{area}-design.md`, and the implementation plan beside it, `docs/{area}-implementation-plan.md`, which records the steps and what has landed; a catalog or roadmap entry names the design its component belongs to.
- Do give an area that needs a technical design two documents in `docs/`, `{area}-design.md` for the design alone and `{area}-implementation-plan.md` for the steps, their state and what departed from the design; treat the design as authoritative for the area, and keep the catalog entry to a name and one line.
- Do read `.editorconfig` before writing C#; it is the authority on style.
- Don't invent scope, status, packaging, or style rules when an authoritative file defines them.

## Structure

- Do put a thing where its owner is: a contract stays with the feature that owns it however many projects reference it, a supporting type a host names lives in the feature assembly, and a type no caller names stays beside the component.
- Do keep a folder and its namespace in step, and give every assembly one root namespace unique to it, its project name.
- Do let dependencies point one way, from the product to the framework it builds on, never upward, and let an optional package extend the library from outside it rather than be referenced from within.
- Do file a project by what it is: what the library builds on is Framework, whether or not the product uses it; a project that is optional and extends a Framework assembly is an AddOn, a host opt-in the host references and registers itself. Optional alone makes nothing an AddOn.
- Do register a feature's services through one registrar at its root, with `TryAdd`, so a host that registered its own first still wins, and ship one aggregate registrar from the product so a host makes one call and still overrides any single service.
- Do let a component whose optional contract is unregistered fall back to a default the library carries; a contract carrying data nobody can synthesize leaves the component in its empty state instead.
- Do make components depend on abstractions and let the host choose implementations through DI.
- Do represent colors and other CSS values with typed values, never arbitrary strings.
- Don't rename or conflate an assembly's identities; a static asset base path is hardcoded by markup and scripts, so it follows the assembly's name exactly.

## Dependencies

A project's references are part of its contract, because every consumer inherits them.

- Do reference the narrowest package that supplies what the code uses.
- Do confine a shared framework reference to projects that genuinely compile against it.
- Do give every project its own reference for what it uses directly, test projects included, rather than relying on one arriving transitively.
- Do let the build settle it: a package the framework already supplies is reported as redundant, and an unresolved type marks one that is genuinely missing.
- Don't reach for a shared framework to obtain a handful of types.
- Don't keep a reference no code uses.

## Designing from what came before

Every component replaces something in the legacy application the repository document names.

- Do study the matching legacy markup, code-behind, styles and widgets before designing a replacement.
- Do preserve its real states, vocabulary, edge cases and required behavior.
- Do reimplement visuals with the library's design tokens and improve accessibility, affordances and edge-case handling where useful.
- Do judge a component by whether it is good, not by whether it is pixel-identical to what it replaces.
- Don't treat the legacy application as a complete specification or an upper limit, reproduce outdated visuals because it used them, or omit a worthwhile capability because it lacked one.

## Build and verification

The gate is the build with no warnings, every test project, the script tests, the format check, and the Showcase for anything that renders; the repository document lists the commands.

- Do treat every compiler warning as a build failure; the `.editorconfig` severity table decides what the compiler reports, and the answer to an unwanted diagnostic is that table, never a local suppression.

## Working agreement

- Do build, review, and commit one component at a time, taking the next from the roadmap's build order.
- Do derive parameters, validation, disposal, CSS, ARIA, localization, and tests from established repository patterns.
- Do deliver the complete component set: markup, logic, stylesheet, warranted translation or script files, tests, and Showcase page.
- Do verify both directions, color-independent states, accessible names and focus, and touch targets before review; the `forma-checking-a11y-touch-rtl` skill carries the full standard.
- Do present the component for review and stop; iterate on that same component until the maintainer declares it complete.
- Do move a component from the roadmap to the catalog after approval, and from the roadmap's structure to the catalog's when its implementation starts.
- Do finish a piece of work with it unstaged, report what changed and what the gate said, and propose a commit message for the maintainer to run themselves.
- Do present a wholesale rename as a ledger of `current -> proposed` lines with a per-member note on what each member does, and apply only what the maintainer's review of that ledger decides; members it leaves unlisted stay unchanged.
- Do keep changes minimal and on-scope.
- Don't move to the next component before the current one is approved.
- Don't mark a component complete on your own judgment.
- Don't run a state-changing Git command, `add`, `commit`, `push`, `reset`, `restore`, `stash`, a path-scoped `checkout`, without permission for that command, asked for once the work exists and the maintainer can see what is in it. An instruction given up front about work not yet done grants nothing, and neither does permission for an earlier commit; read-only Git needs no permission at all.
- Don't retrofit new conventions onto existing components unless asked.

## Commit messages

A commit message describes the durable change the commit introduces, the repository after the commit, never the development session that produced it.

- Do make the title summarize the entire commit and its body; the body may expand the title but never tell a broader story.
- Do split a commit whose body one honest title cannot cover, so each commit reads from its title alone.
- Do add a body only where the title alone is not enough, and keep every part of it inside the one change the title names.
- Do state rationale only where the change cannot be understood correctly without it.
- Don't describe how the change was implemented unless the implementation is itself the subject of the commit.
- Don't narrate attempts, discoveries, or discussion, enumerate files or members, or report test counts, build output, or other routine verification.
- Don't append an attribution trailer such as `Co-Authored-By`.
