# Feature history

Each month of Forma's development, newest first, with the features it added and the changes it made to those already there. Forma was created on June 17, 2025, as part of another project, where its first components were built, and became a project of its own on July 22, 2026. Forma is unreleased: every package carries `1.0.0-preview` and none is published, so a month stands where a version will. Each feature is named by the document that states it now, its entry in [the catalog](docs/component-catalog.md) or [the roadmap](docs/component-roadmap.md), its design under [`docs/`](docs/) or its package's README, and [the features status](docs/features-status.md) says where each stands today.

Until the first release, a month may break what the one before it allowed, and each month lists what it broke.

<a id="2026-10"></a>

## October 2026

*October 1–30 · 73 components, 65 implemented and 8 in progress*

**Components**

- [`FormaNumberInput`](docs/component-catalog.md#formanumberinput): a number typed the way the culture writes it, stepped with the arrow keys, with text beside it and a fixed or bounded number of decimals; a grouping separator is accepted only where it groups digits.
- [`FormaTimePicker`](docs/component-catalog.md#formatimepicker) and [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker): a time typed or chosen from suggested times, over a range that may cross midnight, and a date and a time under one label, as the [time inputs design](docs/time-inputs-design.md) sets out.
- [`FormaCurrencyInput`](docs/component-catalog.md#formacurrencyinput): an amount in a currency named by its ISO code, its symbol where the culture writes it and its decimals the currency's, as the [currency input design](docs/currency-input-design.md) sets out.
- [`FormaCapacityIndicator`](docs/component-catalog.md#formacapacityindicator): how much of a limit is used, nearly full, full or over.
- [`FormaDatePicker`](docs/component-catalog.md#formadatepicker) binds a date that can't be empty, reporting a cleared one as required, and names its trigger by its label and its date.
- [`FormaField`](docs/component-catalog.md#formafield) describes the input it holds by its validation messages, so a reader hears why a value is refused; fields leave their spacing to the layout around them, and [`FormaSearchBox`](docs/component-catalog.md#formasearchbox) takes a label.
- [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) is drawn from the design tokens, its focus ring an outline.
- Inputs bound as they are typed into keep every key typed, and a button with no click handler makes no round trip.
- [`FormaPopover`](docs/component-catalog.md#formapopover) follows a change of reading direction while it is open, [`FormaTabs`](docs/component-catalog.md#formatabs) draws its strip again when a tab's header changes, and [`FormaFileViewer`](docs/component-catalog.md#formafileviewer) resolves its document again when the culture changes.
- Every component [follows the culture state](docs/culture-design.md#components-follow-the-state): its strings, its formats and its direction follow a switch in place, and it [writes its output in the state's cultures](docs/culture-design.md#output-takes-its-culture-from-the-state), whatever culture the flow that renders it holds.
- [An edit in progress keeps the culture it was typed in](docs/culture-design.md#an-edit-in-progress-keeps-its-culture), so `1,234` typed in English commits as a thousand and more after a switch to German.

**Products**

- [The data view](docs/data-view-design.md)'s grid, list, cards and tree draw through [`FormaVirtualize`](docs/virtualization-design.md), each redrawing only what changed, and the arrow keys move among the checkboxes under checked selection.
- The grid edits numbers, times and dates with times through the library's own inputs.
- The data view, [`FormaExplorer`](docs/component-catalog.md#formaexplorer) and [`FormaComposer`](docs/component-catalog.md#formacomposer) write dates and compare text in the culture state's cultures.

**Foundations**

- [The culture state](docs/culture-design.md#one-culture-state-per-user-scope): one per user scope, a circuit or a WebAssembly runtime, holding the culture that formats and the culture that chooses strings apart, and announcing each change.
- [One switch for the page](docs/culture-design.md#one-switch-for-the-page): a host switches through `ICultureSwitch`, and on an Interactive Server page [the coordinator](docs/culture-design.md#the-coordinator) accepts each switch for every circuit registered with it, persists it in the culture cookie where the host chose that, and keeps the [document's language and direction](docs/culture-design.md#the-document-follows-the-state) on the pair it accepted.
- [Culture addresses](docs/culture-design.md#culture-addresses): a culture in the address, switched with a new load or in place, the state following the address a navigation lands at.
- [Locales bound to cultures](src/Forma.Framework/Forma.Framework.Localization/README.md#cultures), so a component's strings follow the state rather than the ambient culture.
- [Measurements](src/Forma.Framework/Forma.Framework.Resources/README.md#formatting-measurements), sizes, durations and rates, written in a culture.
- [The DOM watcher and engine](docs/dom-watcher-engine-design.md): what a component observes in the browser and acts on there goes through one watcher and one engine, its focus and its scrolling among it.
- [Currencies](src/Forma.Framework/Forma.Framework.Currencies/README.md): the ISO 4217 catalog, with each currency's decimals and each culture's symbol, from CLDR.
- [Debouncing](src/Forma.Framework/Forma.Framework.Events/README.md) joins the events assembly.

**Hosting**

- [The product's render-mode halves](docs/webassembly-design.md#registration): [`Forma.Blazor.Server`](src/Forma.Blazor.Server/README.md), with `AddFormaUIServer` and [`FormaPageHead`](docs/component-catalog.md#formapagehead), and [`Forma.Blazor.WebAssembly`](src/Forma.Blazor.WebAssembly/README.md), with `AddFormaUIWebAssembly`, beside `AddFormaUI`, which registers what every render mode needs.
- [Every client library](docs/webassembly-design.md#what-a-client-loads) references the packages it compiles against rather than the shared framework, and [builds with the trim analyzer on](docs/webassembly-design.md#the-clients-build).

**Build and verification**

- [The browser check](docs/repository-structure.md#tests): the components driven headless in a real browser under Server, WebAssembly and Auto, the culture's behavior and each router's navigation checked, and each client's download held to a budget.
- [The screen reader check](docs/screen-reader-checks-design.md): NVDA driven through the Showcase, recording what it speaks.
- The gate shows each test project as it finishes, can load the machine while a test repeats and list the waits that needed time, and fails a component that reads the ambient culture.
- Every Framework assembly is a package of its own.

**Breaking:**

- Packages are named `formaui-net.X`, where they were named `FormaUI.NET-X`.
- The culture state is read-only: a host switches through `ICultureSwitch`, where it called `ICultureState.ChangeAsync`.
- The coordinator replaces the document culture synchronizer, and `ICultureRouteHandler` is retired.
- The culture's assemblies are `Forma.Framework.Culture`, `.Culture.Browser.Interop`, `.Culture.Blazor.Server` and `.Culture.Router.AspNetCore`, and their registrars `AddFormaCultureServices`, `AddFormaCultureCoordination`, `AddFormaCultureServer` and `AddFormaCultureRouting`.
- `FormaPageHead` is in `Forma.Blazor.Server`, and a server host registers with `AddFormaUIServer`.
- `Forma.Framework.Timing` is gone; debouncing is in `Forma.Framework.Events`.
- `SentinelObserver` and the scroll behaviors are gone; the DOM watcher and engine serve them.
- The composer's page designer types are gone.
- A field carries no bottom margin.
- The culture cookie is `forma-culture-route`.
- A component reads its culture state as `Context.CultureState`, where both bases gave it as `CultureState`.

<a id="2026-09"></a>

## September 2026

*September 1–30 · 68 components*

**Components**

- [`FormaPopover`](docs/component-catalog.md#formapopover): an anchored panel, opening toward the wider side and dismissed through the input router, as the [popover design](docs/popover-design.md) sets out.
- [`FormaDialog`](docs/component-catalog.md#formadialog): the modal, the input router's modal scope, opening on its first field.
- [`FormaWindow`](docs/component-catalog.md#formawindow), [`FormaFrame`](docs/component-catalog.md#formaframe), [`FormaChrome`](docs/component-catalog.md#formachrome) and [`FormaRestArea`](docs/component-catalog.md#formarestarea): managed windows, moved, resized, minimized, locked and stacked, as the [window design](docs/window-design.md) sets out.
- [`FormaBackdrop`](docs/component-catalog.md#formabackdrop) and [`FormaStackLayout`](docs/component-catalog.md#formastacklayout): a page composed of regions laid on a canvas.
- [`FormaFreezePane`](docs/component-catalog.md#formafreezepane): a region frozen beside one that scrolls, resized at its edge.
- [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker) and [`FormaCalendar`](docs/component-catalog.md#formacalendar): a range of dates chosen over two months.
- [`FormaInputScope`](docs/component-catalog.md#formainputscope) and [`FormaInputInspector`](docs/component-catalog.md#formainputinspector): a region the input router routes to, and what it routes, shown live.
- [`FormaVirtualize`](docs/component-roadmap.md#formavirtualize): a long list drawn as the run around the viewport, as the [virtualization design](docs/virtualization-design.md) sets out.
- [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) is a form input; [`FormaField`](docs/component-catalog.md#formafield) shows the messages of the input it holds.
- [`FormaToolbar`](docs/component-catalog.md#formatoolbar) routes its shortcuts through the input router and moves among its items with the arrow keys, as the [toolbar design](docs/toolbar-design.md) sets out.
- [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu) takes labeled sections and skips disabled items, [`FormaShareMenu`](docs/component-catalog.md#formasharemenu) shares to LinkedIn, Messenger and Mastodon and connects accounts, as the [share design](docs/share-design.md) sets out, and [`FormaBreadcrumb`](docs/component-catalog.md#formabreadcrumb) opens its segments' menus as drop-down menus.
- Glyphs mirror for a right-to-left reading direction, and a host can focus [`FormaSortableList`](docs/component-catalog.md#formasortablelist).
- [The component bases](docs/component-foundations-design.md#the-bases): every component derives from one of three bases, takes [one component context](docs/component-foundations-design.md#the-services) and logs what it absorbs.
- The design tokens are rebuilt around [the backdrops and the color roles](README.md#styling-and-theming), each role with a theme contract.

**Products**

- [The data view](docs/data-view-design.md), a product of its own, `Forma.Blazor.DataView`: one engine managing a set's selection, sort, paging, grouping, address and states, presented as a grid, a list, cards or a tree, virtualized over a set or a provider's slices, with a sort menu, a view switch, inline editing, columns fitting the width and its loads announced. Its grid replaces the one built on QuickGrid.
- [`FormaExplorer`](docs/explorer-design.md): one item model where every item can be disabled or hidden, a details view whose leading columns are frozen and whose columns the reader chooses, grouping, a header that collapses at a phone's width, and surfaces composed of backdrops; it ships as a package of its own, its items model becoming [`Forma.Framework.Catalog`](src/Forma.Framework/Forma.Framework.Catalog/README.md).
- [`FormaComposer`](docs/composer-design.md): a host describes a composer once, plugins are scoped to it and bring their own strings and dialogs, its contracts ship as `Forma.Framework.Composer.Extensibility`, browsing stored files becomes the add-on [`Forma.AddOns.Composer.Plugins.Explorer`](src/Forma.AddOns/Forma.AddOns.Composer.Plugins.Explorer/README.md), and it gains a shortcuts reference, gutter markers and sanitized bound content.

**Foundations**

- [The input router](docs/input-router-design.md): every key, press, click, wheel turn and drop a component acts on is a route on one router, so which of two nested components owns an input is decided in one place, and every component's input moved onto it.
- [The window manager](docs/window-design.md#the-manager): the workspace one manager coordinates.
- [The JavaScript interop](src/Forma.Framework/Forma.Framework.JavaScript.Interop/README.md): an assembly of its own, and a JS environment that carries the calling component's log and identity, naming the stage a failure stopped at.
- [Localization](docs/localization-design.md): a host adds or rewords the library's strings, one file per category with a shared section, files read by path from `.locales` folders, and the library's languages in [locale packs](docs/localization-design.md#locale-packs).
- [Plugins](src/Forma.Framework/Forma.Framework.Plugins/README.md): one registry, which the composer's plugins are built from.
- [Sanitization](src/Forma.Framework/Forma.Framework.Sanitization/README.md) and the emoji, icon, feedback, files and media contracts each become a Framework assembly.

**Build and verification**

- Every warning is a build error, and the Framework assemblies and add-ons ship as packages with their READMEs and third-party notices, at one central version.
- [The gate](docs/repository-structure.md#build-and-verification): one `build.ps1`, run by CI on Linux and Windows and by a weekly canary, building the task programs and running the locale lint.
- [Releases](docs/repository-structure.md#build-and-verification): `publish.ps1` tags a version and publishes its packages.
- Forma is licensed under Apache 2.0.

**Breaking:**

- Localization's holes are positional alone; `RootNamespace` metadata and `ResourcesPath` are retired; `.locale.json` sits at an assembly's root; Hebrew and Spanish need their locale packs.
- Options travel in records named without the `Forma` prefix; members read `Get…` where they read `…Of`; "window" names a window alone, so a range of positions is a slice or a rendered range.
- `FormaModal` is `FormaDialog`, `FormaPopup` is `FormaPopover`, the first `FormaFrame` is `FormaBackdrop`, docking is embedding, and the `Design` category is `Symbols`.
- `FormaCommandArea` and `FormaCommandBar` are in `Layout`, and `FormaWindow` in `Windows`.
- The composer's engine and the explorer's model, and the feedback, files, media and input contracts, are Framework assemblies; the interop assemblies are `Forma.Framework.JavaScript.Interop` and `Forma.Framework.Browser.Interop`, their bases `JSModule` and `JSInterop` over `IJSEnvironment`.
- The explorer's host boundary is its items; the list view is folded into details; the catalog's types are in `Forma.Framework.Catalog`.
- The composer's contracts are `Forma.Framework.Composer.Extensibility`, its plugin discovery is gone, and the explorer dialog is an add-on.
- The toolbar's items are `ToolbarItem` and its family, without the `Forma` prefix.
- The QuickGrid `FormaDataGrid` is gone from `Forma.Blazor`; the grid is the data view's.
- `KeyBindings` and `NavigationGuard` are retired.
- Packages were named `FormaUI.NET-X`, and the license is Apache 2.0, where it was MIT.

<a id="2026-08"></a>

## August 2026

*August 1–31 · 49 components*

**Components**

- [`FormaButton`](docs/component-catalog.md#formabutton), which every component's buttons consolidate on, [`FormaCloseButton`](docs/component-catalog.md#formaclosebutton), [`FormaEyeButton`](docs/component-catalog.md#formaeyebutton) and [`FormaToggleButton`](docs/component-catalog.md#formatogglebutton).
- [`FormaTextBox`](docs/component-catalog.md#formatextbox), [`FormaSearchBox`](docs/component-catalog.md#formasearchbox), [`FormaTextArea`](docs/component-catalog.md#formatextarea), [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox), [`FormaSelect`](docs/component-catalog.md#formaselect), [`FormaMultiSelect`](docs/component-catalog.md#formamultiselect), [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) and [`FormaSlider`](docs/component-catalog.md#formaslider), each in [`FormaField`](docs/component-catalog.md#formafield)'s field chrome, and [`FormaVisibility`](docs/component-catalog.md#formavisibility).
- [`FormaSortableList`](docs/component-catalog.md#formasortablelist): a list reordered by dragging a whole row.
- [`FormaToolbar`](docs/component-catalog.md#formatoolbar), its items described as data, as the [toolbar design](docs/toolbar-design.md) sets out, with [`FormaSelectionToolbar`](docs/component-catalog.md#formaselectiontoolbar), [`FormaCommandArea` and `FormaCommandBar`](docs/component-catalog.md#formacommandarea--formacommandbar) and [`FormaStatusStrip`](docs/component-catalog.md#formastatusstrip).
- [`FormaFileViewer`](docs/component-catalog.md#formafileviewer), [`FormaFilePicker`](docs/component-catalog.md#formafilepicker), [`FormaFileUploader`](docs/component-catalog.md#formafileuploader) and [`FormaImagePicker`](docs/component-catalog.md#formaimagepicker), over [resources](docs/resources-design.md) any provider resolves.
- [`FormaVideoPlayer`](docs/component-catalog.md#formavideoplayer): video loaded when it is played, through a player a host can provide, as the [media design](docs/media-design.md) sets out.
- [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu), [`FormaOverflowMenu`](docs/component-catalog.md#formaoverflowmenu) and [`FormaShareMenu`](docs/component-catalog.md#formasharemenu), the last over the [share contracts](docs/share-design.md).
- [`FormaBreadcrumb`](docs/component-catalog.md#formabreadcrumb), and [`FormaPagination`](docs/component-catalog.md#formapagination) redesigned, stepping with the wheel.
- [`FormaSplitter`](docs/component-catalog.md#formasplitter), [`FormaSpinner`](docs/component-catalog.md#formaspinner) and [`FormaPageHead`](docs/component-catalog.md#formapagehead), the document head a host needs.
- [`FormaIcon`](docs/component-catalog.md#formaicon), [`FormaEmoji`](docs/component-catalog.md#formaemoji) and [`FormaEmojiPicker`](docs/component-catalog.md#formaemojipicker): [icons](README.md#icons) from Forma's own glyph set or a host's provider, and [emoji](README.md#emoji) from a swappable catalog.
- No state is shown through color alone, every target is usable with a finger, and the library composes its own components.
- One scale of spacing, type and radii, and the stylesheets split into [tokens, reset, base and input](README.md#styling-and-theming).

**Products**

- [`FormaExplorer`](docs/explorer-design.md), introduced as `FormaGallery`: a provider's folders and files, browsed with infinite scroll, a new folder made from a folder's own row, and every action in an overflow menu.
- [`FormaComposer`](docs/composer-design.md): its capability model, sanitizer and history, an engine shell reporting the caret and the selection, and a rich text editor with links, images, video, tables, emoji, templates, paste, a direction per block, one selection bar and read-only preview and source views, written without `execCommand`.

**Foundations**

- [Localization](docs/localization-design.md): JSON in place of resx, behind Forma's own contract, with [CLDR plural rules](src/Forma.Framework/Forma.Framework.Localization/README.md#plural-forms) in a bracket syntax and a schema on every file, the JSON backend a package of its own.
- [Resources](src/Forma.Framework/Forma.Framework.Resources/README.md): a chain of providers resolving a source into what a viewer shows, with [Office](src/Forma.AddOns/Forma.AddOns.Resources.Office/README.md) and [Markdown](src/Forma.AddOns/Forma.AddOns.Resources.Markdown/README.md) conversion as add-ons.
- [Icons](src/Forma.Framework/Forma.Framework.Icons/README.md): `IIconProvider`, Forma's own glyph set on two grids, as the [glyph artwork specification](docs/glyph-artwork-specification.md) states, and brand marks.
- [Emojis](src/Forma.Framework/Forma.Framework.Emojis/README.md): contracts and a swappable provider and catalog, with Twemoji's artwork the add-on [`Forma.AddOns.Emojis.Twemoji`](src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/README.md).
- [Typed CSS lengths](src/Forma.Framework/Forma.Framework.Css/README.md), and [behaviors and operations](src/Forma.Framework/Forma.Framework.Browser.Interop/README.md) shared by the components' scripts.
- One registration call for a host, and the solution in its [three families](docs/repository-structure.md#solution-structure), Framework, AddOns and the product.

**Build and verification**

- The `.editorconfig` style rules are enforced in the build, and a shared support project serves the tests.

**Breaking:**

- The `Admin` namespace is gone; components are named `FormaMultiSelect`, `FormaPasswordBox`, `FormaCheckBox` and `FormaSlider`, and the categories `Buttons` and `Inputs`.
- `FormaPageHeader` is `FormaPageTitle`, the context toolbar is `FormaSelectionToolbar`, the first toolbar is `FormaSearchToolbar`, and `FormaGlyph` is `FormaIcon`.
- Color tokens end in `-color`, and the stylesheets are split.
- Localization reads JSON, not resx, its plurals are written in brackets, and its JSON backend is a package of its own.
- `FormaField`'s `FieldId` is `TargetId`, and `FormaInput` is gone.
- The resource kinds are types, where they were an enum.
- The assemblies are `Forma.Blazor`, the Framework assemblies and the add-ons.

<a id="2026-07"></a>

## July 2026

*July 22–30 · 13 components*

Forma becomes a project of its own, named Forma.

**Components**

- [`FormaStatusBadge`](docs/component-catalog.md#formastatusbadge), [`FormaPagination`](docs/component-catalog.md#formapagination) and a data grid over QuickGrid.
- A field with a floating label, whose chrome [`FormaField`](docs/component-catalog.md#formafield) carries on.

**Foundations**

- [Typed CSS colors](src/Forma.Framework/Forma.Framework.Css/README.md), and the [feedback services](src/Forma.Framework/Forma.Framework.Feedback/README.md)' contracts apart from their implementations.
- Spanish strings beside English and Hebrew.

**Build and verification**

- .NET 11.

**Breaking:**

- The library is `Forma.Blazor`, its components `Forma…`, where it was Site.UI.
- The modal and `FormaPagination` take options records in place of flags.

<a id="2026-03"></a>

## March 2026

*March 3–12*

**Components**

- [`FormaDialog`](docs/component-catalog.md#formadialog), first a modal, [`FormaConfirmDialog`](docs/component-catalog.md#formaconfirmdialog) and [`FormaToastContainer`](docs/component-catalog.md#formatoastcontainer), over notification-driven feedback services.
- [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker), [`FormaDatePicker`](docs/component-catalog.md#formadatepicker), with a calendar of its own, and [`FormaToggleSwitch`](docs/component-catalog.md#formatoggleswitch).
- [`FormaTabs`](docs/component-catalog.md#formatabs), [`FormaPageTitle`](docs/component-catalog.md#formapagetitle), first a page header with its breadcrumb, and [`FormaSearchToolbar`](docs/component-catalog.md#formasearchtoolbar), first a toolbar with a debounced search.

**Foundations**

- The [culture in the address](README.md#culture-in-the-url).
- English and Hebrew strings.

**Build and verification**

- The Showcase and the component tests.

<a id="2025-06"></a>

## June 2025

*June 17*

Forma is created, as part of another project.
