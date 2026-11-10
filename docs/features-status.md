# Features Status

## Current state

**Forma has 66 implemented components and 8 in progress. It runs statically, under Interactive Server, WebAssembly and Auto, and in Blazor Hybrid on WPF, Windows Forms and MAUI on Windows. Every component is checked statically, under each interactive render mode and in a WPF web view, every call it makes to a script and every call back among it; each Blazor Hybrid host's setup is checked from its example; and an inventory holds every component to every render mode.** The product, infrastructure and release work that remains is tracked below, beside the [components still to build](#components-to-build) and the [open decisions](#open-decisions).

| Measure | Now |
| --- | --- |
| Components | 66 implemented and 8 in progress, as [the catalog](component-catalog.md) lists them, and 21 planned, as [the roadmap](component-roadmap.md) orders them |
| Packages | Every project under `src/` but the Showcase, released from `main` by `release.yml` to nuget.org and the organization's GitHub feed, the latest published release `1.0.0-preview.4` |
| Render modes | Interactive Server, WebAssembly and Auto implemented: every call a component makes to a script and every call back checked in each, every runtime on a page following its culture coordinator, a client reading its host's own strings, and a host setting either arrangement up as the README says; static rendering implemented, every check page rendered statically with no enabled control that needs interactivity; Blazor Hybrid implemented on WPF, Windows Forms and MAUI on Windows, every component checked in a web view and each host's application set up as the README says; an inventory holds every component to a check page in every render mode |

The gate runs every check [the verification design](verification-design.md#gates) sets out, each platform's where the machine can run it. It passes locally on Windows and in CI on Linux, on Windows and on an Android emulator.

[The catalog](component-catalog.md) lists the components that exist and [the roadmap](component-roadmap.md) those to build. Each area's design under `docs/` is its contract, and an implementation plan beside a design carries the steps still to build it. [The feature history](../FEATURE-HISTORY.md) records what each month added and what it broke. This file tracks where each feature stands and the work that remains. "Implemented" means built and passing the gate; a feature waiting for the maintainer's approval stays in progress.

<a id="remaining-work"></a>

## Remaining work

| Work | State |
| --- | --- |
| [Components to build](#components-to-build) | `FormaVirtualize` awaits approval; the data view's steps 11 and 13; then the roadmap |
| [Blazor Hybrid on phones and Macs](#blazor-hybrid-on-phones-and-macs) | The component suite on iOS and Mac Catalyst |
| [The DOM watcher and engine](#the-dom-watcher-and-engine) | Steps 5 to 11 open |
| [The composer's later phases](#the-composers-later-phases) | Planned |
| [Screen reader checks](#screen-reader-checks) | The data view's remaining scenarios |
| [The build](#the-build) | Faster tests and a faster gate |
| [Release](#release) | A locale pack proven from a feed |
| [The naming sweep](#the-naming-sweep) | The survey's renames after the add-on assemblies', group by group |

- **One component at a time,** built, reviewed and approved before the next starts, as the working rules in `CLAUDE.md` set out.
- **A change is a commit,** each passing the gate.
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
| Collections: the sortable list | [Catalog](component-catalog.md#formasortablelisttitem) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Feedback: dialogs, popovers, toasts, the spinner, status badges and the capacity indicator | [Catalog](component-catalog.md#formadialog), [popover design](popover-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Files: the file picker, uploader and viewer and the image picker | [Catalog](component-catalog.md#formafileviewer), [resources design](resources-design.md) | Implemented; the media picker [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Inputs: sixteen fields, text, numbers, currency, choices, dates and times among them, each in the field chrome | [Catalog](component-catalog.md#formafield), [time inputs](time-inputs-design.md), [currency input](currency-input-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Layout: backdrops, stacks, frames and chromes, the command area, the freeze pane, the splitter and tabs | [Catalog](component-catalog.md#formabackdrop) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Media: the video player | [Catalog](component-catalog.md#formavideoplayer), [media design](media-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Menus: the drop-down, overflow and share menus | [Catalog](component-catalog.md#formadropdownmenu), [share design](share-design.md) | Implemented; the context menu [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Navigation: the breadcrumb and pagination | [Catalog](component-catalog.md#formabreadcrumb) | Implemented | [July 2026](../FEATURE-HISTORY.md#2026-07) |
| Page: the page title and the document head | [Catalog](component-catalog.md#formapagehead) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Symbols: icons, emoji and the emoji picker | [Catalog](component-catalog.md#formaicon), [glyph artwork specification](glyph-artwork-specification.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Windows: managed windows and the rest area | [Catalog](component-catalog.md#formawindow), [window design](window-design.md) | Implemented; the launch bar [planned](#components-to-build) | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Input scopes and the input inspector | [Catalog](component-catalog.md#formainputscope), [input router design](input-router-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Virtualization, `FormaVirtualize` | [Virtualization design](virtualization-design.md) | Built; awaits approval | [September 2026](../FEATURE-HISTORY.md#2026-09) |

### Products

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The data view: one engine over a grid, a list, cards and a tree, with its sort menu and view switch | [Data view design](data-view-design.md) | In progress: steps 11 and 13 of [its plan](data-view-implementation-plan.md); step 12 is built with the gallery | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The explorer | [Explorer design](explorer-design.md) | Implemented; moving its content onto the data view is the data view's step 11 | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| The composer, its rich text editor and its plugins | [Composer design](composer-design.md) | Implemented through its phases 1 to 3, 5 and 8; [later phases](#the-composers-later-phases) planned | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| The gallery | [Gallery design](gallery-design.md) | Planned | — |

### Foundations

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The component bases and the component context | [Component foundations design](component-foundations-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Design tokens, backdrops and theming | [Styling and theming](../README.md#styling-and-theming) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| The culture state and the switch | [Culture design](culture-design.md) | Implemented, coordinated across a page's circuits and WebAssembly runtimes, in an application's own cultures where it provides them | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Culture addresses | [Culture addresses](culture-design.md#culture-addresses) | Implemented, in coordination, in every runtime | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Localization and locale packs | [Localization design](localization-design.md) | Implemented, every locale file embedded by the build from its entry, a WebAssembly client and a Web App's server reading the host's strings | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| The input router | [Input router design](input-router-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The browser transfer, text edits and the edit boundary | [Browser transfer design](browser-transfer-design.md) | Implemented: every value a reader can make large crosses under Blazor's default hub limit, in every runtime; text fields and the rich text editor send their edits; an action in a form, a host's own form's submit and validation among them, waits for every field, a composer among them | [November 2026](../FEATURE-HISTORY.md#2026-11) |
| The DOM watcher and engine | [DOM watcher and engine design](dom-watcher-engine-design.md) | Implemented for focus, scrolling and intersection; [the rest](#the-dom-watcher-and-engine) open | [October 2026](../FEATURE-HISTORY.md#2026-10) |
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
| The server half, `AddFormaUIServer` and `FormaPageHead` | [`Forma.Blazor.Server`](../src/Blazor/Forma.Blazor.Server/README.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The WebAssembly half, `AddFormaUIWebAssembly` | [WebAssembly design](webassembly-design.md) | Implemented: every call a component makes to a script and every call back checked under WebAssembly and Auto, each runtime following the page's culture coordinator, an independent client culture and a standalone app's page included, a client reading its host's own strings, and the README's setup an example host for each arrangement | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Trimming | [The client's build](webassembly-design.md#the-clients-build) | Implemented: every client library trimmed, Forma's analyzer keeping each type crossing the interop serializable and its generator keeping those types' members with their library | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Blazor Hybrid, the WebView half | [WebView design](webview-design.md) | Implemented on WPF, Windows Forms and MAUI on Windows: `AddFormaUIWebView`, one culture across an application's web views or [a culture per web view](culture-design.md#a-culture-per-web-view), a host's own strings embedded in its application, every component checked in a web view, and the README's setup an example host for each | [November 2026](../FEATURE-HISTORY.md#2026-11) |
| Blazor Hybrid on Android, iOS and Mac Catalyst | [WebView design](webview-design.md#native-operations) | In progress: the MAUI example host builds for Android from the packages and runs on an emulator in CI, its web view driven by its smoke check on Android, every component checked in an Android web view, and builds for iOS and Mac Catalyst once a Mac carries Xcode 27, saving, printing and sharing through the platform's share sheet with `Forma.AddOns.Operations.Maui`; the components are not yet driven on iOS and Mac Catalyst | — |

### Build and verification

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The gate and CI on Linux and Windows | [Build and verification](repository-structure.md#build-and-verification) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The browser check | [Tests](repository-structure.md#tests) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The screen reader check | [Screen reader checks design](screen-reader-checks-design.md) | Implemented in part; [the data view's scenarios](#screen-reader-checks) remain | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Releases | [Build and verification](repository-structure.md#build-and-verification) | Implemented in part; [the rest](#release) planned | [September 2026](../FEATURE-HISTORY.md#2026-09) |

<a id="components-to-build"></a>

## Components to build

`FormaVirtualize` is built and awaits approval, after which it moves into the catalog. The data view, with its views and controls, is finished once two steps of [its plan](data-view-implementation-plan.md) land: the explorer's content moving onto it, and a tree's tab stop returning to the node last focused. Its third open step, the gallery's browsing on it, is built with the gallery and stays open until the gallery starts. [The roadmap](component-roadmap.md) orders what follows: the category selector; the notes editor, export button, design picker and font picker; the permission matrix, product options editor and filter editor; the remaining icon buttons; the context menu; the media picker; the launch bar; the gallery; and the image editor.

<a id="blazor-hybrid-on-phones-and-macs"></a>

## Blazor Hybrid on phones and Macs

A MAUI application on Android builds from Forma's packages, in Release and trimmed, and runs on an emulator in CI,
where its smoke check on Android drives its web view: its strings switching with the culture, its stylesheets and
its share menu reaching the share sheet. On iOS and Mac Catalyst the .NET 11 release candidate's workloads need
Xcode 27, which no hosted Mac carries yet, so those two build on a Mac by hand. Each saves, prints and shares
through the platform's share sheet with
[`Forma.AddOns.Operations.Maui`](../src/AddOns/Forma.AddOns.Operations.Maui/README.md), as the WebView design's
[native operations](webview-design.md#native-operations) set out. Every component's check page runs on Android
as a runtime of its own beside the Web App's and WebView2's, in a check host of its own on an emulator, nightly
on `dev`, on every push to `candidate` and on demand in CI. What remains is the component suite on iOS's
simulator and a Mac, whose `WKWebView` a check reaches through Safari's remote inspector, once a Mac carries
Xcode 27.

<a id="the-dom-watcher-and-engine"></a>

## The DOM watcher and engine

[Its plan](dom-watcher-engine-implementation-plan.md) has landed the component context's disposal, the watcher and engine, intersection and focus, steps 0 to 4. Steps 5 to 11 move the rest of the browser work onto them: DOMQL as the language a component's queries are written in, the input router's participation, and each remaining behavior, operation and component script, retiring the kind folders as each empties.

DOMQL's language now lives in Nexo, which was ported out of DOMQL, and DOMQL has moved onto it in its own repository. The queries [the DOM watcher and engine design](dom-watcher-engine-design.md) writes as examples predate the notation DOMQL took with Nexo, which [replacing Forma's JavaScript with DOMQL](forma-javascripts-domql-replacement.md) writes, and are brought up to it with the design.

<a id="the-composers-later-phases"></a>

## The composer's later phases

[Its plan](composer-implementation-plan.md) leaves three phases open: the Markdown and code editors with `Forma.AddOns.Composer.Markdown`; the satellite capabilities, `Forma.AddOns.Composer.TextMate`, `.KaTeX` and a hardened sanitizer; and the page designer, with its palette, canvas, widget tree and the text widget hosting the rich text editor, which lands last since it composes everything else.

<a id="screen-reader-checks"></a>

## Screen reader checks

[The check](screen-reader-checks-design.md) runs the tree's scenarios, the checkbox's, the time picker's, the date and time picker's, the currency input's, and the slow source's first load in English. The data view's remaining announcements become scenarios in English and Hebrew: a move to the second page and a refresh each saying loading and then the count, the tree's first load the same, taking away the last page's only item and putting everything back each saying the new count, a fourth box pressed in a bounded selection saying the refusal, and the slow source's first load in Hebrew. Once they pass, the data view's manual screen reader check retires.

<a id="the-build"></a>

## The build

- **Faster tests.** The slowest test projects and tests are measured and made faster, starting with the tests that wait on time and those that load the machine.
- **A faster gate.** The gate's total time comes down, starting with its longest step, the format check; and the test step builds the solution again although the gate has just built it, about a minute where the suite takes about 20 seconds on the build already on disk.

<a id="release"></a>

## Release

Forma's releases from `1.0.0-preview.1` to `1.0.0-preview.4` are published, as [the changelog](../CHANGELOG.md) lists them. `release.yml` releases a version from its signed tag on `main`, through Versioning's release check, and publishes the packages the candidate's full run packed and checked, each after the packages it depends on, to nuget.org and the organization's GitHub feed. What remains:

- Prove, against published packages, that a consumer outside the repository taking a locale pack from a feed loads it, and that a project outside the repository declaring `IsLocalePack` packs as a pack.

<a id="the-naming-sweep"></a>

## The naming sweep

A survey finds 629 names of variables, constants and properties that are a bare adjective or participle, or a count named as a plural, where the naming standard names each for the noun it holds. They land in small groups, each reviewed with its call sites; the add-on assemblies' group has landed, and the next group comes for review.

<a id="open-decisions"></a>

## Open decisions

Every decision raised so far is decided.

<a id="deferred"></a>

## Deferred

Each waits for the trigger named beside it before its design or its implementation resumes.

| Work | Why it waits | Reopened by |
| --- | --- | --- |
| An `IStringLocalizerFactory` over Forma's localization | Nothing needs one. It is adopted only once it proves its contract: both factory overloads map to Forma's resources; missing resources, fallback, enumeration and `ResourceNotFound` behave consistently; resource selection reads the UI culture and argument formatting the formatting culture; its lifetime captures no one user's state; and it coexists with a host's own factory in both registration orders, since `TryAdd` keeps only a factory registered before it | A host asking for one |
| Observing the document's child list | No consumer needs it, and its cost is unmeasured | A consumer with a measured cost |
| Growing the DOM watcher and engine's design beyond its plan | Its plan's steps come first | Those steps landed |
| Profiling select-all and a sort in a virtualized data view | The Showcase has no virtualized scenario offering either over an instant source | Such a scenario |
