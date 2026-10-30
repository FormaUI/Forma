# Features Status

## Current state

**Forma has 65 implemented components and 8 in progress. Interactive Server is its established hosting mode, and WebAssembly and Auto are implemented in part.** The current work is [WebAssembly](#webassembly), steps 9 to 12; the product, infrastructure and release work that remains is tracked below, beside the [components still to build](#components-to-build) and the [open decisions](#open-decisions).

| Measure | Now |
| --- | --- |
| Components | 65 implemented and 8 in progress, as [the catalog](component-catalog.md) lists them, and 21 planned, as [the roadmap](component-roadmap.md) orders them |
| Packages | Every project under `src/` but the Showcase, each at `1.0.0-preview`, packed locally and unpublished |
| Render modes | Interactive Server established; WebAssembly and Auto implemented in part: the components load and render, and the culture's coordination is still to come |

The gate is the build with warnings as errors, every test project, the format check, the task scripts, the locale lint, the script tests and the browser check, which drives the components headless under Server, WebAssembly and Auto. It passes locally on Windows. In CI, on Linux and Windows, every step passes but the locale lint, which fails with a 403 until the repository is given access to the package feed it restores `locali` from. The browser check holds [four known failures](#webassembly), each checked to fail as recorded, so a passing gate shows they still fail, never that WebAssembly is complete.

[The catalog](component-catalog.md) lists the components that exist and [the roadmap](component-roadmap.md) those to build. Each area's design under `docs/` is its contract, and an implementation plan beside a design carries the steps still to build it. [The feature history](../FEATURE-HISTORY.md) records what each month added and what it broke. This file tracks where each feature stands and the work that remains. "Implemented" means built and passing the gate; a feature waiting for the maintainer's approval stays in progress.

<a id="remaining-work"></a>

## Remaining work

| Work | State |
| --- | --- |
| [WebAssembly](#webassembly) | Steps 9 to 12 next; step 6's trimming awaits a decision |
| [Components to build](#components-to-build) | `FormaVirtualize` awaits approval; the data view's steps 11 and 13; then the roadmap |
| [The DOM watcher and engine](#the-dom-watcher-and-engine) | Steps 5 to 11 open |
| [The composer's later phases](#the-composers-later-phases) | Planned |
| [Screen reader checks](#screen-reader-checks) | The data view's remaining scenarios |
| [The build](#the-build) | Faster tests and a faster gate |
| [Release](#release) | Planned |

- **One component at a time,** built, reviewed and approved before the next starts, as the working rules in `CLAUDE.md` set out.
- **A change is a commit,** each passing the gate.
- **Breaking changes are made directly,** without compatibility layers, migration notes or deprecations, until the first release; [the feature history](../FEATURE-HISTORY.md) lists what each month broke.
- **Documents move with their change.** A design, a README, the catalog, the roadmap, this file and the feature history change in the commit that changes what they describe.

<a id="features"></a>

## Features

Each feature, the document that states it, its state, and the month it first landed, which [the feature history](../FEATURE-HISTORY.md) records.

### Components

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| Bars: the toolbar, the search and selection toolbars, the status strip | [Catalog](component-catalog.md#formatoolbar), [toolbar design](toolbar-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Buttons: the button, close, eye and toggle buttons | [Catalog](component-catalog.md#formabutton), [icon buttons design](icon-buttons-design.md) | Implemented; eight more icon buttons [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Collections: the sortable list | [Catalog](component-catalog.md#formasortablelist) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Feedback: dialogs, popovers, toasts, the spinner, status badges and the capacity indicator | [Catalog](component-catalog.md#formadialog), [popover design](popover-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Files: the file picker, uploader and viewer and the image picker | [Catalog](component-catalog.md#formafileviewer), [resources design](resources-design.md) | Implemented; the media picker [planned](#components-to-build) | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Inputs: seventeen fields, text, numbers, currency, choices, dates and times among them, each in the field chrome | [Catalog](component-catalog.md#formafield), [time inputs](time-inputs-design.md), [currency input](currency-input-design.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
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
| The culture state and the switch | [Culture design](culture-design.md) | Implemented for circuits, coordinated across a page; WebAssembly's registration is [step 10](#webassembly) | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Culture addresses | [Culture addresses](culture-design.md#culture-addresses) | Implemented; routing in coordination is [step 9](#webassembly) | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Localization and locale packs | [Localization design](localization-design.md) | Implemented; a host's strings in the browser are [step 11](#webassembly) | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| The input router | [Input router design](input-router-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The DOM watcher and engine | [DOM watcher and engine design](dom-watcher-engine-design.md) | Implemented for focus, scrolling and intersection; [the rest](#the-dom-watcher-and-engine) open | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The window manager | [Window design](window-design.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Resources, files and the catalog | [Resources design](resources-design.md), [`Forma.Framework.Catalog`](../src/Forma.Framework/Forma.Framework.Catalog/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Sharing | [Share design](share-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Icons and glyphs | [Glyph artwork specification](glyph-artwork-specification.md), [`Forma.Framework.Icons`](../src/Forma.Framework/Forma.Framework.Icons/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Emojis | [`Forma.Framework.Emojis`](../src/Forma.Framework/Forma.Framework.Emojis/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Media | [Media design](media-design.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Feedback services | [`Forma.Framework.Feedback`](../src/Forma.Framework/Forma.Framework.Feedback/README.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Sanitization | [`Forma.Framework.Sanitization`](../src/Forma.Framework/Forma.Framework.Sanitization/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |
| Plugins | [`Forma.Framework.Plugins`](../src/Forma.Framework/Forma.Framework.Plugins/README.md) | Implemented | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| Currencies | [`Forma.Framework.Currencies`](../src/Forma.Framework/Forma.Framework.Currencies/README.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Events and debouncing | [`Forma.Framework.Events`](../src/Forma.Framework/Forma.Framework.Events/README.md) | Implemented | [March 2026](../FEATURE-HISTORY.md#2026-03) |
| Typed CSS values | [`Forma.Framework.Css`](../src/Forma.Framework/Forma.Framework.Css/README.md) | Implemented | [July 2026](../FEATURE-HISTORY.md#2026-07) |
| JavaScript interop | [`Forma.Framework.JavaScript.Interop`](../src/Forma.Framework/Forma.Framework.JavaScript.Interop/README.md) | Implemented | [August 2026](../FEATURE-HISTORY.md#2026-08) |

### Hosting

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The server half, `AddFormaUIServer` and `FormaPageHead` | [`Forma.Blazor.Server`](../src/Forma.Blazor.Server/README.md) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The WebAssembly half, `AddFormaUIWebAssembly` | [WebAssembly design](webassembly-design.md) | Implemented in part: the components load and render under WebAssembly and Auto; [steps 9 to 12](#webassembly) open | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Trimming | [The client's build](webassembly-design.md#the-clients-build) | Every client library analyzed for trimming; declaring them trimmable is an [open decision](#open-decisions) | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Hybrid | [WebAssembly and Hybrid](culture-design.md#webassembly-and-hybrid) | [Deferred](#deferred) | — |

### Build and verification

| Feature | Contract | State | Since |
| --- | --- | --- | --- |
| The gate and CI on Linux and Windows | [Build and verification](repository-structure.md#build-and-verification) | Implemented; the locale lint awaits package access in CI | [September 2026](../FEATURE-HISTORY.md#2026-09) |
| The browser check | [Tests](repository-structure.md#tests) | Implemented | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| The screen reader check | [Screen reader checks design](screen-reader-checks-design.md) | Implemented in part; [the data view's scenarios](#screen-reader-checks) remain | [October 2026](../FEATURE-HISTORY.md#2026-10) |
| Releases | [Build and verification](repository-structure.md#build-and-verification) | Implemented in part; [the rest](#release) planned | [September 2026](../FEATURE-HISTORY.md#2026-09) |

<a id="webassembly"></a>

## WebAssembly

[The WebAssembly plan](webassembly-implementation-plan.md) builds [the WebAssembly design](webassembly-design.md) and the parts of [the culture design](culture-design.md) a page holding several runtimes needs. Steps 1 to 5, 7 and 8 have landed: the client libraries reference packages rather than the shared framework, the product has its two render-mode halves, the browser check runs the components in every runtime, the culture's checks and the navigation boundary are automated, the culture state switches through `ICultureSwitch`, and a page's circuits share one coordinator. Step 6 landed the trim analyzer on every client library and left the libraries' trimming [open](#open-decisions). What remains:

- **9, routing in coordination:** the culture routes move into a neutral `Forma.Framework.Culture.Router`, name a pair and are published to the page; the interactive landing joins the coordinator; and a pair without a route is refused before anything changes.
- **10, the culture under WebAssembly:** the culture's WebAssembly half registers each runtime with the coordinator and sets the process's default cultures, and the culture arrangement, coordinated or independent, is chosen on both registrars.
- **11, a host's strings in the browser:** a host's locale files embedded in its client assembly, where the browser's loader finds them.
- **12, the host's setup:** both arrangements set out in the READMEs, each an example host the gate builds and runs.

Four of the plan's checks are known failures today and pass once step 10 lands: an event after a switch under WebAssembly, the document following it, WebAssembly starting in the server's pair, and a Server island's switch reaching a WebAssembly island.

<a id="components-to-build"></a>

## Components to build

`FormaVirtualize` is built and awaits approval, after which it moves into the catalog. The data view, with its views and controls, is finished once two steps of [its plan](data-view-implementation-plan.md) land: the explorer's content moving onto it, and a tree's tab stop returning to the node last focused. Its third open step, the gallery's browsing on it, is built with the gallery and stays open until the gallery starts. [The roadmap](component-roadmap.md) orders what follows: the category selector; the notes editor, export button, design picker and font picker; the permission matrix, product options editor and filter editor; the remaining icon buttons; the context menu; the media picker; the launch bar; the gallery; and the image editor.

<a id="the-dom-watcher-and-engine"></a>

## The DOM watcher and engine

[Its plan](dom-watcher-engine-implementation-plan.md) has landed the component context's disposal, the watcher and engine, intersection and focus, steps 0 to 4. Steps 5 to 11 move the rest of the browser work onto them: DOMQL as the language a component's queries are written in, the input router's participation, and each remaining behavior, operation and component script, retiring the kind folders as each empties.

DOMQL's language now lives in Nexo, which was ported out of DOMQL and has changed much since, and DOMQL is brought up to it in its own repository. Until it is, DOMQL's specification and design describe the language as it stood at the port, and so do the queries [the DOM watcher and engine design](dom-watcher-engine-design.md) writes as examples, which change with it.

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

`publish.ps1` releases a version: it writes the version, packs the solution in Release, tags the commit with a signed annotated tag, and pushes the packages, refusing a version a feed already holds. What remains:

- Run the gate before the tag is written.
- Check that each package declares and bundles what it should.
- Publish from the workflow, which refuses any tag of another form.
- Narrow CI's exclusion of prose to what no package carries, since a project's README and notices are packed with it.
- Prove, against published packages, that a consumer outside the repository taking a locale pack from a feed loads it, and that a project outside the repository declaring `IsLocalePack` packs as a pack.
- Give the repository access to the package feed CI restores `locali` from.

<a id="open-decisions"></a>

## Open decisions

Each needs the maintainer's decision; each is given with a proposal.

- **Trimming Forma's libraries.** Declaring them `IsTrimmable` broke the select's list under WebAssembly: the linker strips the members of the objects Forma passes to its scripts, which only the framework's reflection reads, and the trim analyzer can't see it. *Proposal:* every interop payload becomes a record in its assembly's source-generated context, the references it carries travel as arguments of their own, a component analyzer refuses any other argument to Forma's interop bases, and the libraries then declare `IsTrimmable`. It changes every behavior's script contract, so the approach is first proven on the failing select, its callbacks and object references included, in a trimmed publish under the browser check, before the change reaches the rest.
- **The document's base under culture addresses.** `FormaPageHead` writes the page's `<base>`, so a host loading each culture as a new document builds the culture's base and hands it in through `BaseHref`, as [the router's README](../src/Forma.Framework/Forma.Framework.Culture.Router.AspNetCore/README.md#the-documents-base) shows, and `CultureBaseHref`, which writes a `<base>` of its own, has no place in a head `FormaPageHead` renders. *Proposal:* `FormaPageHead` takes its base from culture routing where it is registered, as step 9 moves the page base into the neutral router, and `CultureBaseHref` retires.
- **The earlier culture cookie.** The middleware still reads and expires a `forma-culture` cookie, which no release ever wrote. *Proposal:* drop it from the code and the culture design.
- **The bases of host-placed components.** The component foundations design gives each placement one base, a component a host places being an element, while an element is a component with a root its host addresses. `FormaConfirmDialog`, `FormaToastContainer`, `FormaPageHead` and `FormaVirtualize` are placed by a host on the base of a part, though `FormaPageHead` and `FormaVirtualize` draw no root of their own, and `FormaColumnBase` derives from `ComponentBase` directly. `FormaCurrencyInput` and `FormaSearchBox` stand on the element base as the authoring rules set out: the first composes `FormaNumberInput` and forwards its value, expression and validation to it, and the second is a search field no form binds. *Proposal:* the design chooses a base by responsibility and composition rather than by placement, the element base for a component drawing a root its host addresses, and each case is then reviewed on its own against that rule rather than moved mechanically.
- **The culture state on the bases.** Both bases inject the culture state and subscribe to it themselves, while the authoring rule puts what every component needs on its context, built once, never on a base twice. *Proposal:* the context carries the culture state, and both bases read it and subscribe to it through the context.
- **Subscript and superscript.** The composer design keeps them out of the default preset, while its default formatting plugin adds both. *Proposal:* follow the design.
- **Stepping a number from its draft.** A number input steps from its value, and a step replaces an edit in progress. *Proposal:* a step starts from the number the edit's text writes where it reads as one in the culture it was typed in, and from the value where it doesn't, as a native number field steps from what it shows.
- **What the catalog lists.** `FormaCalendar` is listed though only pickers place it, while the date and time boxes the pickers place are not, and the composer's presets, `FormaStandardRichTextEditor` and `FormaCompactRichTextEditor`, are not. *Proposal:* list what a host places, the presets among them, and fold the calendar into the pickers' entries.

<a id="deferred"></a>

## Deferred

Each waits for the trigger named beside it before its design or its implementation resumes.

| Work | Why it waits | Reopened by |
| --- | --- | --- |
| Hybrid | Forma doesn't support its render mode, and whether one state serves every native window or each its own is unchosen | Supporting Hybrid |
| Observing the document's child list | No consumer needs it, and its cost is unmeasured | A consumer with a measured cost |
| Growing the DOM watcher and engine's design beyond its plan | Its plan's steps come first | Those steps landed |
| Profiling select-all and a sort in a virtualized data view | The Showcase has no virtualized scenario offering either over an instant source | Such a scenario |
