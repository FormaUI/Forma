# Forma Repository Structure

This document maps the Forma repository for someone working in it. The working rules in `CLAUDE.md` point to it.
It sets out:

- what the solution is made of, and where each kind of file lives;
- how the repository is built and verified, and how a release is made;
- where the components' designs come from, and who consumes the library.

Forma is a reusable Blazor component library: .NET 11, C# latest, Razor Class Libraries whose components render
under static server rendering, Interactive Server, WebAssembly and Auto, and in Blazor Hybrid's web view. It is
JS-free by default.

Other documents cover what this one doesn't:

- [The features status](features-status.md) says where the project stands and what comes next.
- [The changelog](../CHANGELOG.md) says what each release changed, and [the feature history](../FEATURE-HISTORY.md)
  what each month added before the first release.
- An area with a technical design takes two documents while it is being built: `{area}-design.md`, the design alone,
  and `{area}-implementation-plan.md`, the steps and what has landed. The plan is retired once every step has landed
  and nothing is open, so a finished area carries its design alone.

In this document:

1. [Solution structure](#solution-structure)
2. [Components](#components)
3. [The Showcase](#the-showcase)
4. [Tests](#tests)
5. [Build and verification](#build-and-verification)
6. [Branches and releases](#branches-and-releases)
7. [Personal work](#personal-work)
8. [Origins](#origins)
9. [Consumers](#consumers)

## Solution structure

Every project is named for its family and its concept: `Forma.Framework.Icons`, `Forma.Framework.JavaScript.Interop`,
`Forma.AddOns.Emojis.Twemoji`, `Forma.Blazor`, `Forma.Framework.Composer`. The family is `Framework`, `AddOns`, or
nothing for the product, and it says what a project is:

- **Framework** is what Forma builds on, whether or not the product itself uses every piece of it.
- **An AddOn** is a project that is both optional and built on a Framework assembly it extends. Optional alone makes
  nothing an AddOn.

Every project sits under `src/` in the folder of its group:

| Folder | Holds |
|--------|-------|
| `src/Framework/` | The Framework family |
| `src/AddOns/` | The AddOns family |
| `src/Blazor/` | The product, its render-mode halves, the components that are their own projects, and the Showcase, which name no family |
| `src/Locales/` | The locale packs, grouped as a family is, since they are many and alike |

### Kinds of project

**`Forma.Blazor` is the product, the components.** It references the feature assemblies its components need and no
more; anything a component works without belongs behind a package a host opts into. Infrastructure that is not a
component but serves several, such as the emoji sprite loader, lives under `Features/{Name}/` in the namespace
`Forma.Blazor.Features.{Name}`.

**`Forma.Blazor.Server`, `Forma.Blazor.WebAssembly` and `Forma.Blazor.WebView` are the product's render-mode
halves.** Each holds what only a server, only a WebAssembly client or only a Blazor Hybrid application needs beside
the components, such as `FormaPageHead` and the registration of the circuit's integrations on the server's side.
Each sits in `src/Blazor/` and builds on the product. The product references none of them, so the components load
in every runtime.

**`Forma.Blazor.{Component}` is a component big enough to be its own project and its own package**, such as
`Forma.Blazor.Explorer` and `Forma.Blazor.Composer`. Each sits in `src/Blazor/`, is laid out as `Forma.Blazor` is and
builds on it. The product never references any of them, so the library stands complete without them.

- One may depend on another when its own markup composes it.
- A capability that joins two components which otherwise stand apart is an AddOn instead, so that neither depends on
  the other, as browsing stored files joins the composer to the explorer.

**`Forma.Blazor.Locales.{Language}` is a locale pack**, such as `Forma.Blazor.Locales.Hebrew`: an assembly of locale
files alone. It compiles no code, and it carries the files of the assemblies it localizes rather than of itself.

- A pack extends no Framework assembly, so it isn't an AddOn. It is named for the product family it carries, as a
  component that is its own project is, and the packs sit together in `src/Locales/`.
- Forma's own packs are one per language and carry every assembly the library localizes, since Forma owns all of
  them. That naming is this repository's convention; the library reads nothing out of a pack's name.
- An application references a pack and a library never does, since which languages ship is the application's
  decision.

[The localization design](localization-design.md) is authoritative for what a pack holds and how it is read.

**`Forma.Framework.*` is what other Forma assemblies build on:** contracts, defaults, base shells, design vocabulary
and horizontal concerns, visual or not. A Framework assembly references only Framework, and is one of two kinds:

- a default shipped beside its contract, named for the contract plus what it supplies,
  `Forma.Framework.{Concept}.{Supply}`, such as Unicode emoji or the library's own artwork;
- a concept of its own, such as input routing or culture routing.

The product need not reference every Framework assembly: `Forma.Blazor` does not reference
`Forma.Framework.Localization.Json`. The contracts the components speak live in Framework assemblies too.

**`Forma.AddOns.*` is a host opt-in that depends on a Framework assembly and extends it:** an artwork pack over the
emoji or icon contracts, destinations over the share contracts, converters over the resource contracts.

- The host references and registers an AddOn itself, and every AddOn ships as its own NuGet package rather than
  inside the product's.
- An AddOn whose markup composes a component depends on that component's package too.
- Nothing inside Forma references an AddOn; the Showcase and the tests demonstrate them.

**A contract stays with the feature that owns it**, however many projects reference it, as `IToastService` stays in
`Feedback`.

### Namespaces and identities

**Every assembly has one root namespace unique to it, its project name.** `Forma.Blazor` holds `Forma.Blazor.*`, and
`Forma.Framework.Icons` holds `Forma.Framework.Icons.*`. The test support projects' are `Forma.Tests`, for the
bUnit support `Forma.Tests.Blazor`, and for the hosts' lifecycle `Forma.Tests.Hosts`. A locale pack compiles no
code, so it owns no namespace.

**A folder and its namespace stay in step**, so `Forma.Blazor/Components/Symbols/` holds
`Forma.Blazor.Components.Symbols`. The group folder a project sits in, such as `Framework/` or `Blazor/`, stays out of
that correspondence, since it groups projects rather than naming one.

Three kinds of assembly borrow the namespace of what they extend or split from:

- **A contracts assembly split from the engine that implements it** shares the engine's namespace, so
  `Forma.Framework.Composer.Extensibility` compiles into `Forma.Framework.Composer`. The split is a packaging boundary
  rather than a second concept, and a type moving across it must not change a caller's `using`.
- **An AddOn** maps to what it extends: `Forma.AddOns.Share.Targets` compiles into `Forma.Framework.Share.Targets`.
  `AddOns` appears in code only in the AddOn's static asset base path and in its test project's namespace.
- **A component that is its own project** borrows the product's namespace, so `Forma.Blazor.Composer/` holds
  `Forma.Blazor.Components.Composer` and a host reaches it through the lines it already writes. The product's
  render-mode halves borrow `Forma.Blazor` itself, so `FormaPageHead` stays in `Forma.Blazor.Components.Page` and
  every registrar is reached through `using Forma.Blazor;`.

**Three identities stay in step:**

| Identity | Form | Example |
|----------|------|---------|
| Assembly and namespace | The one name both carry | `Forma.Blazor` |
| Package | That name under the reserved prefix, without the product's own | `formaui-net.Blazor` |
| Static asset base path | `_content/{assembly name}` for a product, a component that is its own project or an AddOn; `_content/forma/{name}` for an interop assembly | `_content/Forma.Blazor` |

The root targets file derives the package name for every packable project, so none declares a `PackageId`.
`AssemblyName` is never set where it would repeat the project name. The packages publish under the `FormaUI`
organization, whose prefix the identifier carries and whose name the package metadata states.

### Packaging

**Every project under `src/` but the Showcase is a package:** the product, every component that is its own project,
every AddOn, every locale pack and every Framework assembly, a new one included.

- A Framework package provides a reusable capability, an engine such as the composer's or the explorer's among them.
  A product package composes those capabilities and supplies the product's integration and defaults.
- Which types a host may name is decided by their visibility, never by how an assembly ships, so what no caller names
  stays internal.

**A locale pack depends on `Forma.Framework.Localization` alone.** The targets file in its package stamps a
referencing project with that assembly's attribute. A pack never depends on the assemblies it localizes, since a host
installs a pack beside whichever of them it already has, and nothing it holds is reached by name.

**A feature whose contracts something outside it extends keeps those contracts in an assembly named for their
role**, as `Forma.Framework.Composer.Extensibility` stands beside the engine `Forma.Framework.Composer`. An extension
then depends on the contracts alone, never on the engine. Such a package carries no registrar and constructs nothing.

**A model two surfaces browse alike is a feature assembly of its own**, named for the model and rooted in its own
namespace, as `Forma.Framework.Catalog` is beside the explorer's engine. It takes what a host or a second surface
reuses: the items, how they arrive, what the host is asked, and the bookkeeping every surface repeats. A surface's
settings, and what that surface's parts read, stay in each surface's engine. Two surfaces then share the package
without either depending on the other.

**A package depends on packages alone** and declares each one its own code requires, never one it reaches only
through another. A component that is its own project depends on the product's package, and on another component's
where its markup composes it. Every package ships at the one version the root build properties set, so a package
depends on the others at the version it was built with.

**A Framework assembly that talks to the browser does so through an interop assembly.** An interop assembly is a
package like any other that ships its scripts under its own static asset base path, so a script keeps its address
whichever package brings it to a host.

### Registration

**A feature with services to register has a `ServiceCollectionExtensions` at its project root**, registering with
`TryAdd`. Its method is named for what lands in the container: an artwork package adds a pack named for its source,
`AddFormaTwemojiPack()`, and a feature whose defaults are services is `AddFormaFeedbackServices()`. The exceptions:

- Culture routing registers through `CultureRoutingExtensions`.
- The product's server half registers on the host's builder through `HostApplicationBuilderExtensions`, its
  WebAssembly half through `WebAssemblyServiceCollectionExtensions` and its WebView half through
  `WebViewServiceCollectionExtensions`. All three borrow the product's namespace, whose `ServiceCollectionExtensions`
  a second class of that name would collide with.
- A share destination the host switches on one by one, `AddFormaDiscordSharing(configure)`, is added rather than
  tried.

**`Forma.Blazor` ships one aggregate registrar, `AddFormaUI()`**, fanning out to every default a component needs in
any render mode. Each render-mode half ships one more that calls it and adds its render mode's halves, so a host
makes one call on each side: `AddFormaUIServer()` on the host's builder, `AddFormaUIWebAssembly()` and
`AddFormaUIWebView()`. A Framework half that serves a render mode names its registrar for it: `AddFormaCultureServer()`,
`AddFormaCultureWebAssembly()`, `AddFormaCultureWebView()` and `AddFormaBrowserServer()`.

**A component whose optional contract is unregistered falls back to a default the library carries**, as `FormaIcon`
does with the library's own glyph artwork. A contract carrying data nobody can synthesize, such as an emoji catalog,
leaves the component in its empty state instead.

## Components

Components live in `src/Blazor/Forma.Blazor/Components/{Category}/`, where a category names a UI role: `Bars`,
`Feedback`, `Inputs`, `Navigation`, `Layout`, `Windows`. A component with supporting types of its own may take a
folder inside its category, as `Debugging/InputInspector/` does.

A component large enough to grow sub-components and ship as its own package, such as `Explorer` or `Composer`, is
instead its own project in `src/Blazor/`. It is laid out the same way but with no `Components/{Category}/`
wrapper, since the project itself is the category.

A component is a file set:

```text
Components/{Category}/
├── FormaX.razor          # markup
├── FormaX.razor.cs       # logic
└── FormaX.razor.css      # isolated stylesheet
```

Every component derives from one of three bases in `src/Blazor/Forma.Blazor/Components/`, which
[the component foundations design](component-foundations-design.md) sets out:

| Base | For |
|------|-----|
| `FormaElementBase` | A component that draws a root its host addresses |
| `FormaInputBase<TValue>` | A form input |
| `FormaComponentBase` | Every other component: a part inside its owner, or one without a root of its own |

### Strings

The library ships three cultures: `en` as the source language, `he` and `es`. A component's strings are a section of
its category's file in each language's pack, keyed by the English source text:

```text
src/Locales/Forma.Blazor.Locales.{Language}/Forma.Blazor/Components/{Category}/{culture}.json
```

A component that is its own project has a folder of its own in each pack, named for its assembly.
[Locali](https://github.com/FormaUI/Locali), a repository of its own, ships `locali`, which checks locale files
against the `L[...]` call sites that key them.

### Scripts

Scripts ship with the project that owns them:

- **A product built on the component library** ships the scripts only it uses itself, under its `wwwroot/scripts/`,
  with their wrappers in its `Interop/` folder, served as `_content/{Product}/scripts/`. `Forma.Blazor.DataView` ships
  its selection, column-fit, height and card-grid behaviors, `Forma.Blazor.Explorer` its layout tier, and
  `Forma.Blazor.Composer` the rich text editor and the behaviors around its canvas.
- **`Forma.Framework.JavaScript.Interop`** holds the interop bases, `JSModule`, `JSInterop` and the `IJSEnvironment`
  they take, and `JSTransfer`, a value the browser sends .NET through the transfer. It also holds the scripts every
  other script builds on: the report to a .NET owner, the transfer that carries a large value to .NET, the selectors
  of the controls a list leaves alone, the frame scheduler, the reading direction, the scroller lookup and the markup
  sanitizer.
- **`Forma.Framework.Browser.Interop`** holds the browser capabilities the component library's components use or
  several products share. The DOM watcher and engine sit at its root, and each capability area in a folder of its own,
  such as `Scrolling`. Until their areas replace them, behaviors, operations and component scripts sit under
  `wwwroot/scripts/behaviors/`, `operations/` and `components/`, with wrappers in the matching `Behaviors/`,
  `Operations/` and `Components/` folders. `Forma.Framework.Browser.Blazor.Server` connects it to an Interactive
  Server circuit.
- **A Framework feature with scripts of its own** ships them in `Forma.Framework.{Feature}.Interop`, scripts directly
  under `wwwroot/scripts/` and wrappers at the assembly root, as `Forma.Framework.Input.Interop` serves
  `scripts/InputRouter.js` beside `InputRouter.cs`.

The interop assemblies are served side by side as `_content/forma/core`, `_content/forma/browser`,
`_content/forma/input` and `_content/forma/culture`. A script reaches another
assembly's script by climbing to the content root, so the import survives a host path base:

| From | Import |
|------|--------|
| A behavior | `../../../core/scripts/FrameScheduler.js` |
| A product's script | `../../forma/core/scripts/FrameScheduler.js` |

**DOMQL**, the query language the DOM watcher and engine are designed to take, lives in its own repository,
[DOMQL](https://github.com/FormaUI/DOMQL). It is a JavaScript library published as the package
`formaui-net.DOMQL`, whose one bundled file a host serves as `_content/domql/domql.js`.
[Replacing Forma's JavaScript with DOMQL](forma-javascripts-domql-replacement.md) sets out what each script becomes.

**`FormaComposer` is the one component allowed external JavaScript libraries**, each to be confined to its own
`Forma.AddOns.Composer.{Capability}` package behind a plugin contract, as [the composer design](composer-design.md)
lays out.

### Stylesheets

Shared stylesheets live in `src/Blazor/Forma.Blazor/wwwroot/`:

| File | Purpose |
|------|---------|
| `forma-tokens.css` | Every design token; values only |
| `forma-reset.css` | Cross-browser normalization |
| `forma-base.css` | The opt-in look of HTML elements |
| `forma-input.css` | The one input box every field draws |
| `forma-prose.css` | The look of a document the composer authored |

Component-specific rules belong in each component's isolated stylesheet.

## The Showcase

`src/Blazor/Forma.Blazor.Showcase` is the review surface. It has one interactive page per component, under
`Components/Pages/{Category}/FormaXPage.razor`, rendering every state the component can enter in all three
cultures; the data view's, the composer's and the explorer's pages sit under `Features/`. A page's strings are a
section of its category's locale pair.

A page opens with:

```razor
@page "/x-component"
@attribute [ShowcasePage("FormaX")]
@rendermode InteractiveServer
@inherits ShowcaseComponentBase
```

`ShowcaseComponentBase` gives the page its strings, `L`, read in the culture state's cultures, and renders it again
on each change of the state. A page that shows nothing interactive leaves the render mode out.

A page is structured as `<h1>` plus `<p class="description">`, then one `demo-section` with `<h2>` and `demo-box` per
scenario.

The Showcase runs a compiled binary with no hot reload:

```powershell
dotnet run --project src/Blazor/Forma.Blazor.Showcase
```

- **Stop it before a rebuild.** A running Showcase holds its `bin` DLLs open.
- **Check the rebuild reached it.** `src/Blazor/Forma.Blazor.Showcase/bin/Debug/net11.0/Forma.Blazor.dll` being newer
  than the sources is the proof.
- **`scripts/Start-Showcase.ps1` does both**, with `-Force` stopping the one already listening and `-Port` choosing
  where. It sets the address and the environment itself rather than leaving them to the launch profile, which is not
  checked in and so exists only where a project has been run from an editor.

## Tests

### Where tests live

The tests sit under `tests/` in a folder for their kind, as [the verification design](verification-design.md#layout)
sets out:

| Folder | Holds |
|--------|-------|
| `tests/unit/` | Unit tests |
| `tests/bunit/` | bUnit tests |
| `tests/integration/` | Integration tests |
| `tests/smoke/` | Smoke checks |
| `tests/e2e/` | End-to-end checks |
| `tests/scripts/` | The JavaScript tests |
| `tests/support/` | The support projects |

A unit or bUnit suite sits in the group folder of the assembly it tests, `AddOns/`, `Blazor/` or `Framework/`. Three
suites sit elsewhere:

- The examples' tests sit in `tests/unit/Examples/`, since they hold the README's blocks to the examples' files.
- The analyzers' tests sit in `eng/`, beside the analyzers.
- The glyph drawing rules' tests sit beside the rules, in `scripts/cs/GlyphParserTests.cs`, a single-file test
  program the gate's task step runs.

### Test projects and their names

**Every production assembly has one test project for each kind of test it has**, named after it and the kind, as
[the verification design](verification-design.md#names) sets out:

| Kind | Project |
|------|---------|
| Unit | `X.Tests` |
| bUnit | `X.BUnit.Tests` |
| Integration | `X.Integration.Tests` |

Each mirrors the source paths, so component tests live under
`tests/bunit/Blazor/Forma.Blazor.BUnit.Tests/Components/{Category}/`, as `FormaXTests.razor` with a
`FormaXTests.razor.cs`. A component that is its own project takes suites named after it.

**Some assemblies are covered from elsewhere:**

| Assembly | Covered by | Because |
|----------|------------|-------|
| `Forma.Framework.Share` | `tests/unit/AddOns/Forma.AddOns.Share.Targets.Tests` | A contracts assembly's behavior shows only through what extends it |
| `Forma.Framework.Composer.Extensibility` | `tests/unit/Framework/Forma.Framework.Composer.Tests`, the engine implementing it | The same reason |
| `Forma.Framework.Media`, `Forma.Framework.Toolbar` | The components that use them, in `tests/bunit/Blazor/Forma.Blazor.BUnit.Tests` | |
| The locale packs | The component tests, whose bUnit context reads the packs as an application does | A pack compiles no code |
| The scripts of `Forma.Framework.Browser.Interop` and the products | The script test project | |

The wrappers of `Forma.Framework.Browser.Interop`'s kind folders are driven from the tests of the components and
features that own them.

### Test support

- **`FormaBunitContext`**, in `Forma.Tests.Support.Blazor`, is the shared test context of every bUnit suite. It
  registers localization, the Twemoji pack and `AddFormaUI`. That project holds what a component test needs and a
  Framework test must not pull in, bUnit and the product itself, so `Forma.Tests.Support` stays free of both and every
  test project can reference it.
- **A fake, fixture or utility one suite uses** lives in that suite's `Support/` folder, under its `Support`
  namespace, as `Forma.Blazor.BUnit.Tests.Support`. It moves to `tests/support/Forma.Tests.Support` once a second
  suite uses it.
- **In `Forma.Tests.Support`**, the types sit in a folder mirroring the production area they serve, such as
  `Interop/`, `Culture/` and `Maui/`, with `Shared/` for what no area owns and the namespace following the folder:
  `Forma.Tests.Interop`, `Forma.Tests.Culture`, `Forma.Tests.Shared`. `FakeJSRuntime` and `FakeJSObjectReference`
  drive interop wrappers, `FakeLoggerFactory` reads what was logged, and `StubStringLocaleFactory` reads what strings
  were asked for. `Localization/Rooted/` stays there with one consumer, since its types' strings sit in that assembly
  under a root its own locale config declares, which is what the JSON localization tests read.
- **`tests/support/Forma.Tests.Support.Hosts`** holds the operations a host's lifecycle needs, which the smoke and E2E
  suites share, each keeping its own fixtures and assertions:
  - `WebHostProcess` starts a published web host on a free port, waits until it answers and stops it, cleaning up
    after a start that failed.
  - `WebViewApplication` starts a Blazor Hybrid application from its publish with WebView2's DevTools port open and
    connects to its web views.
  - `AndroidDevice` installs, starts and reads an application on the Android device or emulator adb reaches.

### Script tests

`tests/scripts` runs the `wwwroot/scripts/` modules of every assembly that ships scripts, under Vitest and
happy-dom. It reaches each assembly through an alias named for the assembly's concept, and holds that assembly's
tests in a folder of the same name, laid out as its `wwwroot/scripts/` is:

| Alias | Reaches |
|-------|---------|
| `#core/`, `#browser/`, `#input/`, `#culture/` | The interop assemblies |
| `#composer/`, `#dataview/`, `#explorer/`, `#showcase/` | The products and the Showcase |

### The browser check

The browser check drives the components in a real browser, headless, in each runtime a Web App renders them in.

**Its projects:**

| Project | What it is |
|---------|------------|
| `tests/e2e/Forma.Blazor.Browser.Tests` | The check itself: an xUnit program the gate's browser step runs after it publishes both clients in Release, never `dotnet test` |
| `tests/e2e/Clients/Forma.Blazor.Browser.Host` | A Web App with a Server, a WebAssembly and an Auto page |
| `tests/e2e/Clients/Forma.Blazor.Browser.Host.Client` | Its client, the minimal one a host builds: the components and their WebAssembly half alone |
| `tests/e2e/Clients/Forma.Blazor.Browser.Full` | A standalone client referencing every package a client can load |
| `tests/e2e/Components/Forma.Blazor.Browser.Components.Checks` | The component checks' pages, a Razor class library |
| `tests/e2e/Components/Forma.Blazor.Browser.Components` and its client | The Web App hosting the check pages |
| `tests/e2e/Components/Forma.Blazor.Browser.WebView` | The WebView check host, a WPF application |
| `tests/e2e/Components/Forma.Blazor.Browser.Android` | The Android check host, a MAUI application for Android |

#### Download budgets

The check holds each client's compressed download, by dependency group, to the budgets in its `budgets.json`, which
a reviewed change raises when a package grows on purpose. It also records which assemblies the publish trims.

The full client is published to measure what each optional package adds, and served as a standalone app, its files
as a static host serves them. The components host is apart from the minimal client, whose download stays what a host
using no component pays.

#### Culture checks

The culture checks run against the host started in several modes:

- islands and an interactive router switching through culture addresses;
- islands switching the culture state alone, rendering every request in a named culture and, apart, in the
  invariant culture, so no check rests on the culture of the machine it runs on;
- islands persisting a switch in the culture cookie, which request localization reads back;
- islands beside a client culture each WebAssembly runtime keeps as its own, with culture capture off, the host
  handing the runtime its arrangement through an environment variable at `Blazor.start`;
- a recording router in each runtime, for the navigation checks.

They also run against the full client, served as a standalone app.

#### Component checks

**Check pages.** The checks' Razor class library holds a check page per component, with the services the checked
components need. The components host and its client render the pages, and the WebView check host renders them too.
A page's address names the runtime:

- `server`, `webassembly`, `auto`, `webview` or `android`, which the host's `App` renders the interactive router in;
- `static`, which it renders statically.

**Interactive checks.** A test class per category, in the check's `Components/` folder, drives each component's
interactions, every call it makes to a script and every call a script makes back among them. It runs them in each
runtime, and each case asserts the runtime it ran in:

- in the Server;
- in WebAssembly;
- in Auto twice: on a first visit, which runs on the Server, and once WebAssembly has downloaded, which runs there;
- in the web view, WebView2's and Android's.

**The static check** renders every check page statically. It holds each page to show its state, raise no error, show
nothing waiting on a script and offer no enabled control whose action needs interactivity.

**The inventory** holds every component the catalog lists to a check page, and every check page to a check that runs
in every interactive runtime.

**The `Runtime` trait.** Each runtime's row of a component check carries its runtime as the `Runtime` trait. The
browser step filters on it to run the Android rows after the rest, in a run of their own, on a device whose screen
holds a check's 1280 by 720 page.

#### The WebView check host

`tests/e2e/Components/Forma.Blazor.Browser.WebView` is a WPF application whose native windows each render the
components to a web view.

- It registers through the product's WebView half, before or after the platform's registrar, as an environment
  variable says, and embeds culture routes and strings of its own.
- It renders every component's check page. Each component check opens a native window of its own at its page and
  closes it as it ends, one at a time.
- The browser step publishes it on Windows, the one platform WebView2 runs on. Elsewhere its checks report themselves
  skipped.
- Each of its checks starts an application of its own and drives its web views through WebView2's DevTools. They run
  alone, once every other check has run, since a whole application would compete with the timing of the checks
  beside it.

#### The Android check host

`tests/e2e/Components/Forma.Blazor.Browser.Android` is a MAUI application for Android whose one web view renders every
component's check page.

- It registers through the product's WebView half, and is listed in a solution of its own beside it, since it builds
  against the `maui-android` workload.
- The browser step builds it where `-Android` asks for it. Without it, the Android checks report themselves skipped.
- Each component check connects to its web view afresh through the DevTools socket adb forwards and loads its page
  there, one at a time. It closes the connection as it ends, which takes back what the check added to the page.

The Android checks and the MAUI example's smoke check on Android reach the device through `AndroidDevice` in
`tests/support/Forma.Tests.Support.Hosts`.

### Example hosts and smoke checks

`examples/` at the repository's root holds the example hosts:

- a Web App and its client for each culture arrangement;
- an application for each Blazor Hybrid host: WPF, Windows Forms and MAUI on Windows.

Each is set up as the library's README sets a host up, behind build files of its own, so the examples reference
Forma's packages and take nothing from the repository's build. They are listed in a solution of their own,
`examples/Forma.Examples.slnx`, which restores once the browser or mobile step has packed the packages.

**The examples restore into a packages folder of their own**, `artifacts/browser/restored`, which their `nuget.config`
names, so the machine's package cache never holds what the repository packed. The browser step packs the solution
at the version the root properties file declares, clears that version from the examples' packages folder, publishes
the examples and drives them.

**The smoke checks** start each example and use it once:

- `tests/smoke/Forma.Examples.Smoke.Tests` is an xUnit program the browser step runs before the browser check. Each
  Web App's page turns a page of the library's pagination in its Server island and its WebAssembly island. Each
  Blazor Hybrid application loads the library's stylesheets from its published assets and turns a page in its web
  view.
- `tests/smoke/Forma.Examples.Maui.Smoke.Tests` drives the MAUI example on Android, run by the mobile step.

**`tests/unit/Examples/Forma.Examples.Tests`** holds each README block tagged with an example's file to that file.

### Known failures

What the code does today is a plain check. A requirement it doesn't meet yet is a known failure, carrying the `Kind`
trait `KnownFailure`. A known failure passes while the requirement fails and fails once it is met, so the change that
meets it makes it a plain check.

## Build and verification

### The gate

**`build.ps1` at the root runs the gate**, so a person and a machine run the same checks:

| Command | Runs |
|---------|------|
| `./build.ps1` | The full gate |
| `./build.ps1 Fast` | The fast gate |
| `./build.ps1 DotNet`, `Scripts` or `Browser` | That part of the gate |
| `./build.ps1 -Clean` | Deletes build output, alone or before a run |

The gate covers every command below but the last. It also builds the task programs written in C#, which no other
step reaches, and checks each task script's shape:

```powershell
dotnet build Forma.slnx
dotnet test Forma.slnx
dotnet format Forma.slnx --verify-no-changes
dotnet tool restore
dotnet locali --path Forma.slnx --severity warning
npm test --prefix tests/scripts
./scripts/build/browser.ps1
dotnet run --project src/Blazor/Forma.Blazor.Showcase
```

[The verification design](verification-design.md#gates) sets out which gates a change runs before its commit, and
what it leaves to the full runs in CI.

**The fast gate** builds the unit and bUnit suites and what they reference once, through a solution filter it writes
under `artifacts/fast/`, and runs them from that build. It also runs the script tests and the task programs' tests,
for feedback within minutes.

**The full gate runs every check the machine can.** On Windows that includes WebView2's checks. Where adb reaches a
device and the `maui-android` workload is installed, it includes the components in Android's web view and the MAUI
example's smoke check on Android, through the steps `browser.ps1 -Android` and `mobile.ps1 -Platform Android` run.
Its summary names each platform check the machine couldn't run, with the reason.

**On Windows, the browser step also drives the Blazor Hybrid example hosts.** It publishes them and drives them
through WebView2, closing a native window as its close button does. The MAUI one builds against the `maui-windows`
workload, which a Windows machine installs once, from an elevated shell, with `dotnet workload install maui-windows`,
and which CI's Windows run installs itself. The step stops at once, naming the command, where the workload is
missing.

**The gate reports its progress.** The test step shows each test project as it finishes, with how many have finished
and the time left against the run before, and closes with the totals and the failing tests by name. Every step's
heading gives the time it took the last time it passed, kept under `artifacts/build/`.

**The Showcase stays a check made by hand**, started through `scripts/Start-Showcase.ps1`, since it never exits.

### CI

| Workflow | Runs | When |
|----------|------|------|
| `fast.yml` | The fast gate, on Linux | Every push but `main`'s and `candidate`'s, and every pull request into `dev` |
| `full.yml` | The full gate on Linux and on Windows; the MAUI example's build and smoke check on Android, and the components in Android's web view, both on an emulator of 1280 by 800 points; on the nightly run, the browser check twice more on Linux and on Windows, and a report that files each failing check as an issue | Every push to `candidate`, nightly on `dev`, and a run dispatched by hand |
| `canary.yml` | The full gate on Linux against the newest preview SDK in place of the pinned one, then the list of packages with newer versions, as a report alone | Every Monday at 06:00 UTC, and a run dispatched by hand |

The MAUI example's Apple builds, for iOS and Mac Catalyst, need a Mac whose Xcode is the one their workloads name,
which no hosted Mac carries yet.

### Narrower loops

Beside the build's module, `scripts/build/` holds one lowercase script for each part of the gate, so a run that only
needs one project's tests doesn't wait for the whole gate:

| Script | Does |
|--------|------|
| `compile.ps1` | Builds the solution, without tests or the format check |
| `test.ps1` | Runs the tests: the solution's, the script tests, or both |
| `format.ps1` | Verifies formatting |
| `tasks.ps1` | Builds the task programs written in C# |
| `browser.ps1` | Publishes the browser check's hosts and drives them in a browser; `-Visible` in a window a person can watch, and `-Android` on the Android check host's web view too, on the device or emulator adb reaches |
| `mobile.ps1` | Builds the MAUI example host from the packed packages for `-Platform` `Android`, `iOS` or `MacCatalyst`, and drives the Android build on the device or emulator adb reaches, where one is attached |
| `clean.ps1` | Deletes build output |

`mobile.ps1` reaches what no other step does: each platform needs its MAUI workload, and Apple's need a Mac. CI's full
workflow runs it on Linux for Android.

**`test.ps1` narrows and repeats a .NET run:**

| Parameter | Does |
|-----------|------|
| `-Target` | Chooses the tests: `DotNet`, `Scripts`, or both |
| `-Project` | Runs one test project, named rather than spelled out as a path |
| `-Filter` | Narrows which tests run, as `dotnet test` takes it; a filter that runs no test fails the run |
| `-NoBuild` | Runs against the output already on disk |
| `-Times` | Repeats the run, continuing past a failure, so a test failing sometimes shows as a rate |
| `-LoadWith` | Runs other test projects over and over beside it, to load the machine |
| `-WaitLog` | Names the folder the waits a component test makes with diagnostics record themselves in, cleared of an earlier run's records first and read back as the waits that needed time |

**`browser.ps1` narrows and repeats the browser check the same way.** Each run writes its results to
`artifacts/browser/results`, and the step ends by listing each check that failed, with how many of the runs that
ran it failed it:

| Parameter | Does |
|-----------|------|
| `-Filter` | Narrows the examples' smoke checks and the browser checks, as `dotnet test` takes a filter; a filter that selects no check is refused before anything is published |
| `-Times` | Repeats the checks on the hosts published once, continuing past a failure |
| `-LoadWith` | Runs test projects over and over beside the checks, from the output already on disk, to load the machine |

**Each script is a wrapper.** What an action does lives in `scripts/build/Forma.Build.psm1`, so the gate and a single
action can't disagree about what a build is.

### How the build is tested

**Every action understands `-WhatIf`.** That lets `scripts/build/build.tests.ps1` assert what each script does by
running it without doing it, since a step that quietly ran nothing would otherwise look exactly like a step that
passed.

**Steps whose bodies can be wrong also run for real there**, against folders made for the purpose, since a step whose
body is wrong describes itself perfectly. Cleaning and the wait log are among them.

**The gate's tooling step** reads every script in `scripts/` for the shape one keeps there, and exercises those it can
run without a side effect, through `scripts/Test-TaskScript.ps1`. It then runs `scripts/build/build.tests.ps1`. A
script added and covered by nothing fails there, so it is covered, or it belongs under `.local/` as nobody's to
maintain.

### Package contents

The browser step compares each package's files with the list `eng/package-files.txt` keeps of them, and fails on a
file missing or unlisted, so a package gains or loses a file only by a change to that list. It writes what it found to
`artifacts/browser/package-files.txt`, in the list's form.

### Analyzers and the locale lint

- **Warnings are errors.** `TreatWarningsAsErrors` is on for `src` and `tests` alike. The `.editorconfig` severity
  table decides what the compiler reports, and the root file and the per-project files beneath it, such as
  `src/Blazor/Forma.Blazor/.editorconfig`, differ deliberately.
- **Components can't read or set the ambient culture.** A component project imports `eng/ComponentAnalyzers.targets`, which
  bans the members that read or set the ambient culture through `eng/BannedSymbols.Components.txt`. A component that
  reaches for `CultureInfo.CurrentCulture` fails the build, with the culture state member to use instead.
- **WebAssembly libraries keep their types once trimmed.** Every library a WebAssembly client loads builds with
  Forma's own analyzers in `eng/Forma.Analyzers`, tested by `eng/Forma.Analyzers.Tests` and shipped in no package.
  Their rules keep each type that crosses the interop boundary serializable once trimmed. Their generator keeps every
  annotated type's members with its library, since the trimmer applies an annotation only where code asks a value
  typed as that type for its type.
- **The locale lint** restores `locali`, pinned in `.config/dotnet-tools.json`, and runs it over the solution, so a
  machine without it fetches the pinned version rather than skipping the step.

No project sets `GenerateDocumentationFile`. A throwaway build lists the undocumented public members:

```powershell
dotnet build Forma.slnx -p:GenerateDocumentationFile=true --no-incremental -v:q -nologo |
    Select-String CS1591
```

### Scripts

**Whether the build runs a script decides what it is.** A script the gate would run belongs to the build. One that
fetches artwork, generates a checked-in asset or drives a browser against a running Showcase is a task script.

**Build scripts.** The build is entered through `build.ps1` at the root, with its module and its wrapper scripts under
`scripts/build/`. A build script is named for its action in lowercase, `compile.ps1` and `test.ps1`, because it is a
command someone types rather than a cmdlet they discover. The functions inside the module take `Verb-Noun` with an
approved verb, as PowerShell requires of anything a module exports.

**Task scripts.** Everything else in `scripts/` is a task script: PowerShell 7, cross-platform, named
`Verb-Noun.ps1`, such as `Get-TwemojiPack.ps1`, `Build-GlyphPack.ps1` and `Update-PluralRules.ps1`. `BrandMarks.psm1`
beside them is the module the brand-artwork scripts share.

**`Update-CheckIssue.ps1`** is the nightly run's report, which CI runs: it reads every run's results and files each
failing check as an issue, as [the verification design](verification-design.md#branches-and-promotion) sets out.
`-WhatIf` describes what it would file without reading or changing an issue.

**`Stop-MsBuild.ps1` and `Stop-DotNet.ps1`** are for a build that fails to copy a file nothing appears to be using.
MSBuild keeps its workers alive between builds, and they hold what they last built.

- `Stop-MsBuild.ps1` stops those workers. `Stop-DotNet.ps1` goes further and stops whatever is running out of the
  repository too.
- Both ask for the supported shutdown before stopping anything.
- `Stop-MsBuild.ps1` never reaches the language server an editor runs for C#. `Stop-DotNet.ps1` reaches it only under
  `-All`, which stops every other .NET process on the machine too.

**C# programs.** Work a script can't do in PowerShell without writing C# through it, browser automation and glyph
geometry so far, is a file-based C# program under `scripts/cs/` that the script runs: `Test-ComponentScale.ps1` runs
`cs/CheckComponentScale.cs`.

- **A check built around a tool of its own** keeps everything for that tool in a folder named for it, its program
  included, beside the script that runs it: `Test-ScreenReaderSpeech.ps1` runs `nvda/CheckScreenReaderSpeech.cs`,
  with the speech recorder NVDA loads from the same folder.
- **A program is named for what it does**, in PascalCase without the hyphen a cmdlet name carries, since it is a file
  rather than a command. The script that runs it keeps the approved verb.
- **A program declares the packages it needs in its own header**, which is why central package management stops at
  `scripts/` rather than reaching into it.
- **A program that is an entry point opens with a shebang**, and the gate builds each one, since nothing else
  compiles them.
- **A file included into a program rather than run** takes a plain noun for a name, as `GlyphParser.cs` does. Logic
  in a program that is worth testing moves into such a file, which the program includes and a test program beside it
  includes too. The test program is named for what it tests with `Tests` after it, as `GlyphParserTests.cs` is, and
  the gate's task step runs it once it has built it.

**Task programs build outside the repository**, in the SDK's own place in the system temp directory, and every edit
and rename leaves the folder before it behind there. `clean.ps1 -All` sweeps those too. It checks that a folder holds
nothing but what the SDK put there rather than trusting its name, and it reaches past this repository to anything
else run from a single file on the machine.

## Branches and releases

Work lands on `dev`, and a release reaches `main` by promotion, as
[the verification design](verification-design.md#branches-and-promotion) sets out after
[Versioning's rules](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/README.md).

**Every package ships at the one version the root properties file holds.** A commit on `dev` sets it before the
candidate's final run, and the examples read it from that file.

A release takes these steps:

1. **`candidate` fast-forwards to the candidate**, whose push runs the full workflow. Its Linux gate keeps the
   packages the browser step packed at that version, built the examples from and checked, as the artifact `packages`.
2. **A pull request from `candidate` into `main`** carries the required `promotion` check, which `promotion.yml` runs
   through Versioning's promotion check.
3. **The maintainer's release command promotes and tags the candidate.** Versioning's
   [`commands/New-Release.ps1`](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/README.md#the-release-command),
   run from a checkout of the Versioning release Forma follows, holds the release to the rules, signs the annotated
   tag `vX.Y.Z` on the candidate's exact commit, and pushes `main` and the tag in one atomic push, which marks the pull
   request merged.
4. **The tag starts `release.yml`.** It runs Versioning's release check and attaches the candidate's packages to the
   tag's GitHub release with their digests. It then publishes those packages, each after the packages it depends on,
   to nuget.org through its trusted publishing and to the organization's GitHub feed.

A publication that stops partway runs again for the same tag and publishes what the feeds lack, so a tag marks
exactly the commit its packages were built from.

## Personal work

Work that belongs to a person rather than to the repository lives under `.local/`, ignored in its entirety: the
benchmarks, the spikes, and a document written for a piece of work rather than for the repository itself. One ignored
root takes the place of a rule per directory, so something new that is nobody else's business needs no rule at all,
and nothing kept there can be staged by accident.

## Origins

Every component derives from the legacy admin application, which sits outside the workspace in a repository of its
own:

| What | Where |
|------|-------|
| Admin design system | `App_Themes/Cms/css-3.6.5.css`, `fa-3.6.5.css`, `global-3.6.5.css`, `responsive-3.6.5.css` |
| Page markup | `admin/*.aspx` and `admin/pops/*.aspx` |
| Behavior and rendering helpers | Matching `*.aspx.cs` files |
| Replaced jQuery widgets | `admin/Client/{toggle,date,cp,modal,Tabs,jcrop,minibar}` |

The legacy admin's boxy gradient toggle becoming a token-driven iOS-style `FormaToggleSwitch` is the intended level of
fidelity: the real states, vocabulary, edge cases and required behavior survive; the visuals are Forma's.

## Consumers

The current consumers are the admin application Forma first served and **Épica Deportiva**, brought in by project
reference or local NuGet. Épica's own documentation lists the subset it requires and tracks its status.
