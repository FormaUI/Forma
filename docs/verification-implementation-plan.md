# Forma Verification Implementation Plan

The plan for bringing the repository to [the verification design](verification-design.md): its steps, the
gate each passes, and their state, with the ledger of every move the steps make.

## Steps

Each step lands buildable and passes its gate before the next starts.

1. **The move ledger.** ✅ Each suite's current and proposed path, assembly name, kind, runtime and gate, below. A
   project mixing kinds is classified by its assertions. `Forma.Analyzers.Tests` moving under `eng/`, the
   JavaScript unit tests, which have no project file, and the Android example driver, apart from the 149
   Android component rows, are each accounted for. Gate: the maintainer's review of the ledger, whose decisions
   it records.
2. **The moves.** ✅ Projects move to their places with every reference to their paths: `Forma.slnx`, the build
   and mobile scripts, CI's path filters, the README's example markers, the repository's guides and the
   examples' build and NuGet configuration. Production assembly names and static asset paths stay as they are.
   Gate: the full gate, and each suite's discovered cases matching its baseline. Departures: the files moved one
   at a time, since the editor's language servers hold every project folder open and Windows renames no folder
   with a handle inside it, and git records each as a rename. The analyzers' tests take the tests' build
   settings through a `Directory.Build.props` of their own, and the test sources' rules moved from
   `tests/.editorconfig` to the root's severity table, keyed on the file name, so a suite takes them wherever it
   lives. `test.ps1 -Project` looks a level deeper and in `eng/`. By the maintainer's revisions of decisions 4
   and 8, the glyph rules' tests became a single-file program beside the rules, which the task step runs, and
   the JavaScript tests moved to `tests/scripts/`. Every suite's 5,163 .NET cases and the 958 JavaScript cases
   were discovered at their new places name for name.
3. **The splits, the support and smoke.** Mixed suites split by kind and helpers move by the support rule. After
   each split the cases discovered in every destination are compared with the baseline case by case, so an
   omitted case and a duplicate can't cancel out in a total. Smoke coverage is a deliverable of this step, not a
   by-product of the moves: a new check for each runnable example, which builds it from the packed packages,
   starts it, performs one Forma interaction and verifies its visible effect, discovered and run on its
   platform, and recorded apart from the moved cases so new coverage can't hide a lost one. The testing guide
   and the repository's working rules take the naming and support rules as they land. Gate: the full gate,
   discovery matching the baselines, and every new smoke check run on its platform.
4. **The gates.** `./build.ps1 Fast`, its selection verified to run on Linux and timed from a job's start to its
   result, setup and restore included; the step timings decide any further work, such as running the
   JavaScript and .NET tests side by side. `./build.ps1` stays the full local gate, and names each platform
   check the machine couldn't run. Gate: the fast selection's discovery and timing, and the full gate's report
   on a machine without WebView2 or an Android device.
5. **The promotion workflow and publication.** CI runs the fast gate on feature branches, pull requests into
   `dev` and integrated `dev` commits; the full gate, every required platform job included, nightly on `dev` and
   on each `preview` promotion; and the release on an annotated tag. Once a full `preview` run passes and the
   candidate is approved, `main` fast-forwards to that exact SHA and the annotated `vX.Y.Z` tag goes on it; the
   tag's workflow verifies the version and the approved SHA, then publishes the packages of that revision.
   Android's E2E is confirmed in both full workflows before the weekly Android workflow retires. Gate:
   publication exercised end to end against a dry run or a test feed, and a later commit without a passing
   `preview` run refused.

## Ledger

### Decisions

| # | Question | Decision |
| --- | --- | --- |
| 1 | The MAUI operations tests' temporary folder | Classified per test by what must work for its assertion to pass. The one test that reads back the file Forma's operation wrote is integration; the four that check a decision (the share request's address, Windows declining, the file's name kept inside its folder, the registration) are unit. |
| 2 | `CultureFixtureTests` | Stays in `Forma.Blazor.Tests`. Its subject is the shared support's `CultureFixture`, not `Forma.Blazor`. |
| 3 | The README's setup check | Unit, in `Forma.Examples.Tests`, in the fast gate: reading the README and the examples supplies the comparison's inputs. |
| 4 | `Forma.Scripts.Tests` | Unit: all 25 cases parse a drawing's text in process, and none launches PowerShell or crosses a process boundary. Revised by the maintainer: the tests move beside the rules they test, as `scripts/cs/GlyphParserTests.cs`, a single-file test program that names its packages in its own header and includes `GlyphParser.cs`, and the project goes; the gate's `tasks` step runs each task program named `*Tests.cs` once it has built it. |
| 5 | E2E's groups | `Components/` and `Clients/`; the browser check keeps its name, `Forma.Blazor.Browser.Tests`. |
| 6 | Host lifecycle | `Forma.Tests.Support.Hosts`: publishing, launching, waiting until ready, stopping and cleaning up after a failed start. Smoke and E2E each own their fixture's lifetime and their assertions. |
| 7 | Smoke checks | One for each runnable example: Coordinated and its client as one Web App, Independent and its client as another, Wpf, WindowsForms, and Maui on Windows; MAUI on Android has a smoke suite of its own. Both web culture-transition checks stay in E2E. |
| 8 | The JavaScript tests | One Vitest project, its tests grouped by the assembly whose modules they exercise, in the fast gate. Revised by the maintainer: at `tests/scripts/`. |

Six suites split, five into unit and bUnit and one into unit and integration; one loses a reference it never
used; one is only renamed.

### `src/`, `examples/` and `eng/`

Production assembly names and static asset paths don't change.

| Current | Proposed |
| --- | --- |
| `src/Forma.AddOns/*` (6) | `src/AddOns/*` |
| `src/Forma.Blazor`, `.Composer`, `.DataView`, `.Explorer`, `.Server`, `.Showcase`, `.WebAssembly`, `.WebView` (8) | `src/Blazor/*` |
| `src/Forma.Framework/*` (34) | `src/Framework/*` |
| `src/Forma.Locales/*` (2) | `src/Locales/*` |
| `tests/Forma.Examples/*`: Coordinated and its client, Independent and its client, Wpf, WindowsForms, Maui, with their build and NuGet configuration and README | `examples/*`; the package file imports the root's one level up instead of two |
| `tests/Forma.Analyzers.Tests` | `eng/Forma.Analyzers.Tests`, unit, 20 cases, in the fast gate |

### Unit, bUnit and integration

Every suite here runs on .NET on Linux and is in the fast gate. Cases are the runner's own count, theory rows
included.

| Current | Proposed path | Assembly | Kind | Cases |
| --- | --- | --- | --- | --- |
| `Forma.AddOns.Composer.Plugins.Explorer.Tests` (18) | `tests/bunit/AddOns/` | `Forma.AddOns.Composer.Plugins.Explorer.BUnit.Tests` | bUnit | 14 |
| | `tests/unit/AddOns/` | `Forma.AddOns.Composer.Plugins.Explorer.Tests` | unit | 4 |
| `Forma.AddOns.Operations.Maui.Tests` (5) | `tests/unit/AddOns/` | `Forma.AddOns.Operations.Maui.Tests` | unit | 4 |
| | `tests/integration/AddOns/` | `Forma.AddOns.Operations.Maui.Integration.Tests` | integration | 1 |
| `Forma.AddOns.Emojis.Twemoji.Tests`, `.Resources.Markdown.Tests`, `.Resources.Office.Tests`, `.Share.Targets.Tests` | `tests/unit/AddOns/` | same | unit | 19, 26, 32, 29 |
| `Forma.Blazor.Tests` (1,527) | `tests/bunit/Blazor/` | `Forma.Blazor.BUnit.Tests` | bUnit | 1,466 |
| | `tests/unit/Blazor/` | `Forma.Blazor.Tests` | unit | 61 |
| `Forma.Blazor.Composer.Tests` (171) | `tests/bunit/Blazor/` | `Forma.Blazor.Composer.BUnit.Tests` | bUnit | 142 |
| | `tests/unit/Blazor/` | `Forma.Blazor.Composer.Tests` | unit | 29 |
| `Forma.Blazor.DataView.Tests` (251) | `tests/bunit/Blazor/` | `Forma.Blazor.DataView.BUnit.Tests` | bUnit | 248 |
| | `tests/unit/Blazor/` | `Forma.Blazor.DataView.Tests` | unit | 3 |
| `Forma.Blazor.Explorer.Tests` (123) | `tests/bunit/Blazor/` | `Forma.Blazor.Explorer.BUnit.Tests` | bUnit | 123 |
| `Forma.Blazor.Server.Tests` (38) | `tests/bunit/Blazor/` | `Forma.Blazor.Server.BUnit.Tests` | bUnit | 26 |
| | `tests/unit/Blazor/` | `Forma.Blazor.Server.Tests` | unit | 12 |
| `Forma.Blazor.WebAssembly.Tests`, `Forma.Blazor.WebView.Tests` | `tests/unit/Blazor/` | same | unit | 3, 6 |
| `Forma.Framework.Emojis.Unicode.Tests` | `tests/unit/Framework/` | same | unit | 3 |
| The other 29 `Forma.Framework.*.Tests` | `tests/unit/Framework/` | same | unit | 1,788 together |
| `Forma.Scripts.Tests` | `scripts/cs/GlyphParserTests.cs` | (single-file program) | unit | 25 |
| `ExampleHostTests.Should_set_up_each_example_as_the_readme_says`, from the browser check | `tests/unit/Examples/` | `Forma.Examples.Tests` | unit | 1 |
| `Forma.Blazor.Tests.Scripts` (Vitest, no project file) | `tests/scripts/` | (npm) | unit | 958 |

`Forma.Blazor.Tests`' unit cases are `LocalePackTests`, `ServiceCollectionExtensionsTests`,
`ComponentContextTests`, `BreadcrumbItemTests`, `EmojiSpriteLoaderTests`, `CultureFixtureTests`,
`DateRangeTests`, `SizeTests` and `ZoomScaleTests`; the registrar's and the sprite loader's take the support's
`FakeJSRuntime` in place of bUnit's, and `LocalePackTests` finds its assembly through a type of its own. The
Composer's are `ComposerPluginTests` and `RichTextPluginTests`, DataView's `DataViewGroupingTests`, the
Server's `HostApplicationBuilderExtensionsTests`, and the Explorer add-on's `ExplorerPluginTests`.
`Forma.Framework.Emojis.Unicode.Tests` drops its unused bUnit reference.

### Smoke and E2E

Smoke and E2E run in the full gate.

| Current | Proposed | Assembly | Runtime | Cases |
| --- | --- | --- | --- | --- |
| `WebViewExampleTests.Should_load_the_library_s_stylesheets_from_its_published_assets`, from the browser check | `tests/smoke/Forma.Examples.Smoke.Tests` | `Forma.Examples.Smoke.Tests` | WebView2, Windows | 3 |
| (new) | same | same | Web App and WebView2 | one per runnable example, 5, recorded apart |
| `tests/Forma.Blazor.Browser.Android.Tests` | `tests/smoke/Forma.Examples.Maui.Smoke.Tests` | `Forma.Examples.Maui.Smoke.Tests` | MAUI, Android | 4 |
| `tests/Forma.Blazor.Browser.Tests` | `tests/e2e/Forma.Blazor.Browser.Tests` | same | every runtime | 1,071 |
| `tests/Forma.Blazor.Browser.Components.Checks`, `.Components`, `.Components.Client`, `.WebView`, `.Android` | `tests/e2e/Components/*` | same | | |
| `tests/Forma.Blazor.Browser.Host`, `.Host.Client`, `.Full` | `tests/e2e/Clients/*` | same | | |

The browser check keeps every component check, `CultureTests`, `DownloadTests`, `NavigationBoundaryTests`,
`RenderModeTests`, `WebAssemblyCultureTests`, `WebViewCultureTests`, `WebViewProbeTests`, `ExampleHostTests`'
two culture checks and `WebViewExampleTests`' window-title switch, closed window's release and saved culture.
`Forma.Blazor.Browser.Host` and its client serve the runtime checks (`RenderModeTests`,
`NavigationBoundaryTests`), the culture checks (`CultureTests`, `WebAssemblyCultureTests`) and the minimal
client's download budget (`DownloadTests`), beside `Forma.Blazor.Browser.Full`, the full client's.

### Support

| Current | Proposed |
| --- | --- |
| `tests/Forma.Tests.Support` | `tests/support/Forma.Tests.Support`, keeping the types two or more suites use |
| `tests/Forma.Tests.Support.Blazor` | `tests/support/Forma.Tests.Support.Blazor`, keeping the bUnit context, `RenderedComponentExtensions`, `DomProbe` and `InputRouterProbe` |
| (new) | `tests/support/Forma.Tests.Support.Hosts`: `WebViewApplication`, the web hosts' start from `BrowserHost`, and `AndroidDevice` |

Types one suite uses, counted against the suites after the splits, move to that suite's `Support/` folder:

| Suite | Types |
| --- | --- |
| `Forma.Blazor.BUnit.Tests` | `FakeEmojiProvider`, `FakeEmojiSpriteLoader`, `RecordingDebounceable`, `FakeBrowserFile`, `CultureText`, `PinnedCultures`, `FakePlayerProvider`, `ChunkedFileStore`, `FakeFileStore`, `FakeGatedResourceProvider`, `FakeResourceProvider`, `FakeStalledFileStore`, `FakeAccountShareTarget`, `FakeShareConnections`, `FakeModel` |
| `Forma.Framework.Localization.Tests` | `FakeLocaleFileFormat`, `FakeLocaleSource`, `PreparedLocaleSource`, `StubPluralRuleSet`, `LocaleSections`, `TempDirectory` |
| `Forma.Framework.Localization.Json.Tests` | `RootedWidget`, `RootedToolbar` |
| `Forma.AddOns.Resources.Office.Tests` | `OfficeFixture` |
| `Forma.Framework.Input.Interop.Tests` | `HeldJSObjectReference` |
| `Forma.Framework.Culture.Router.Tests` | `FakeRenderer` |
| `Forma.Framework.Culture.Blazor.WebView.Tests` | `UIThreadSynchronizationContext` |
| `Forma.Framework.Windows.Tests` | `FakeWindow` |

The MAUI fakes, `FakeDeviceInfo`, `FakeFileSystem` and `FakeShare`, stay shared: the MAUI suite's split gives
them two consumers.

### Discovery baselines

Captured before the first move from every suite's runner, theory rows enumerated: 5,163 .NET cases in 47
suites and 958 JavaScript cases, no name duplicated in any suite. The browser check's 1,075 are its 926 checks
outside Android and its 149 Android component rows; the MAUI example's Android driver has 4 cases of its own.
After a move or a split, each destination's discovered cases, named relative to its root namespace, must equal
its expected list exactly, so a case lost and a case duplicated can't offset each other; new smoke checks are
counted apart.

### Paths that follow the moves

- `Forma.slnx`, and the build module's context: the browser check's projects, the examples, the script tests.
- CI: `ci.yml`'s and `canary.yml`'s npm cache path and path filters.
- The README's example markers and the examples' README.
- Test code naming paths: `ComponentChecks.ReadPagePaths` and `RenderModeInventoryTests`, `DownloadTests`,
  `CultureRouteMapTests`' fixture shared with the JavaScript tests, and `scripts/cs/GlyphParser.cs`'s comment
  naming its tests.
- The repository's guides and documents: `CLAUDE.md`, `docs/repository-structure.md`, the testing and verifying
  skills, and any document naming a moved path.
