# Features Status

## Current state

**Forma has 81 implemented components. It runs statically, under Interactive Server, WebAssembly and Auto, and in Blazor Hybrid on WPF, Windows Forms and MAUI on Windows. Every component is checked statically, under each interactive render mode and in a WPF web view, every call it makes to a script and every call back among it; each Blazor Hybrid host's setup is checked from its example; and an inventory holds every component to every render mode.** The product, infrastructure and release work that remains is tracked below, beside the [components still to build](#components-to-build).

| Measure | Now |
| --- | --- |
| Components | 82 implemented, as [the catalog](component-catalog.md) lists them, and 20 planned, as [the roadmap](component-roadmap.md) orders them |
| Packages | Every project under `src/` but the Showcase, released from `main` by `release.yml` to nuget.org and the organization's GitHub feed, the latest published release `1.0.0-preview.20` |
| Render modes | Interactive Server, WebAssembly and Auto implemented: every call a component makes to a script and every call back checked in each, and on the Server in WebKit, the engine of Safari and of Apple's web views, every runtime on a page following its culture coordinator, a client reading its host's own strings, and a host setting either arrangement up as the README says; static rendering implemented, every check page rendered statically with no enabled control that needs interactivity; Blazor Hybrid implemented on WPF, Windows Forms and MAUI on Windows, every component checked in a web view and each host's application set up as the README says; an inventory holds every component to a check page in every render mode |

The gate runs every check [the verification design](verification-design.md#gates) sets out, each platform's where the machine can run it. It passes locally on Windows and in CI on Linux, on Windows and on an Android emulator.

[The catalog](component-catalog.md) lists the components that exist and [the roadmap](component-roadmap.md) those to build. Each area's design under `docs/` is its contract, and an implementation plan beside a design carries the steps still to build it. [The feature history](../FEATURE-HISTORY.md) records what each month added and what it broke. This file tracks where each feature stands and the work that remains. "Implemented" means built and passing the gate; a feature waiting for the maintainer's approval stays in progress.

<a id="remaining-work"></a>

## Remaining work

| Work | State |
| --- | --- |
| [Components to build](#components-to-build) | The roadmap, the notes editor, export button, design picker and font picker first |
| [Blazor Hybrid on phones and Macs](#blazor-hybrid-on-phones-and-macs) | The component suite on iOS and Mac Catalyst |
| [The DOM watcher and engine](#the-dom-watcher-and-engine) | Steps 5 to 11 open |
| [The composer's later phases](#the-composers-later-phases) | Planned |
| [The build](#the-build) | Faster tests, a faster gate, builds in processes of their own and the examples' locale lint; the affected-checks command next |
| [The input router's host routes](#the-input-routers-host-routes) | Installed on every interactive page with a Forma component |
| [A host's integration](#a-hosts-integration) | Fifteen findings, all fixed |
| [A host's requests](#a-hosts-requests) | Four components implemented, twenty-six changes fixed, and a path per culture open |
| [A security check](#a-security-check) | Every step in place; the CI failures #61, #62 and #64 open |
| [Themes](#themes) | Steps 1 to 7 landed, six steps open, the server's half next |
| [The Showcase's review modes](#the-showcases-review-modes) | Planned, nine steps open, the pointer emulation's prototype and the preview first |

- **One component at a time,** built, reviewed and approved before the next starts, as the working rules in `CLAUDE.md` set out.
- **A change is a commit,** each passing its own checks, and the full gate runs at integration and release, as [the verification design](verification-design.md#gates) sets out.
- **Breaking changes say how to migrate** from the first release on: [the changelog](../CHANGELOG.md)'s entry names each change that breaks what the release before it allowed, as [Versioning's preview stage](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/versions.md#stages) promises. Until then they were made directly, and [the feature history](../FEATURE-HISTORY.md) lists what each month broke.
- **Documents move with their change.** A design, a README, the catalog, the roadmap, this file and the changelog change in the commit that changes what they describe.

<a id="features"></a>

## Features

Each feature, the document that states it, its state, and the month it first landed, which [the feature history](../FEATURE-HISTORY.md) records, or for a feature that lands after the first release, the release it first shipped in, which [the changelog](../CHANGELOG.md) records.

### Components

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| Bars: the toolbar, the search and selection toolbars, the status strip | [Catalog](component-catalog.md#formatoolbar), [toolbar design](toolbar-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Buttons: the button, close, eye and toggle buttons | [Catalog](component-catalog.md#formabutton), [icon buttons design](icon-buttons-design.md) | Implemented; eight more icon buttons [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Charts: the line and bar charts | [Charts design](charts-design.md) | Implemented | [`1.0.0-preview.14`](../CHANGELOG.md#100-preview14) |
| Collections: the sortable list | [Catalog](component-catalog.md#formasortablelisttitem) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Feedback: dialogs, popovers, toasts, the spinner, status badges and the capacity indicator | [Catalog](component-catalog.md#formadialog), [popover design](popover-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Files: the file picker, uploader and viewer and the image picker | [Catalog](component-catalog.md#formafileviewer), [resources design](resources-design.md) | Implemented; the media picker [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Inputs: seventeen fields, text, numbers, currency, choices, dates and times among them, each in the field chrome | [Catalog](component-catalog.md#formafield), [time inputs](time-inputs-design.md), [currency input](currency-input-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Layout: backdrops, stacks, frames and chromes, the command area, the freeze pane, the splitter and tabs | [Catalog](component-catalog.md#formabackdrop) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Media: the video player | [Catalog](component-catalog.md#formavideoplayer), [media design](media-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Menus: the drop-down, overflow and share menus | [Catalog](component-catalog.md#formadropdownmenu), [share design](share-design.md) | Implemented; the context menu [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Navigation: the breadcrumb, pagination and link tabs | [Catalog](component-catalog.md#formabreadcrumb) | Implemented | [July 2026](../FEATURE-HISTORY.md#2026-07) |
| Page: the page title and the document head | [Catalog](component-catalog.md#formapagehead) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Symbols: icons, emoji and the emoji picker | [Catalog](component-catalog.md#formaicon), [glyph artwork specification](glyph-artwork-specification.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Windows: managed windows and the rest area | [Catalog](component-catalog.md#formawindow), [window design](window-design.md) | Implemented; the launch bar [planned](#components-to-build) | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Input scopes and the input inspector | [Catalog](component-catalog.md#formainputscope), [input router design](input-router-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Virtualization, `FormaVirtualize` | [Virtualization design](virtualization-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |

### Products

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The data view: one engine over a grid, a list, cards and a tree, with its sort menu and view switch | [Data view design](data-view-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The explorer | [Explorer design](explorer-design.md) | Implemented, its content on the data view | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| The composer, its rich text editor and its plugins | [Composer design](composer-design.md) | Implemented through its phases 1 to 3, 5 and 8; [later phases](#the-composers-later-phases) planned | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| The gallery | [Gallery design](gallery-design.md) | Planned | — |

### Foundations

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The component bases and the component context | [Component foundations design](component-foundations-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Design tokens, backdrops and theming | [Styling and theming](../README.md#styling-and-theming) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Themes: named themes in a light and a dark scheme, the reader's choice kept and switched in place | [Theme design](theme-design.md) | [In progress](#themes): a theme and a scheme drawn wherever an element names them, Forma's in both schemes and Forma High Contrast beside it | — |
| The culture state and the switch | [Culture design](culture-design.md) | Implemented, coordinated across a page's circuits and WebAssembly runtimes, in an application's own cultures where it provides them | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Culture addresses | [Culture addresses](culture-design.md#culture-addresses) | Implemented, in coordination, in every runtime | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Localization and locale packs | [Localization design](localization-design.md) | Implemented: every locale file, in whatever layout its project chose, read into one entry, a culture, an assembly, a type and a key with its text, and embedded by the build from its entry; the text chosen by culture, then source; a conflict within a source logged by the runtime and reported by the locale lint as an error; a WebAssembly client and a Web App's server reading the host's strings; code written against Microsoft's `IStringLocalizer` reading the same strings through `Forma.Framework.Localization.StringLocalizerAdapter` | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| The input router | [Input router design](input-router-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The browser transfer, text edits and the edit boundary | [Browser transfer design](browser-transfer-design.md) | Implemented: every value a reader can make large crosses under Blazor's default hub limit, in every runtime; text fields and the rich text editor send their edits; an action in a form, a host's own form's submit and validation among them, waits for every field, a composer among them | [November 2026](../FEATURE-HISTORY.md#2026-11) |
| The DOM watcher and engine | [DOM watcher and engine design](dom-watcher-engine-design.md) | Implemented for focus, scrolling, intersection, autofill and size; [the rest](#the-dom-watcher-and-engine) open | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The window manager | [Window design](window-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Resources, files and the catalog | [Resources design](resources-design.md), [`Forma.Framework.Catalog`](../src/Framework/Forma.Framework.Catalog/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Sharing | [Share design](share-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Icons and glyphs | [Glyph artwork specification](glyph-artwork-specification.md), [`Forma.Framework.Icons`](../src/Framework/Forma.Framework.Icons/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Emojis | [`Forma.Framework.Emojis`](../src/Framework/Forma.Framework.Emojis/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Media | [Media design](media-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Feedback services | [`Forma.Framework.Feedback`](../src/Framework/Forma.Framework.Feedback/README.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Sanitization | [`Forma.Framework.Sanitization`](../src/Framework/Forma.Framework.Sanitization/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Plugins | [`Forma.Framework.Plugins`](../src/Framework/Forma.Framework.Plugins/README.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Currencies | [`Forma.Framework.Currencies`](../src/Framework/Forma.Framework.Currencies/README.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Events and debouncing | [`Forma.Framework.Events`](../src/Framework/Forma.Framework.Events/README.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Typed CSS values | [`Forma.Framework.Css`](../src/Framework/Forma.Framework.Css/README.md) | Implemented | [July 2026](../FEATURE-HISTORY.md#2026-07) |
| JavaScript interop | [`Forma.Framework.JavaScript.Interop`](../src/Framework/Forma.Framework.JavaScript.Interop/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |

### Hosting

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The server half, `AddFormaServerIntegration` and `FormaPageHead` | [`Forma.Blazor.Server`](../src/Blazor/Forma.Blazor.Server/README.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The WebAssembly half, `AddFormaWebAssemblyIntegration` | [WebAssembly design](webassembly-design.md) | Implemented: every call a component makes to a script and every call back checked under WebAssembly and Auto, each runtime following the page's culture coordinator, an independent client culture and a standalone app's page included, a client reading its host's own strings, and the README's setup an example host for each arrangement | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Trimming | [The client's build](webassembly-design.md#the-clients-build) | Implemented: every client library trimmed, Forma's analyzer keeping each type crossing the interop serializable and its generator keeping those types' members with their library | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Blazor Hybrid, the WebView half | [WebView design](webview-design.md) | Implemented on WPF, Windows Forms and MAUI on Windows: `AddFormaWebViewIntegration`, one culture across an application's web views or [a culture per web view](culture-design.md#a-culture-per-web-view), a host's own strings embedded in its application, every component checked in a web view, and the README's setup an example host for each | [November 2026](../FEATURE-HISTORY.md#2026-11) |
| Blazor Hybrid on Android, iOS and Mac Catalyst | [WebView design](webview-design.md#native-operations) | In progress: the MAUI example host builds for Android from the packages and runs on an emulator in CI, its web view driven by its smoke check on Android, every component checked in an Android web view, and builds for iOS and Mac Catalyst in CI on a hosted Mac, saving, printing and sharing through the platform's share sheet with `Forma.Framework.Browser.Maui`; the components are not yet driven on iOS and Mac Catalyst | — |

### Build and verification

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The gate and CI on Linux and Windows | [Build and verification](repository-structure.md#build-and-verification) | Implemented, Android's checks in [the environment the repository declares](repository-structure.md#the-android-environment), its JDK and its emulator, on every machine and in CI alike | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The browser check | [Tests](repository-structure.md#tests) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The Showcase's review modes: a page in each device, pointer, theme, scheme and direction, and side by side | [Showcase design](showcase-design.md) | [Planned](#the-showcases-review-modes) | — |
| The screen reader check | [Screen reader checks design](screen-reader-checks-design.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Releases | [Build and verification](repository-structure.md#build-and-verification) | Implemented: each release published from its signed tag on `main` to nuget.org and the organization's GitHub feed, and proven against what it published: a consumer outside the repository reading a locale pack it restored from a feed into an empty cache, and a project outside it declaring `IsLocalePack` packing as a pack | [September 2026](../FEATURE-HISTORY.md#2026-09) |

<a id="components-to-build"></a>

## Components to build

[The roadmap](component-roadmap.md) orders what is built next: the notes editor, export button, design picker and font picker; the permission matrix, product options editor and filter editor; the remaining icon buttons; the context menu; the media picker; the launch bar; the gallery; and the image editor.

<a id="blazor-hybrid-on-phones-and-macs"></a>

## Blazor Hybrid on phones and Macs

A MAUI application on Android builds from Forma's packages, in Release and trimmed, and runs on an emulator in CI,
where its smoke check on Android drives its web view: its strings switching with the culture, its stylesheets and
its share menu reaching the share sheet. On iOS and Mac Catalyst it builds in CI on a hosted Mac carrying Xcode 27,
reported beside Android's without holding a release to it. Each saves, prints and shares
through the platform's share sheet with
[`Forma.Framework.Browser.Maui`](../src/Framework/Forma.Framework.Browser.Maui/README.md), as the WebView design's
[native operations](webview-design.md#native-operations) set out. Every component's check page runs on Android
as a runtime of its own beside the Web App's and WebView2's, in a check host of its own on an emulator, nightly
on `dev`, on every push to `candidate` and on demand in CI, and in Playwright's WebKit, the engine of Apple's web
views, in the browser check. What remains is the component suite on iOS's simulator and a Mac, whose `WKWebView` a
check reaches through Safari's remote inspector.

<a id="the-dom-watcher-and-engine"></a>

## The DOM watcher and engine

[Its plan](dom-watcher-engine-implementation-plan.md) has landed the component context's disposal, the watcher and engine, intersection and focus, steps 0 to 4, and of step 11 the autofill and size observations. Steps 5 to 11 move the rest of the browser work onto them: DOMQL as the language a component's queries are written in, the input router's participation, and each remaining behavior, operation and component script, retiring the kind folders as each empties.

DOMQL's language now lives in Nexo, which was ported out of DOMQL, and DOMQL has moved onto it in its own repository. The queries [the DOM watcher and engine design](dom-watcher-engine-design.md) writes as examples predate the notation DOMQL took with Nexo, which [replacing Forma's JavaScript with DOMQL](forma-javascripts-domql-replacement.md) writes, and are brought up to it with the design.

<a id="the-composers-later-phases"></a>

## The composer's later phases

[Its plan](composer-implementation-plan.md) leaves three phases open: the Markdown and code editors with `Forma.AddOns.Composer.Markdown`; the satellite capabilities, `Forma.AddOns.Composer.TextMate`, `.KaTeX` and a hardened sanitizer; and the page designer, with its palette, canvas, widget tree and the text widget hosting the rich text editor, which lands last since it composes everything else.

<a id="the-build"></a>

## The build

- **Faster tests.** The suite runs in about 13 seconds on the build on disk, its test projects side by side. The explorer's component tests stand in classes named for their subjects, which run side by side too, taking 5 seconds where one class of 129 took 16, and so do the data view grid's, the data view's project taking 11 seconds in the fast gate where it took 15 to 19. What remains: the Office provider's tests, one class run in turn, set the fast gate's slowest suite at about 10 seconds; and the localization build's integration tests set the full suite's time, one of them running a build for 9 seconds.
- **A faster gate.** The format check reads each tracked C# file's whitespace, line endings, final newline and charset in about 6 seconds, where it took 132, the build holding the code style; the test step runs on the build the gate has just made; the build's own cases and the task scripts' contract run side by side, in about 20 seconds where they took 60; a filtered browser run builds its checks and their hosts once, through a solution filter, and starts the host and the components host from that build rather than publishing them, so the chart checks in two runtimes take about a minute where the step spent four minutes building and publishing before its first check; a filtered Android run builds the check host untrimmed, as a plain package, the narrow grid's eight checks taking three minutes where they took nine; a build outside CI stamps no commit into its assemblies, so a build after a commit finds the projects it didn't change up to date, a 50-second no-op where it compiled the solution again; and a change set's gate runs in one kept worktree, moved to each commit, the steps the set reaches. What remains: the locale lint's 32 seconds, which are the linter's own, loading the solution, and which a faster Locali takes up.
- **Builds in processes of their own.** Every project or solution built from the command line in the repository, by the
  gate or by hand, starts build processes of its own and uses no MSBuild server, as `Directory.Build.rsp` asks, so it
  holds each project under the path as it spells it itself. A server or a worker node an earlier build left held a
  project under that build's spelling, and a build spelling the drive letter in the other case listed
  `Forma.Framework.Culture.Browser.Interop`'s JS initializer twice and stopped in its compression; the incremental build
  takes the time it took before.
- **The examples' locale lint.** From Locali 1.0.0-preview.27 a run given a solution reads the files of the projects
  it loads alone, so `Forma.Examples.slnx` is linted in a run of its own. The examples restore only once their
  packages are packed, so the browser step makes the run, after it has published every example, on Windows, where
  the Blazor Hybrid examples publish; elsewhere the gate names it among the checks the machine couldn't run. The run
  reads a linting configuration of the examples' own, in `examples/`, which allows the locale files each example's
  build embeds.
- **The affected-checks command, next.** [The verification design](verification-design.md#what-a-change-runs) runs a
  change's checks by its affected set. A command is to compute that set from a change set's commits: the test projects
  and classes and the browser check pages each changed path reaches, transitively, with the render modes, engines and
  platforms its reach adds, each check named with why it was chosen, and the set widened where a path's reach isn't
  mapped. Until then, the set is worked out by hand from the same rules.

<a id="the-input-routers-host-routes"></a>

## The input router's host routes

A host's own routes, such as the shortcut that opens a command palette, answer on every interactive page that renders a
Forma component: the first one to render interactively has the input router carry the host's policy to the page, as
[its design](input-router-design.md#preemptive-routes-are-absolute) means a palette's shortcut to, whether or not any
component on the page registers with the router.

<a id="a-hosts-integration"></a>

## A host's integration

Reviewing a host's integration against Forma's source turned up fifteen findings, listed here in the order they are taken, each with what the code does and its state.

| Finding | What the code does | State |
| --- | --- | --- |
| A tab submits its form | `FormaTabs` rendered its tab buttons without a type, so inside a form choosing a tab, by press, Enter or Space, submitted it. Every other button a component renders is typed, and every Enter a component consumes goes through the input router, which cancels the browser's default | Fixed |
| The explorer's asynchronous first load | `FormaExplorer` heard from its data view only after its first render, which on the Server and in a web view runs once the page has taken that render in, so a store answering within that round trip announced its answer to nobody and the explorer showed it loading until a refresh. It now hears from the data view as it captures it | Fixed |
| A menu runs its command before it closes | `FormaDropDownMenuItem` awaited its `OnClick` before it closed the menu, so a command opening a dialog kept the menu open while the dialog was up, and one removing its row left focus on a removed element. An item that doesn't keep the menu open now closes it, handing the focus back to its trigger, before its command runs; where the focus goes once a row is gone stays with the list or the host | Fixed |
| A field's description | No input carried instructions in its `aria-describedby`, and `FormaField`'s `FooterContent` served the password box alone. Every input now takes a `Description`, drawn beneath it and named after the host's own ids, its field, a checkbox's label or the composer's frame drawing it | Fixed |
| Focusing an input | No input had a focus operation, and the roots of `FormaDateTimePicker` and the composer are wrappers rather than what takes the focus. Every input now has a `FocusAsync`, which focuses the control it is worked in and answers whether it asked an element, answering false before the input is drawn, once it is disposed, under a static render and while it is disabled | Fixed |
| A paged data view's reserved height | `FormaDataView` held a full page's height whenever it paged and never let it go, a single page included, as its design's rule said. It now holds it only while another page is known to exist, by its count, its source's `HasMore` or a page before the one shown, and lets it go once the set fits one page; the design says so | Fixed |
| Upload refusals | `IFileStore.SaveAsync` refused only by throwing, and `CatalogConstraints` had no per-file ceiling. `SaveAsync` now answers with the resource it saved or a `FileRefusal`, its reason and the limit the file went past, which `FormaFileUploader` words for the reader and reports apart from a failure; `CatalogConstraints.MaxFileBytes` refuses a file in `FormaExplorer` as it is picked, and the store still keeps its own limits | Fixed |
| A link that looks like a button | `FormaButton` always rendered a `<button>`. Given an `Href`, it is now a link in the button's look, with `Target` and `Rel`; disabled or busy, it names no address and stays a link, and it neither submits nor reads a form | Fixed |
| Type-ahead in the select | `FormaSelect` and `FormaDropDownMenu` matched no typed label, and only `FormaDataTree` jumped, by one letter. All three now search with one `TypeAhead`, a prefix built within a pause, a space among it, and a repeated character cycling, in the culture in force, the select opening on the match and choosing nothing, and the menu reading each item's label as the page shows it | Fixed |
| Resetting the image picker | Neither `FormaFilePicker` nor `FormaImagePicker` could be cleared, and the file dialog couldn't choose the same file twice in a row. Each now has a `ClearAsync`, which lets go of the files, previews and refusals and empties the file input; nothing clears on its own, since a host's handling of the files doesn't show that each was taken | Fixed |
| The toast container's offsets | `FormaToastContainer` sat `--space-5` from the block-start and inline-end edges, with no hook of its own, and a toast could run past a narrow screen. It now reads `--forma-toast-container-inset-block-start` and `--forma-toast-container-inset-inline-end`, which the README shows, and keeps a toast between them | Fixed |
| The page title's actions | `FormaPageTitle`'s row didn't wrap and its heading couldn't shrink. Its actions now move beneath the heading, still at the end, where the two don't fit one line, and a long word breaks; `FormaDialog`'s title shrinks beside its close button too | Fixed |
| Localized routing | The culture router replaced the culture segment and kept the rest of the path. A host's `ICultureDestinationResolver` now names where a page leads in another culture, its own address there or a fallback, and every switch, link and web view following its application's culture goes there; publication stays the host's | Fixed |
| Localized references carried as data | No type carried a localization source, a key and arguments to resolve when displayed. `LocalizableText` now does, localized by `IStringLocaleFactory.Localize` in the cultures in force when it is shown, and the locale lint checks its keys as it checks a lookup's | Fixed |
| The composer's integration guidance | The composer's README said to sanitize on the server, but not how authoring and saving share one policy, that the selected plugins don't set the policy, or how a stored video placeholder is rendered. It now says each, with every attribute a placeholder carries, a sanitizer pass over what the composer kept changes nothing, and the icon set has a solid `Pause` beside `TriangleRight` | Fixed |

<a id="a-hosts-requests"></a>

## A host's requests

A host's list of what it needs from Forma named four components Forma lacked, `FormaComboBox`, `FormaLinkTabs`,
`FormaLineChart` and `FormaBarChart`, all now implemented, as [the catalog](component-catalog.md) lists them, and a
search of its commands, which `FormaCommandPalette` is. It also named twenty-seven changes to components that exist, listed
here in the order they are taken, each with what the code does and its state.

| Request | What the code does | State |
| --- | --- | --- |
| A read-only text area | `FormaTextArea` had no `ReadOnly`, which the text box and the number, currency, date and time fields have. It takes one now, and every labeled field draws a read-only element with the read-only fill, its hook `--forma-input-readonly-bg-color` | Fixed |
| A select option's mark | A `SelectOption` was a value and a label. It carries an optional `Icon`, drawn ahead of the label in the select's and the multi-select's lists, trigger and tags, and an `IconLabel` describing the option to assistive technology | Fixed |
| Tabs past their width | `FormaTabs` wrapped its tabs onto a second line once they ran out of width. Its strip keeps one line and scrolls behind an arrow at each end, and a freeze pane keeps the focused element in view as an arrow appearing narrows it | Fixed |
| A badge's colors from a theme | `StatusBadgeStyle` took its colors as `CssColor` values. It takes `CssColorValue`s, a literal color or a custom property a theme defines, rendered as `var(--name)` | Fixed |
| The canvas under a dark scale | `--backdrop-canvas-bg-color` is a step toward ink on the gray scale, so a theme reversing the scale drew the canvas lighter than the backdrops laid on it. The theme contract, in the README and the token stylesheet, names the canvas among what a dark theme sets, since its scale holds no step below the paper, and the emphasis of each role it uses whose emphasis mixes toward black | Fixed |
| The explorer's toolbar in a bounded height | The header keeps its height in an explorer held short, but an open upload panel kept its full height and pushed the media out of the explorer. The media now give way first, and the panel then scrolls what it holds | Fixed |
| A row removed under the focus | A command removing the row it was run from, as a row's menu deleting the row does, left the focus on the page's body. The data view hands it to the item after it, else the one before, else the first control of its empty state. A command that confirms first left it there too, the dialog handing it back to the row's trigger, which had gone with the row; a dialog or a popover whose opener went with an item now hands the focus to the same heir | Fixed |
| Sharing from a static page | `FormaShareMenu`'s trigger was disabled on a page rendered statically. It opens the browser's own popover there, its networks, email and download links and the commands a script runs disabled | Fixed |
| A role's color as text in a dark theme | A role's `-emphasis-color` was both the hover fill under white text and the role's color as text on the paper, which no one dark value serves. Each role now has an `-ink-color` for text, the emphasis in the light theme, which a dark theme sets apart | Fixed |
| The share menu's commands on a static page | Copy Link, Print and the device's share were drawn disabled on a page rendered statically. The page's script enables each the browser can run and runs it | Fixed |
| A list as wide as its field | The combo box's, the select's and the multi-select's lists were as wide as their options, narrower than a wide field. Each is now at least as wide as its field | Fixed |
| A stepper on a static page | A statically rendered stepper, measured by no renderer, cut each name short at a phone's width. The page's script measures it, so its steps not current show by their numbers | Fixed |
| A stepper's current marker apart from its marks | `FormaStepper` filled its current marker in the active color its completed steps' marks take, and no one shade fills the marker deep enough for its number and draws the marks light enough on a dark band. The current marker takes its fill from `--forma-stepper-current-marker-bg-color`, the active color where it's unset | Fixed |
| An image picker's types | `FormaImagePicker` set `Accept` to `image/*` whatever the host gave. It takes the image types the host names, and every image type where it names none | Fixed |
| A multi-select's search by every word, without accents | `FormaMultiSelect` matched the text typed by case alone. It finds an option by every word typed, ignoring case, accents and width, ranks the options as `FormaComboBox` does, and offers to add the text only where no option holds that name | Fixed |
| A disabled control's reason | A disabled control left the tab order and the pointer's reach, so a reason attached to it was never met. [The tooltip design](tooltip-design.md) adds `FormaTooltip`, and `IDisableable`, which the buttons, the inputs, the menus, the toolbars, the sortable list and the composer implement, each showing its `DisabledReason` as its tooltip; the data view's items show theirs through `ItemDisabledReason`, the explorer's catalog items among them, and a region, the frame, the chrome and the window, makes its content inert whole, its content's controls saying why | Fixed |
| A path per culture | The culture router gives a page one path in every culture. A route of one identity with a path in each culture, `/es/noticias` and `/en/news`, which links, alternate addresses and the switch all read, with a page to lead to where a culture has no counterpart, is planned; a host routes such pages itself until then, as it does today | Planned, not under way |
| The data view on a phone | At a phone's width a grid keeps every column its priority holds, its check box and actions columns among them, so a cell is left a few letters wide and its text wraps a letter or two a line. Built as the maintainer set it out, as [the data view design](data-view-design.md#at-a-narrow-width) holds it: at a narrow width a row is stacked into two lines, or three where its detail needs them, the item's picture at its start where it has one, its name on one line and its detail beneath, cut short rather than wrapped, and no other columns; in place of the actions column one menu of three dots offering Select and the host's commands, Edit and Delete among them, each enabled by what is selected and by the host's context, a disabled one saying why; a row's own actions from a swipe toward its inline start and from a command the keyboard and a screen reader reach; and selection on demand, from a long press or a command, a row's box appearing at its start and pushing its content toward its end, and a card's mark on its thumbnail | Fixed |
| Tabs on a phone | `FormaTabs` and `FormaLinkTabs` scrolled sideways behind arrows at every pointer, so at a phone's width a title was cut mid-word under an arrow. Now, under a coarse pointer, the freeze pane's arrow form they scroll in scrolls by a swipe, a title cut at an edge fading out, and the arrows stay with a fine pointer | Fixed |
| One mark on a field that opens a list | `FormaSelect`, `FormaMultiSelect`, `FormaDateBox` and `FormaDateRangePicker` drew a `▾` character at the extra-small type size, where `FormaComboBox` draws the chevron icon in a button, so two fields side by side carried two marks. Now every field that opens a list draws the combo box's chevron icon | Fixed |
| A picker's prompt under touch | `FormaFilePicker` and `FormaImagePicker` took one `Prompt`, "Drop files here or click to browse" unless a host gives another, so a phone was told to drop where nothing can be dropped. Now both take `TouchPrompt`, which a coarse pointer reads, speaking of choosing alone, the library's own or a host's | Fixed |
| The composer's toolbar on a phone | At a phone's width the composer's bars stood a row each, each scrolling with an overflow of its own. Now, under a coarse pointer, the bars stand in one row a finger swipes | Fixed |
| A text area's grip under touch | `FormaTextArea` kept `resize: vertical` under a coarse pointer, whose grip a finger can't drag. Now it draws no grip under a coarse pointer, a longer text scrolling within the area's rows | Fixed |
| Switches in a column | `FormaToggleSwitch` stood right after its label, so a column of switches was ragged. Now, under a coarse pointer, the switch stands at its row's end, its label at the start | Fixed |
| The explorer on a phone | `FormaExplorer` was a box of `40rem` at most that scrolled inside the page. On a phone, a viewport under 40rem wide or under 40rem tall beneath a coarse pointer, it now has no height of its own, so its items are the page's own list, an infinite scroll appending each page as the page nears their end, its header staying at the top; a height the host gives it keeps it a box. The virtualizer follows what scrolls a list now, so a container that grows with its content leaves its list to the page | Fixed |
| A line chart's axis on a phone | At a phone's width `FormaLineChart` drew its value axis's labels over the series they measure. Now both charts label their values in a gutter at the plot's inline start, as wide as the longest label, the marks and the time axis standing past it at every width | Fixed |
| The data view's Load More | `FormaDataViewMore`'s Load More was a ghost button, which read as text. Now it is a secondary button, drawn as one | Fixed |

<a id="a-security-check"></a>

## A security check

[The security design](security-design.md) states what Forma accepts from a host and from the browser, what it writes
into a page from the data it's given, and the checks that hold it to both. [Its implementation plan](security-implementation-plan.md)
carries it out in seven steps:

1. ✅ One standard for validating arguments, applied across the codebase and enforced by the analyzers.
2. ✅ Every place Forma writes markup, an address or a style from data, found from the source and tested with hostile
   input or held to a typed value, an address with a script-running scheme written inert.
3. ✅ Malformed input from the browser refused, never thrown.
4. ✅ A cap on every size Forma accepts from the browser, the uploader's 10 MB by default among them.
5. ✅ A job of the full gate failing on a known vulnerability in any NuGet or npm package.
6. ✅ An OWASP ZAP scan of the running Showcase, rerun from a script.
7. ✅ Forma under a strict Content Security Policy.

<a id="themes"></a>

## Themes

[The theme implementation plan](theme-implementation-plan.md) carries out [the theme design](theme-design.md) in
thirteen steps and four parts:

- the switch coordinator, taken out of the culture coordinator;
- the themes, which a host draws by writing the two attributes itself;
- the reader's choice, kept, resolved, painted first and switched in place, with the picker;
- Blazor Hybrid, once each platform's document-start script is proven.

The first two parts are independent of each other. Steps 1 to 7 have landed: the culture coordinator runs on the
switch coordinator the theme's is to be built on, the tokens draw the theme and the scheme the nearest element naming
them carries, so a host draws a theme of its own by writing the two attributes, `Forma.Framework.Themes.Testing` holds a
theme to the contrast targets, Forma's theme meets them in both its schemes, as Forma High Contrast, a stylesheet
beside the tokens, meets its own in both of its, `FormaThemeScope` draws a part of a page in a theme or a scheme of its
own, and a host registers its themes with `AddFormaThemes`, beside Forma's two.

<a id="the-showcases-review-modes"></a>

## The Showcase's review modes

[The Showcase implementation plan](showcase-implementation-plan.md) carries out [the Showcase design](showcase-design.md)
in nine steps. A prototype proving the pointer emulation and a preview that keeps nothing come first, then the
review bar, the device view, the pointer emulation, and direction and comparison. The themes in a view, the
window's theme picker and the Themes page follow, each waiting on the theme plan's steps it names. None of the
steps has landed.

<a id="deferred"></a>

## Deferred

Each waits for the trigger named beside it before its design or its implementation resumes.

| Work | Why it waits | Reopened by |
| --- | --- | --- |
| Observing the document's child list | No consumer needs it, and its cost is unmeasured | A consumer with a measured cost |
| Growing the DOM watcher and engine's design beyond its plan | Its plan's steps come first | Those steps landed |
| A System preference following the device's `prefers-contrast` | Forma High Contrast is chosen explicitly first, as [the theme design](theme-design.md#light-dark-and-high-contrast) sets out | The theme system in use |
| The device view as a component for hosts | It serves the Showcase's review alone, as [the Showcase design](showcase-design.md#where-the-parts-live) sets out | A host's concrete need |
