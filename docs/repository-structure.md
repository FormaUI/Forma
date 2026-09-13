# Forma Repository Structure

Forma is a reusable Blazor component library: .NET 11, C# latest, a Razor Class Library rendered Interactive Server, JS-free by default. This document describes the repository as a codebase: what the solution is made of, where things live, what it derives from, who consumes it, and how it is built and verified. The working rules in `CLAUDE.md` refer to it by role. Each area with a technical design should have two documents beside this one: `{area}-design.md`, the design alone, and `{area}-implementation-plan.md`, the steps and what has landed; some areas carry the design alone.

## Solution structure

Every project is named family plus concept, with the family one of `Framework`, `AddOns`, or nothing for the product: `Forma.Framework.Icons`, `Forma.Framework.Core.Interop`, `Forma.AddOns.Emojis.Twemoji`, `Forma.Blazor`, `Forma.Framework.Composer`. The family says what a project is: Framework is what Forma builds on, whether or not the product itself uses every piece of it; an AddOn is a project that is both optional and built on a Framework assembly it extends. Optional alone makes nothing an AddOn.

- `Forma.Blazor` is the product, the components. It references the feature assemblies its components need and no more; anything a component works without belongs behind a package a host opts into. Infrastructure that is not a component but serves several, such as the emoji sprite loader, lives under `Features/{Name}/` in the namespace `Forma.Blazor.Features.{Name}`.
- `Forma.Framework.*` is what other Forma assemblies build on: contracts, defaults, base shells, design vocabulary, horizontal concerns, visual or not. A Framework assembly references only Framework. A Framework assembly is either a default shipped beside its contract and named for the contract plus what it supplies, `Forma.Framework.{Concept}.{Supply}`, such as Unicode emoji or the library's own artwork, or a concept of its own, such as input routing or culture routing; the product need not reference every Framework assembly, and `Forma.Blazor` does not reference `Forma.Framework.Globalization`.
- `Forma.AddOns.*` is a host opt-in that depends on a Framework assembly and extends it, an artwork pack over the emoji or icon contracts, destinations over the share contracts, converters over the resource contracts: the host references and registers it itself, and every AddOn ships as its own NuGet package rather than inside the product's. Nothing inside Forma references an AddOn; the Showcase and the tests demonstrate them.
- A contract stays with the feature that owns it however many projects reference it, as `IToastService` stays in `Feedback`.

Namespaces and identities:

- Every assembly has one root namespace unique to it, its project name: `Forma.Blazor` holds `Forma.Blazor.*`, `Forma.Framework.Icons` holds `Forma.Framework.Icons.*`; the test support project's is `Forma.Tests`. A folder and its namespace stay in step, so `Forma.Blazor/Components/Symbols/` holds `Forma.Blazor.Components.Symbols`.
- An AddOn owns no namespace of its own and maps to what it extends: `Forma.AddOns.Share.Targets` compiles into `Forma.Framework.Share.Targets`, and `AddOns` appears in code only in the AddOn's static asset base path and in its test project's namespace.
- Three identities stay distinct: assembly and namespace `Forma.Blazor`, package ID `2Site.Forma.Blazor`, and one static asset base path per assembly that ships assets: `_content/{assembly name}` for a project a host installs, `_content/forma/{name}` for an interop assembly. `AssemblyName` is never set where it would repeat the project name, and every packable project has a `PackageId` of `2Site.` followed by its project name.

Packaging:

- A project is packable when something outside the product can use it on its own: the product itself, every AddOn, and a Framework assembly that is a concept of its own, one a host or an AddOn can take without a component behind it, such as a contract with its default for icons, emoji, resources or localization, the CSS builders, sanitization, timing, or culture routing. A Framework assembly an AddOn extends is packable for that reason alone, as the share contracts are, since the AddOn's package declares it as a dependency.
- A project is not packable when it exists only to serve the components: a component's feature vocabulary, the contracts the explorer, the composer, the toolbars or the windows speak, and every interop assembly, since scripts reach a browser only through the product. Those ship inside the product's package through `PrivateAssets="all"`, so a host installing the library needs nothing else to render.
- A packable project depends on packable projects alone, so its package can declare each dependency; the product is the one exception, since it is what bundles the non-packable assemblies. A Framework default is therefore contracts and data and never talks to the browser; whatever does, such as the emoji sprite loader, lives in the product or in an interop assembly the product bundles.

Registration:

- A feature with services to register has a `ServiceCollectionExtensions` at its project root, registering with `TryAdd`, named for what lands in the container: an artwork package adds a pack named for its source, `AddFormaTwemojiPack()`; a feature whose defaults are services is `AddFormaFeedbackServices()`. The exceptions: culture routing registers through `CultureRoutingExtensions`, and a share target the host configures with a token or an address, `AddFormaDropboxSharing(accessToken)`, is added rather than tried.
- `Forma.Blazor` ships exactly one aggregate registrar, `AddFormaUI()`, fanning out to every default a component needs.
- A component whose optional contract is unregistered falls back to a default the library carries, as `FormaIcon` does with the library's own glyph artwork; a contract carrying data nobody can synthesize, such as an emoji catalog, leaves the component in its empty state instead.

## Components

Components live in `src/Forma.Blazor/Components/{Category}/`, where a category names a UI role: `Bars`, `Feedback`, `Inputs`, `Navigation`, `Layout`, `Windows`, and a category of its own for a component large enough to grow sub-components, such as `Explorer` and `Composer`. A component with supporting types of its own may take a folder inside its category, as `Debugging/InputInspector/` does. A component is a file set:

```text
Components/{Category}/
├── FormaX.razor          # markup
├── FormaX.razor.cs       # logic
├── FormaX.razor.css      # isolated stylesheet
├── FormaX.he.json        # translations, when the component invents text
└── FormaX.es.json
```

Every component derives from one of three bases in `src/Forma.Blazor/Components/`: `FormaElementBase` for a component a host places, `FormaInputBase<TValue>` for a form input, `FormaComponentBase` for a part that only lives inside its owner. `component-foundations-design.md` is the design.

The library ships three cultures: `en` as the source language, `he` and `es`. A component's translation pair is `FormaX.he.json` and `FormaX.es.json` beside it, keyed by the English source text. The sibling repository `Forma.Locali` ships `locali`, which checks translation files against the `L[...]` call sites that key them.

Interop lives in interop assemblies, never in `Forma.Blazor`:

- `Forma.Framework.Core.Interop` holds the interop bases, `JSModule`, `JSInterop` and the `IJSEnvironment` they take, and the scripts every other script builds on: the report to a .NET owner, the selectors of the controls a list leaves alone, the frame scheduler, the reading direction, the scroller lookup, the markup sanitizer.
- `Forma.Framework.Common.Interop` holds every behavior and operation any component may attach, and the wrappers of components that belong to no feature. It sorts by kind: scripts under `wwwroot/scripts/behaviors/`, `operations/` and `components/`, wrappers in the matching `Behaviors/`, `Operations/` and `Components/` folders.
- A feature with scripts of its own ships them in `Forma.Framework.{Feature}.Interop`, scripts directly under `wwwroot/scripts/` and wrappers at the assembly root, as `Forma.Framework.Input.Interop` serves `scripts/InputRouter.js` beside `InputRouter.cs`.
- The assemblies are served side by side as `_content/forma/core`, `_content/forma/common`, `_content/forma/input` and `_content/forma/composer`. A script reaches another assembly's script by climbing to that root, `../../../core/scripts/FrameScheduler.js` from a behavior, so the import survives a host path base.
- `FormaComposer` is the one component allowed external JavaScript libraries, each to be confined to its own `Forma.AddOns.Composer.{Capability}` package behind a plugin contract, as `composer-design.md` lays out.

Stylesheets: `src/Forma.Blazor/wwwroot/forma-tokens.css` holds every token, value-only; `forma-reset.css` is cross-browser normalization; `forma-base.css` is the opt-in element-level look; `forma-input.css` is the one input box every field draws; `forma-prose.css` is the look of a document the composer authored. Component rules live in the component's isolated stylesheet.

## The Showcase

`src/Forma.Blazor.Showcase` is the review surface: one interactive page per component under `Components/Pages/{Category}/FormaXPage.razor`, with its own translation pair, rendering every state the component can enter in all three cultures; the composer's and the explorer's pages sit under `Features/`. A page opens with:

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

A running Showcase holds its `bin` DLLs open, so it is stopped before a rebuild. `src/Forma.Blazor.Showcase/bin/Debug/net11.0/Forma.Blazor.dll` being newer than the sources is the proof a rebuild reached it.

## Tests

Every production assembly should have one test project named after it under `tests/`, mirroring the source paths, so component tests live under `tests/Forma.Blazor.Tests/Components/{Category}/` as `FormaXTests.razor` with a `FormaXTests.razor.cs`. The exceptions, each covered from elsewhere: `Forma.Framework.Share`, a contracts assembly whose behavior shows only through what extends it, is exercised through `tests/Forma.AddOns.Share.Targets.Tests`; `Forma.Framework.Media` and `Forma.Framework.Toolbar` through the components that use them in `tests/Forma.Blazor.Tests`; and the scripts of `Forma.Framework.Common.Interop` and `Forma.Framework.Composer.Interop` through the script test project, with their wrappers driven from the tests of the components and features that own them.

- `FormaBunitContext` in `Forma.Blazor.Tests` is the shared test context; it registers localization, the Twemoji pack and `AddFormaUI`.
- `tests/Forma.Tests.Support` holds every fake, fixture and utility, in a folder mirroring the production area it serves, such as `Resources/`, `Media/` and `Interop/`, with `Shared/` for what no area owns and the namespace following the folder: `Forma.Tests.Resources`, `Forma.Tests.Shared`, `Forma.Tests.Interop`. `FakeJSRuntime` and `FakeJSObjectReference` drive interop wrappers; `FakeLoggerFactory` reads what was logged; `StubStringLocaleFactory` reads what strings were asked for.
- `tests/Forma.Blazor.Tests.Scripts` runs the `wwwroot/scripts/` modules of every assembly that ships scripts under Vitest and happy-dom, reaching each through an alias named for it, `#core/`, `#common/`, `#input/`, `#composer/`, with that assembly's tests in a folder of the same name laid out as its `wwwroot/scripts/` is.

## Build and verification

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

Task scripts live in `scripts/` and are PowerShell 7, cross-platform, such as `Get-TwemojiPack.ps1`, `Build-GlyphPack.ps1` and `Update-PluralRules.ps1`. The C# extension rewrites `Forma.slnx` with a duplicate `<Folder>` entry and drops `<Configurations>` when a project is added, which fails the build with `MSB4025` until the file is reopened.

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
