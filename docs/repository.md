# The Forma repository

Forma is a reusable Blazor component library: .NET 11, C# latest, a Razor Class Library rendered Interactive Server, JS-free by default. This document describes the repository as a codebase: what the solution is made of, where things live, what it derives from, who consumes it, and how it is built and verified. The working rules in `CLAUDE.md` refer to it by role.

## Solution structure

Every project is named family plus concept, with the family one of `Framework`, `AddOns`, or nothing for the product: `Forma.Framework.Icons`, `Forma.Framework.Core.Interop`, `Forma.AddOns.Emojis.Twemoji`, `Forma.Blazor`, `Forma.Framework.Composer`. The family alone answers whether a project is required or optional: Framework is required, AddOns are optional.

- `Forma.Blazor` is the product, the components. It references the feature assemblies its components need and no more; anything a component works without belongs behind a package a host opts into.
- `Forma.Framework.*` is what other Forma assemblies build on: contracts, defaults, base shells, design vocabulary, horizontal concerns, visual or not. A Framework assembly references only Framework. What the product cannot run without and a host does not choose is a Framework assembly, either a default shipped beside its contract and named for the contract plus what it supplies, `Forma.Framework.{Concept}.{Supply}`, such as Unicode emoji or the library's own artwork, or a concept of its own, such as culture routing.
- `Forma.AddOns.*` is a host opt-in extending something: the host references and registers it itself, and it ships as its own NuGet package rather than inside the product's. Nothing inside Forma references an AddOn; the Showcase and the tests demonstrate them.
- A contract stays with the feature that owns it however many projects reference it, as `IToastService` stays in `Feedback`.

Namespaces and identities:

- Every assembly has one root namespace unique to it, its project name: `Forma.Blazor` holds `Forma.Blazor.*`, `Forma.Framework.Icons` holds `Forma.Framework.Icons.*`. A folder and its namespace stay in step, so `Forma.Blazor/Components/Symbols/` holds `Forma.Blazor.Components.Symbols`.
- An AddOn owns no namespace of its own and maps to what it extends: `Forma.AddOns.Share.Targets` compiles into `Forma.Framework.Share.Targets`, and `AddOns` never appears in code.
- Three identities stay distinct: assembly and namespace `Forma.Blazor`, package ID `2Site.Forma.Blazor`, and one static asset base path per assembly that ships assets, `_content/{assembly name}`. `AssemblyName` is never set where it would repeat the project name, and only a project a host installs by name has a `PackageId`.
- A feature that ships separately does so only when it is optional, carrying its own assets, with the contracts it needs travelling inside it through `PrivateAssets="all"`. The components' package carries the features they depend on, so a host installing the library needs nothing else to render.

Registration:

- A feature with services to register has a `ServiceCollectionExtensions` at its project root, registering with `TryAdd`, named for what lands in the container: an artwork package adds a pack named for its source, `AddFormaTwemojiPack()`; a feature whose defaults are services is `AddFormaFeedbackServices()`.
- `Forma.Blazor` ships exactly one aggregate registrar, `AddFormaUI()`, fanning out to every default a component needs.
- A component whose optional contract is unregistered falls back to a default the library carries, as `FormaIcon` does with the library's own glyph artwork; a contract carrying data nobody can synthesise, such as an emoji catalog, leaves the component in its empty state instead.

## Components

Components live in `src/Forma.Blazor/Components/{Category}/`, where a category names a UI role: `Bars`, `Feedback`, `Inputs`, `Navigation`, `Layout`, `Windows`, and a category of its own for a component large enough to grow sub-components, such as `Explorer` and `Composer`. A component is a file set:

```text
Components/{Category}/
├── FormaX.razor          # markup
├── FormaX.razor.cs       # logic
├── FormaX.razor.css      # isolated stylesheet
├── FormaX.he.json        # translations, when the component invents text
└── FormaX.es.json
```

Every component derives from one of three bases in `src/Forma.Blazor/Components/`: `FormaElementBase` for a component a host places, `FormaInputBase<TValue>` for a form input, `FormaComponentBase` for a part that only lives inside its owner. `docs/component-base-design.md` is the design.

The library ships three cultures: `en` as the source language, `he` and `es-419`. A component's translation pair is `FormaX.he.json` and `FormaX.es.json` beside it, keyed by the English source text. The sibling repository `Forma.Locali` ships `locali`, which checks translation files against the `L[...]` call sites that key them.

Interop lives in interop assemblies, never in `Forma.Blazor`:

- `Forma.Framework.Core.Interop` holds the interop bases, `JSModule`, `JSInterop` and the `IJSEnvironment` they take, and the scripts every other script builds on: the report to a .NET owner, the selectors of the controls a list leaves alone, the frame, the reading direction, the scroller lookup, the markup sanitizer.
- `Forma.Framework.Common.Interop` holds every behaviour and operation any component may attach, and the wrappers of components that belong to no feature. It sorts by kind: scripts under `wwwroot/scripts/behaviours/`, `operations/` and `components/`, wrappers in the matching `Behaviours/`, `Operations/` and `Components/` folders.
- A feature with scripts of its own ships them in `Forma.Framework.{Feature}.Interop`, scripts directly under `wwwroot/scripts/` and wrappers at the assembly root, as `Forma.Framework.Input.Interop` serves `scripts/InputRouter.js` beside `InputRouter.cs`.
- The assemblies are served side by side as `_content/forma/core`, `_content/forma/common`, `_content/forma/input` and `_content/forma/composer`. A script reaches another assembly's script by climbing to that root, `../../../core/scripts/FrameScheduler.js` from a behaviour, so the import survives a host path base.
- `FormaComposer` is the one component allowed external JavaScript libraries, each confined to its own `Forma.AddOns.Composer.{Capability}` package behind a plugin contract.

Stylesheets: `src/Forma.Blazor/wwwroot/forma-tokens.css` holds every token, value-only; `forma-reset.css` is cross-browser normalization; `forma-base.css` is the opt-in element-level look. Component rules live in the component's isolated stylesheet.

## The Showcase

`src/Forma.Blazor.Showcase` is the review surface: one interactive page per component under `Components/Pages/{Category}/FormaXPage.razor`, with its own translation pair, rendering every state the component can enter in all three cultures. A page opens with:

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

Every production assembly should have one test project named after it under `tests/`, mirroring the source paths, so component tests live under `tests/Forma.Blazor.Tests/Components/{Category}/` as `FormaXTests.razor` with a `FormaXTests.razor.cs`. The exceptions, each covered from elsewhere: `Forma.Framework.Share`, a contracts assembly whose behaviour shows only through what extends it, is exercised through `tests/Forma.AddOns.Share.Targets.Tests`; `Forma.Framework.Media` and `Forma.Framework.Toolbar` through the components that use them in `tests/Forma.Blazor.Tests`; and the scripts of `Forma.Framework.Common.Interop` and `Forma.Framework.Composer.Interop` through the script test project, with their wrappers driven from the tests of the components and features that own them.

- `FormaBunitContext` in `Forma.Blazor.Tests` is the shared test context; it registers localization, the bundled artwork and `AddFormaUI`.
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

`TreatWarningsAsErrors` is on for `src` and `tests` alike; the `.editorconfig` severity table decides what the compiler reports, and the root file and `src/Forma.Blazor/.editorconfig` differ deliberately. No project sets `GenerateDocumentationFile`; a throwaway build lists the undocumented public members:

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
| Behaviour and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

Coi's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of fidelity: the real states, vocabulary, edge cases and required behaviour survive; the visuals are Forma's.

## Consumers

The current consumers are **Coi** at `c:\Users\eyal\Development\Projects\Coi` and **Épica Deportiva** at `c:\Users\eyal\Development\Projects\EpicaDeportiva`, brought in by project reference or local NuGet. Épica's `docs/forma-components.md` lists the subset it requires and tracks its own status; it is Épica's to maintain.
