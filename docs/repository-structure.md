# Forma Repository Structure

Forma is a reusable Blazor component library: .NET 11, C# latest, a Razor Class Library rendered Interactive Server, JS-free by default. This document describes the repository as a codebase: what the solution is made of, where things live, what it derives from, who consumes it, and how it is built and verified. The working rules in `CLAUDE.md` refer to it by role. An area with a technical design takes two documents beside this one while it is being built: `{area}-design.md`, the design alone, and `{area}-implementation-plan.md`, the steps and what has landed. The plan is retired once every step has landed and nothing is open, so a finished area carries its design alone.

## Solution structure

Every project is named family plus concept, with the family one of `Framework`, `AddOns`, or nothing for the product: `Forma.Framework.Icons`, `Forma.Framework.JavaScript.Interop`, `Forma.AddOns.Emojis.Twemoji`, `Forma.Blazor`, `Forma.Framework.Composer`. The family says what a project is: Framework is what Forma builds on, whether or not the product itself uses every piece of it; an AddOn is a project that is both optional and built on a Framework assembly it extends. Optional alone makes nothing an AddOn. Every family with more than one project sits in its own folder under `src/` named for the family, `Forma.Framework/` and `Forma.AddOns/`; the product, the Showcase and the components that are their own projects, which name no family, sit at `src/` root, and the locale packs sit together under `src/Forma.Locales/`, which groups them as a family folder does without naming a family, since they are many and alike.

- `Forma.Blazor` is the product, the components. It references the feature assemblies its components need and no more; anything a component works without belongs behind a package a host opts into. Infrastructure that is not a component but serves several, such as the emoji sprite loader, lives under `Features/{Name}/` in the namespace `Forma.Blazor.Features.{Name}`.
- `Forma.Blazor.{Component}` is a component big enough to be its own project and its own package, such as `Forma.Blazor.Explorer` and `Forma.Blazor.Composer`. Each sits at `src/` root, is laid out as `Forma.Blazor` is and builds on it; the product never references any of them, so the library stands complete without them. One may depend on another when its own markup composes it; a capability that joins two components which otherwise stand apart is an AddOn instead, so that neither depends on the other, as browsing stored files joins the composer to the explorer.
- `Forma.Blazor.Locales.{Language}` is a **locale pack**: an assembly of locale files alone, compiling no code, carrying the files of the assemblies it localizes rather than of itself, such as `Forma.Blazor.Locales.Hebrew`. A pack extends no Framework assembly, so it is no AddOn; it is named for the product family it carries, as a component that is its own project is, and the packs sit together in `src/Forma.Locales/`. Forma's own are one per language and carry every assembly the library localizes, since Forma owns all of them; that naming is this repository's convention rather than something the library enforces, which reads nothing out of a pack's name. An application references a pack and a library never does, since which languages ship is the application's decision. `localization-design.md` is authoritative for what a pack holds and how it is read.
- `Forma.Framework.*` is what other Forma assemblies build on: contracts, defaults, base shells, design vocabulary, horizontal concerns, visual or not. A Framework assembly references only Framework. A Framework assembly is either a default shipped beside its contract and named for the contract plus what it supplies, `Forma.Framework.{Concept}.{Supply}`, such as Unicode emoji or the library's own artwork, or a concept of its own, such as input routing or culture routing; the product need not reference every Framework assembly, and `Forma.Blazor` does not reference `Forma.Framework.Globalization`. The contract such a component speaks lives here too, alongside every other Framework assembly.
- `Forma.AddOns.*` is a host opt-in that depends on a Framework assembly and extends it, an artwork pack over the emoji or icon contracts, destinations over the share contracts, converters over the resource contracts: the host references and registers it itself, and every AddOn ships as its own NuGet package rather than inside the product's. An AddOn whose markup composes a component depends on that component's package too. Nothing inside Forma references an AddOn; the Showcase and the tests demonstrate them.
- A contract stays with the feature that owns it however many projects reference it, as `IToastService` stays in `Feedback`.

Namespaces and identities:

- Every assembly has one root namespace unique to it, its project name: `Forma.Blazor` holds `Forma.Blazor.*`, `Forma.Framework.Icons` holds `Forma.Framework.Icons.*`; the test support project's is `Forma.Tests`; a locale pack compiles no code and so owns no namespace at all. A folder and its namespace stay in step, so `Forma.Blazor/Components/Symbols/` holds `Forma.Blazor.Components.Symbols`; the family folder a project sits under, `Forma.Framework/` or `Forma.AddOns/`, is not part of that folder-namespace correspondence, since it groups projects by family rather than naming one.
- A contracts assembly split from the engine that implements them shares that engine's namespace, so `Forma.Framework.Composer.Extensibility` compiles into `Forma.Framework.Composer`: the split is a packaging boundary rather than a second concept, and a type moving across it must not change a caller's `using`.
- An AddOn owns no namespace of its own and maps to what it extends: `Forma.AddOns.Share.Targets` compiles into `Forma.Framework.Share.Targets`, and `AddOns` appears in code only in the AddOn's static asset base path and in its test project's namespace. A component that is its own project borrows the product's namespace the same way, so `Forma.Blazor.Composer/` holds `Forma.Blazor.Components.Composer` and a host reaches it through the lines it already writes.
- Two identities stay distinct: the one name an assembly, its namespace and its package all carry, `Forma.Blazor`, and one static asset base path per assembly that ships assets: `_content/{assembly name}` for a project a host installs, `_content/forma/{name}` for an interop assembly. `AssemblyName` is never set where it would repeat the project name, and a packable project declares no `PackageId`, taking that same name instead. The packages publish under the `FormaUI` organization, which the package metadata names rather than the identifier carrying it.

Packaging:

- A project is packable when something outside the product can use it on its own: the product itself, every AddOn, every component that is its own project, and a Framework assembly that is a concept of its own, one a host or an AddOn can take without a component behind it, such as a contract with its default for icons, emoji, resources or localization, the CSS builders, sanitization, timing, or culture routing. A Framework assembly an AddOn or such a component extends is packable for that reason alone, as the share contracts are, since the dependent's package declares it as a dependency.
- A locale pack is packable and depends on `Forma.Framework.Localization` alone, whose attribute the targets file in its package stamps a referencing project with. It never depends on the assemblies it localizes, since a host installs a pack beside whichever of them it already has, and nothing it holds is reached by name.
- A feature whose contracts something outside its own package extends splits those contracts into a packable assembly named for its role, as `Forma.Framework.Composer.Extensibility` stands beside the engine `Forma.Framework.Composer`, which stays bundled: without the split both packages would carry a copy of the one assembly, and publishing the whole of it would make the engine's registries and catalogs public API for callers that never touch them. Such a package carries no registrar and constructs nothing.
- A model two surfaces browse alike is a feature assembly of its own, packable, named for the model and rooted in its own namespace, as `Forma.Framework.Catalog` is beside the explorer's engine: it takes what a host or a second surface reuses, the items, how they arrive, what the host is asked, and the bookkeeping every surface repeats, and never a surface's settings or what that surface's parts read, which stay in the engine each surface bundles. Two surfaces then share the package without either depending on the other.
- A project is not packable when it exists only to serve one product's components: a component's feature vocabulary, the contracts the explorer, the composer, the toolbars or the windows speak, and every interop assembly, since scripts reach a browser only through a product. Those ship inside the package of the product whose components speak them through `PrivateAssets="all"`, so a host installing a product needs nothing else to render it.
- A packable project depends on packable projects alone, so its package can declare each dependency; a product is the exception, since it is what bundles the non-packable assemblies, and it declares every packable assembly it or they reach as a dependency. Such a component's package depends on the product's package, and on another's where its markup composes it, bundling only what neither dependency already carries; whatever its bundled assemblies reach through a dependency ships with that dependency, which the pack checks by asking the dependency what it resolves. Every package ships at the one version the root build properties set, so a package depends on the others at the version it was built with. A Framework default is therefore contracts and data and never talks to the browser; whatever does, such as the emoji sprite loader, lives in a product or in an interop assembly a product bundles.

Registration:

- A feature with services to register has a `ServiceCollectionExtensions` at its project root, registering with `TryAdd`, named for what lands in the container: an artwork package adds a pack named for its source, `AddFormaTwemojiPack()`; a feature whose defaults are services is `AddFormaFeedbackServices()`. The exceptions: culture routing registers through `CultureRoutingExtensions`, and a share destination the host switches on one by one, `AddFormaDiscordSharing(configure)`, is added rather than tried.
- `Forma.Blazor` ships exactly one aggregate registrar, `AddFormaUI()`, fanning out to every default a component needs.
- A component whose optional contract is unregistered falls back to a default the library carries, as `FormaIcon` does with the library's own glyph artwork; a contract carrying data nobody can synthesize, such as an emoji catalog, leaves the component in its empty state instead.

## Components

Components live in `src/Forma.Blazor/Components/{Category}/`, where a category names a UI role: `Bars`, `Feedback`, `Inputs`, `Navigation`, `Layout`, `Windows`. A component with supporting types of its own may take a folder inside its category, as `Debugging/InputInspector/` does. A component large enough to grow sub-components and ship as its own package, such as `Explorer` or `Composer`, is instead its own project directly under `src/`, laid out the same way but with no `Components/{Category}/` wrapper, since the project itself is the category. A component is a file set:

```text
Components/{Category}/
├── FormaX.razor          # markup
├── FormaX.razor.cs       # logic
├── FormaX.razor.css      # isolated stylesheet
└── .locales/             # the category's locale files, when a component invents text
    ├── he.json
    └── es.json
```

Every component derives from one of three bases in `src/Forma.Blazor/Components/`: `FormaElementBase` for a component a host places, `FormaInputBase<TValue>` for a form input, `FormaComponentBase` for a part that only lives inside its owner. `component-foundations-design.md` is the design.

The library ships three cultures: `en` as the source language, `he` and `es`. A category's locale pair is `.locales/he.json` and `.locales/es.json` inside its folder, a section per component keyed by the English source text, with a component in a nested folder sectioned under that folder's name. The sibling repository `Forma.Locali` ships `locali`, which checks locale files against the `L[...]` call sites that key them.

Interop lives in interop assemblies, never in `Forma.Blazor`:

- `Forma.Framework.JavaScript.Interop` holds the interop bases, `JSModule`, `JSInterop` and the `IJSEnvironment` they take, and the scripts every other script builds on: the report to a .NET owner, the selectors of the controls a list leaves alone, the frame scheduler, the reading direction, the scroller lookup, the markup sanitizer.
- `Forma.Framework.Browser.Interop` holds every behavior and operation any component may attach, and the wrappers of components that belong to no feature. It sorts by kind: scripts under `wwwroot/scripts/behaviors/`, `operations/` and `components/`, wrappers in the matching `Behaviors/`, `Operations/` and `Components/` folders.
- A feature with scripts of its own ships them in `Forma.Framework.{Feature}.Interop`, scripts directly under `wwwroot/scripts/` and wrappers at the assembly root, as `Forma.Framework.Input.Interop` serves `scripts/InputRouter.js` beside `InputRouter.cs`.
- The assemblies are served side by side as `_content/forma/core`, `_content/forma/browser`, `_content/forma/input` and `_content/forma/composer`. A script reaches another assembly's script by climbing to that root, `../../../core/scripts/FrameScheduler.js` from a behavior, so the import survives a host path base.
- `FormaComposer` is the one component allowed external JavaScript libraries, each to be confined to its own `Forma.AddOns.Composer.{Capability}` package behind a plugin contract, as `composer-design.md` lays out.

Stylesheets: `src/Forma.Blazor/wwwroot/forma-tokens.css` holds every token, value-only; `forma-reset.css` is cross-browser normalization; `forma-base.css` is the opt-in element-level look; `forma-input.css` is the one input box every field draws; `forma-prose.css` is the look of a document the composer authored. Component rules live in the component's isolated stylesheet.

## The Showcase

`src/Forma.Blazor.Showcase` is the review surface: one interactive page per component under `Components/Pages/{Category}/FormaXPage.razor`, its strings a section of the category's locale pair, rendering every state the component can enter in all three cultures; the composer's and the explorer's pages sit under `Features/`. A page opens with:

```razor
@page "/x-component"
@attribute [ShowcasePage("FormaX")]
@rendermode InteractiveServer
@inject IStringLocale<FormaXPage> L
```

and is structured as `<h1>` plus `<p class="description">`, then one `demo-section` with `<h2>` and `demo-box` per scenario. It runs a compiled binary with no hot reload:

```powershell
dotnet run --project src/Forma.Blazor.Showcase
```

A running Showcase holds its `bin` DLLs open, so it is stopped before a rebuild. `src/Forma.Blazor.Showcase/bin/Debug/net11.0/Forma.Blazor.dll` being newer than the sources is the proof a rebuild reached it. `scripts/Start-Showcase.ps1` does both, with `-Force` stopping the one already listening and `-Port` choosing where; it sets the address and the environment itself rather than leaving them to the launch profile, which is not checked in and so exists only where a project has been run from an editor.

## Tests

Every production assembly should have one test project named after it under `tests/`, as does the one thing tested there that is not an assembly, the glyph drawing rules in `tests/Forma.Scripts.Tests`, mirroring the source paths, so component tests live under `tests/Forma.Blazor.Tests/Components/{Category}/`, and a component that is its own project takes a test project named after it, as `FormaXTests.razor` with a `FormaXTests.razor.cs`. The exceptions, each covered from elsewhere: `Forma.Framework.Share`, a contracts assembly whose behavior shows only through what extends it, is exercised through `tests/Forma.AddOns.Share.Targets.Tests`; `Forma.Framework.Composer.Extensibility`, for the same reason, through `tests/Forma.Framework.Composer.Tests`, which tests the engine implementing it; `Forma.Framework.Media` and `Forma.Framework.Toolbar` through the components that use them in `tests/Forma.Blazor.Tests`; and the scripts of `Forma.Framework.Browser.Interop` and `Forma.Framework.Composer.Interop` through the script test project, with their wrappers driven from the tests of the components and features that own them.

- `FormaBunitContext` in `Forma.Tests.Support.Blazor` is the shared test context of every component test project; it registers localization, the Twemoji pack and `AddFormaUI`. That project holds what a component test needs and a Framework test must not pull in, bUnit and the product itself, so `Forma.Tests.Support` stays free of both and every test project can reference it.
- `tests/Forma.Tests.Support` holds every fake, fixture and utility, in a folder mirroring the production area it serves, such as `Resources/`, `Media/` and `Interop/`, with `Shared/` for what no area owns and the namespace following the folder: `Forma.Tests.Resources`, `Forma.Tests.Shared`, `Forma.Tests.Interop`. `FakeJSRuntime` and `FakeJSObjectReference` drive interop wrappers; `FakeLoggerFactory` reads what was logged; `StubStringLocaleFactory` reads what strings were asked for.
- `tests/Forma.Blazor.Tests.Scripts` runs the `wwwroot/scripts/` modules of every assembly that ships scripts under Vitest and happy-dom, reaching each through an alias named for the root it is served under, `#core/`, `#browser/`, `#input/`, `#composer/`, with that assembly's tests in a folder of the same name laid out as its `wwwroot/scripts/` is.

## Build and verification

`build.ps1` at the root runs the gate, so a person and a machine run the same checks; `-Target` narrows it to `DotNet` or `Scripts`, and `-Clean` deletes build output, alone or before a run. A task program is built outside the repository, in the SDK’s own place in the system temp directory, and every edit and rename leaves the folder before it behind there, so `clean.ps1 -All` sweeps those too; it checks that a folder holds nothing but what the SDK put there rather than trusting its name, and it reaches past this repository to anything else run from a single file on the machine. It covers the first four commands below, and builds the task programs written in C#, which no other step reaches. Beside the module under `scripts/build/` sit the narrower loops, one lowercase script each: `compile.ps1`, `test.ps1`, `format.ps1`, `tasks.ps1` and `clean.ps1`. They exist because the gate is about two and a half minutes and the format check alone is half of it, so a run that only needs one project’s tests should not wait for the rest: `test.ps1` takes `-Target`, `-Project`, `-Filter`, `-NoBuild` and `-Times`, where a repeated run continues past a failure so that a test failing sometimes shows as a rate. Each of them is a wrapper: what an action does lives in `scripts/build/Forma.Build.psm1`, so the gate and a single action cannot disagree about what a build is. Every action understands `-WhatIf`, which is what lets `scripts/build/build.tests.ps1` assert what each script does by running it without doing it, because a step that quietly ran nothing would otherwise look exactly like a step that passed. A description is not everything, though: a step whose body is wrong describes itself perfectly, so cleaning is also run for real there against a tree made for the purpose. The gate reads every script in `scripts/` for the shape one here keeps and exercises those it can run without a side effect, through `scripts/Test-TaskScript.ps1`, because the scripts broke once and stayed broken for weeks while nothing ran them; a script added and covered by nothing fails there, so it is covered or it belongs under `.local/` as nobody’s to maintain. The locale lint runs where `locali` is on the path and is reported as skipped where it is not. The showcase never exits, so it stays a check made by hand, started through `scripts/Start-Showcase.ps1`.

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
npm test --prefix tests/Forma.Blazor.Tests.Scripts
dotnet run --project src/Forma.Blazor.Showcase
```

`TreatWarningsAsErrors` is on for `src` and `tests` alike; the `.editorconfig` severity table decides what the compiler reports, and the root file and the per-project files beneath it, such as `src/Forma.Blazor/.editorconfig`, differ deliberately. No project sets `GenerateDocumentationFile`; a throwaway build lists the undocumented public members:

```powershell
dotnet build Forma.slnx -p:GenerateDocumentationFile=true --no-incremental -v:q -nologo |
    Select-String CS1591
```

Work that belongs to a person rather than to the repository lives under `.local/`, ignored in its entirety: the benchmarks, the spikes, and a document written for a piece of work rather than for the repository itself. One ignored root takes the place of a rule per directory, so something new that is nobody else's business needs no rule at all, and nothing kept there can be staged by accident.

The build is entered through `build.ps1` at the root, with its module and its wrapper scripts under `scripts/build/`. A build script is named for its action in lowercase, `compile.ps1` and `test.ps1`, because it is a command someone types rather than a cmdlet they discover; the functions inside the module are PowerShell functions and take `Verb-Noun` with an approved verb, as PowerShell requires of anything a module exports. Everything else in `scripts/` is a task script: PowerShell 7, cross-platform, named `Verb-Noun.ps1`, such as `Get-TwemojiPack.ps1`, `Build-GlyphPack.ps1` and `Update-PluralRules.ps1`. `Stop-MsBuild.ps1` and `Stop-DotNet.ps1` are there for the build that fails to copy a file nothing appears to be using: MSBuild keeps its workers alive between builds and they hold what they last built, and the second goes further and stops whatever is running out of the repository too. Both ask for the supported shutdown before stopping anything, and neither reaches the language server an editor runs for C# unless it is asked to. What decides which a thing is, is whether the build runs it: a script the gate would run belongs to the build, and one that fetches artwork, generates a checked-in asset or drives a browser against a running Showcase does not. Work a script cannot do in PowerShell without writing C# through it, browser automation and glyph geometry so far, is a file-based C# program under `scripts/cs/` that the script runs: `Test-ComponentScale.ps1` runs `cs/CheckComponentScale.cs`. A program there is named for what it does in PascalCase without the hyphen a cmdlet name carries, since it is a file rather than a command, and the script that runs it keeps the approved verb. A program declares the packages it needs in its own header, which is why central package management stops at that folder rather than reaching into it. A program that is an entry point opens with a shebang, and the gate builds each one, since nothing else compiles them and they broke unnoticed once when they were compiled by nothing. A file included into a program rather than run takes a plain noun for a name, as `GlyphParser.cs` does, and logic in a program that is worth testing moves into such a file, which the program includes and `tests/Forma.Scripts.Tests` compiles. The C# extension rewrites `Forma.slnx` with a duplicate `<Folder>` entry and drops `<Configurations>` when a project is added, which fails the build with `MSB4025` until the file is reopened.

## Origins

Every component derives from the legacy Coi admin at `c:\Users\eyal\Development\Projects\2Site\Coi\Coi\`, which sits outside the workspace and is searched from the terminal:

```powershell
Select-String -Path 'c:\Users\eyal\Development\Projects\2Site\Coi\Coi\admin\*.aspx' -Pattern '...'
```

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` and `admin/pops/*.aspx` |
| Behavior and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

Coi's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of fidelity: the real states, vocabulary, edge cases and required behavior survive; the visuals are Forma's.

## Consumers

The current consumers are **Coi** at `c:\Users\eyal\Development\Projects\Coi` and **Épica Deportiva** at `c:\Users\eyal\Development\Projects\EpicaDeportiva`, brought in by project reference or local NuGet. Épica's `docs/forma-components.md` lists the subset it requires and tracks its own status; it is Épica's to maintain.
