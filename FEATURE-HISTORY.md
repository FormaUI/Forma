# Feature history

Each month of Forma's development, newest first, with the features it added and the changes it made to those already there. Forma was created on June 17, 2025, as part of another project, where its first components were built, and became a project of its own on July 22, 2026. Forma is unreleased: every package carries `1.0.0-preview` and none is published, so a month stands where a version will. Each feature is named by the document that states it now, its entry in [the catalog](docs/component-catalog.md) or [the roadmap](docs/component-roadmap.md), its design under [`docs/`](docs/) or its package's README, and [the features status](docs/features-status.md) says where each stands today.

Until the first release, a month may break what the one before it allowed, and each month lists what it broke.

<a id="2026-11"></a>

## November 2026

*November 1–30 · 74 components, 66 implemented and 8 in progress*

**Components**

- [`FormaPopover`](docs/component-catalog.md#formapopover) opens on its next render where its script failed to open it, rather than staying hidden while it reads as open.
- [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu) focuses an item only while it is open, so a click that closes it as it opens no longer ends a Server circuit.
- Every component renders its state under a static render and offers no control whose action needs interactivity: the calendar's days in [`FormaDatePicker`](docs/component-catalog.md#formadatepicker), [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker) and [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker), [`FormaTabs`](docs/component-catalog.md#formatabs), [`FormaPagination`](docs/component-catalog.md#formapagination)'s current step, the triggers of [`FormaDatePicker`](docs/component-catalog.md#formadatepicker), [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker), [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker), [`FormaSelect`](docs/component-catalog.md#formaselect) and [`FormaMultiSelect`](docs/component-catalog.md#formamultiselect), [`FormaEmojiPicker`](docs/component-catalog.md#formaemojipicker)'s tabs and emojis, [`FormaSortableList`](docs/component-catalog.md#formasortablelist)'s handles, [`FormaVideoPlayer`](docs/component-catalog.md#formavideoplayer)'s play control, [`FormaExplorer`](docs/component-catalog.md#formaexplorer)'s folders, items, selection marks and group headings, [`FormaDataTree`](docs/data-view-design.md#the-tree)'s nodes and a [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) with a click action render disabled until the page is interactive, as a button with a click handler does, each keeping the look that carries its state.
- Under a static render [`FormaDataTree`](docs/data-view-design.md#the-tree) shows its roots, [`FormaEmojiPicker`](docs/component-catalog.md#formaemojipicker) a category's every emoji and [`FormaEmoji`](docs/component-catalog.md#formaemoji) its artwork, each of which had waited on a script.
- A pressed [`FormaToggleButton`](docs/component-catalog.md#formatogglebutton) keeps its fill while disabled, so whether it is on still shows.
- [`FormaComposer`](docs/component-catalog.md#formacomposer) offers subscript and superscript where `BasicFormattingPluginOptions` includes them, and leaves them out of its default formatting, as [its design](docs/composer-design.md#12-full-parity-nothing-dropped) keeps them out of the default preset.
- [`FormaField`](docs/component-catalog.md#formafield) keeps one message line's room beneath an input whose form's messages it shows, so a message arriving or leaving never moves what follows, and a press on a submit button that takes the focus from the input submits the first time.
- [`FormaNumberInput`](docs/component-catalog.md#formanumberinput) steps from the number its edit in progress writes, read in the culture it was typed in, where it had stepped from the value it last took, so a number typed past a limit steps back within it.
- [`FormaConfirmDialog`](docs/component-catalog.md#formaconfirmdialog) and [`FormaToastContainer`](docs/component-catalog.md#formatoastcontainer) take a host's id, class, style and attributes, the confirm dialog's onto its dialog.
- [`FormaTextArea`](docs/component-catalog.md#formatextarea) takes its value as each character is typed or, with `UpdateOn` set to `TextUpdate.Change`, as its edit commits, its character count still following each keystroke in the browser, so a long text never travels with every keystroke; [`FormaTextBox`](docs/component-catalog.md#formatextbox) takes the same choice in its options, and [`FormaComposer`](docs/component-catalog.md#formacomposer)'s plain text editor in the composer's `PlainTextUpdateOn`.

**Foundations**

- [The JavaScript interop](src/Forma.Framework/Forma.Framework.JavaScript.Interop/README.md) absorbs a call the runtime canceled, as it cancels every call of a circuit that ends, and logs it at `Debug` as it logs a dropped circuit, where the cancellation escaped a component's disposal as an unhandled exception in its circuit.

- [Registration in either order](docs/component-foundations-design.md#the-null-objects): Forma registers no stand-in for what the platform or a package supplies, so a host registers Forma beside its platform's services, its logging, its localization and an artwork pack in any order, and the components fall back to the source text, the reader's font, the bundled glyphs and a runtime that throws at its first call where none is registered.

- [The WebView half](src/Forma.Blazor.WebView/README.md), `AddFormaUIWebView`, and [the culture under Blazor Hybrid](docs/culture-design.md#blazor-hybrid): one culture across an application's web views, switched from any web view or from native code, applied to the state, the process's defaults and every web view's UI thread, each web view's document taking its language and direction and, under culture routing, its address moving into the switched culture's route, which a navigation handler tells by `IsCultureFollowing`; [the README](README.md#blazor-hybrid) sets up a WPF, a Windows Forms or a MAUI application on Windows, refreshing its native user interface and keeping its culture from one run to the next.

- [Native operations](docs/webview-design.md#native-operations): a host whose web view can't save a file, print or share does each natively through `INativeOperations`, which `FormaShareMenu` and `FormaComposer`'s preview and source views take where it is registered; printing hands the reader the document, written with the page's styles, as a file; and [`Forma.AddOns.Operations.Maui`](src/Forma.AddOns/Forma.AddOns.Operations.Maui/README.md) does it through the platform's share sheet in a MAUI application on Android, iOS and Mac Catalyst, which the MAUI example host now builds for.

- [A culture per web view](docs/culture-design.md#a-culture-per-web-view): a Blazor Hybrid application declaring `CultureArrangement.Independent` gives each web view a culture state and a switch of its own, its document and its address following it alone, and a web view opening in the culture its start path names.

- [Culture routing](src/Forma.Framework/Forma.Framework.Culture.Router/README.md) registers only its own services, so `AddFormaCultureRouting` goes before or after a product's registrar; registered first, it had left a WebAssembly runtime's culture state scoped.

- The state notifier takes subscriptions from any thread and notifies every subscriber where one fails, where it had stopped at the first failure.

- [The input router](docs/input-router-design.md#a-fields-pending-edit) commits the focused text field's pending edit before it routes an action or a dismissal, so a shortcut pressed in a field that updates on change reads the text as it stands; [the DOM engine](src/Forma.Framework/Forma.Framework.Browser.Interop/README.md) keeps a text field's running count of its characters in the browser, as `Text.AttachAsync`.

- [A host's locale files](src/Forma.Framework/Forma.Framework.Localization/README.md) are embedded in a Blazor Hybrid application's assembly, as in a WebAssembly client's, so an application MAUI packages reads them with no folder beside it.

- [The JavaScript interop](src/Forma.Framework/Forma.Framework.JavaScript.Interop/README.md), the input registration and the culture registration leave a call into script still under way to finish as they're disposed, releasing what it produced then, where they had waited for it, so a component disposed where its runtime will never answer, as a web view whose document is gone, completes its disposal.

**Build and verification**

- [The WebView check host](docs/repository-structure.md#tests): a WPF application whose native windows each render the components to a web view, driven by the browser check through WebView2, checking what Blazor Hybrid guarantees of its renderer, a web view's registration, the culture each web view's events run in, a web view's address replaced and refused, a traversal, and a web view's release.

- Every component's checks run in Blazor Hybrid's web view too, as the `webview` runtime, each check page in a native window of the WebView check host, from [the check pages' own library](docs/repository-structure.md#tests) the components host shares.

- The static check: every check page renders statically too, as the `static` runtime, and is held to show its state, raise no error, show nothing waiting on a script and offer no enabled control whose action needs interactivity.

- The render mode inventory: every component the catalog lists has a check page, and every check page is opened by a check that runs in every interactive runtime and rendered by the static check; [`FormaField`](docs/component-catalog.md#formafield), [`FormaVisibility`](docs/component-catalog.md#formavisibility) and `FormaDataSortMenu` gained check pages.

- [The Blazor Hybrid example hosts](tests/Forma.Examples/README.md): a WPF, a Windows Forms and a MAUI application on Windows, built from Forma's packages as [the README](README.md#blazor-hybrid) sets an application up and driven through WebView2, each checked for its published assets, a switch across two web views and its native windows' titles, a closed native window's release and the culture it keeps from one run to the next; the README's setup is held to them.

**Breaking:**

- `AddFormaUI` registers no `IJSRuntime`, `IStringLocaleFactory`, `IIconProvider` or `IEmojiProvider` of its own: a host that resolves one from the container registers it, as `AddJsonLocalization`, `AddFormaGlyphServices` and `AddFormaEmojiServices` do, and its logging is the platform's `LoggerFactory`, with the host's providers, where it was a null factory.
- `AddFormaCultureRouting` registers no culture state or coordination: a host registers them through a product's registrar, as every host already does.
- `StateNotifier.NotifyAsync` faults with an `AggregateException` holding every subscriber's failure, where it faulted with the first one's.
- `BasicFormattingPlugin` offers no subscript or superscript, their toolbar toggles, shortcuts or commands, unless `BasicFormattingPluginOptions.IncludeSubscriptAndSuperscript` is set.
- `FormaColumnBase` derives from `FormaComponentBase` and carries no `CultureState` of its own; a column reads its cultures through `Context.CultureState`.
- `Print.PrintPageAsync` takes the title the page goes by as a file.

<a id="2026-10"></a>

## October 2026

*October 1–31 · 73 components, 65 implemented and 8 in progress*

**Components**

- [`FormaNumberInput`](docs/component-catalog.md#formanumberinput): a number typed the way the culture writes it, stepped with the arrow keys, with text beside it and a fixed or bounded number of decimals; a grouping separator is accepted only where it groups digits.
- [`FormaTimePicker`](docs/component-catalog.md#formatimepicker) and [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker): a time typed or chosen from suggested times, over a range that may cross midnight, and a date and a time under one label, as the [time inputs design](docs/time-inputs-design.md) sets out.
- [`FormaCurrencyInput`](docs/component-catalog.md#formacurrencyinput): an amount in a currency named by its ISO code, its symbol where the culture writes it and its decimals the currency's, as the [currency input design](docs/currency-input-design.md) sets out.
- [`FormaCapacityIndicator`](docs/component-catalog.md#formacapacityindicator): how much of a limit is used, nearly full, full or over.
- [`FormaDatePicker`](docs/component-catalog.md#formadatepicker) binds a date that can't be empty, reporting a cleared one as required, and names its trigger by its label and its date.
- [`FormaField`](docs/component-catalog.md#formafield) describes the input it holds by its validation messages, so a reader hears why a value is refused; fields leave their spacing to the layout around them, and [`FormaSearchBox`](docs/component-catalog.md#formasearchbox) takes a label.
- [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) is drawn from the design tokens, its focus ring an outline.
- Inputs bound as they are typed into keep every key typed, and a button with no click handler makes no round trip.
- [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu) moves through its items in the order the page holds them, a group's, those added later and those the page reorders among them.
- [`FormaSplitter`](docs/component-catalog.md#formasplitter) follows a drag in the browser, ending it wherever the pointer is let go, and Escape returns the size the drag started from.
- [`FormaImagePicker`](docs/component-catalog.md#formaimagepicker) previews an image chosen before its script attached, and attaches the preview again on a later render after an attach failed.
- Every component that attaches a script claims the attach before awaiting it, so a render arriving meanwhile attaches nothing a second time, and [`FormaVirtualize`](docs/component-catalog.md#formavirtualize)'s list, as a time picker's, opens where it should on a slow connection.
- [`FormaIcon`](docs/component-catalog.md#formaicon) is decorative unless its `Label` names it, a brand mark as every other icon, so a share menu's network, a tab or a toggle button is named once.
- [`FormaDialog`](docs/component-catalog.md#formadialog) is named by its title.
- [`FormaTextBox`](docs/component-catalog.md#formatextbox) and [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox) hold their label above a value the browser fills.
- [`FormaFileViewer`](docs/component-catalog.md#formafileviewer) zooms an image past its frame, and pans across all of it.
- [`FormaTimePicker`](docs/component-catalog.md#formatimepicker)'s list and [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker)'s open on the time held, whatever was chosen before.
- An open surface that closes gives up its layer among the input router's.
- [`FormaPopover`](docs/component-catalog.md#formapopover) follows a change of reading direction while it is open, [`FormaTabs`](docs/component-catalog.md#formatabs) draws its strip again when a tab's header changes, and [`FormaFileViewer`](docs/component-catalog.md#formafileviewer) resolves its document again when the culture changes.
- Every component [follows the culture state](docs/culture-design.md#components-follow-the-state): its strings, its formats and its direction follow a switch in place, and it [writes its output in the state's cultures](docs/culture-design.md#output-takes-its-culture-from-the-state), whatever culture the flow that renders it holds.
- [An edit in progress keeps the culture it was typed in](docs/culture-design.md#an-edit-in-progress-keeps-its-culture), so `1,234` typed in English commits as a thousand and more after a switch to German.

**Products**

- [The data view](docs/data-view-design.md)'s grid, list, cards and tree draw through [`FormaVirtualize`](docs/virtualization-design.md), each redrawing only what changed, and the arrow keys move among the checkboxes under checked selection.
- The grid edits numbers, times and dates with times through the library's own inputs.
- The data view, [`FormaExplorer`](docs/component-catalog.md#formaexplorer) and [`FormaComposer`](docs/component-catalog.md#formacomposer) write dates and compare text in the culture state's cultures.
- [`FormaComposer`](docs/component-catalog.md#formacomposer) keeps what is typed as soon as it is editable, and puts a link, a table and a stored file into the document in a trimmed WebAssembly client.
- [`FormaComposer`](docs/component-catalog.md#formacomposer) applies a block style, lead or line height to a line typed into an empty document.
- [`FormaComposer`](docs/component-catalog.md#formacomposer) reopens a link with the tab it opens in, and an anchor under its own name.
- [`FormaComposer`](docs/component-catalog.md#formacomposer) loads the assets of every plugin in play, a feature's as an editor's, and answers the shortcuts of the view shown from its toolbar.

**Foundations**

- [The culture state](docs/culture-design.md#one-culture-state-per-user-scope): one per user scope, a circuit or a WebAssembly runtime, holding the culture that formats and the culture that chooses strings apart, and announcing each change.
- [One switch for the page](docs/culture-design.md#one-switch-for-the-page): a host switches through `ICultureSwitch`, and on an Interactive Server page [the coordinator](docs/culture-design.md#the-coordinator) accepts each switch for every circuit registered with it, persists it in the culture cookie where the host chose that, and keeps the [document's language and direction](docs/culture-design.md#the-document-follows-the-state) on the pair it accepted.
- [The culture under WebAssembly](docs/webassembly-design.md#registration): each WebAssembly runtime on a page registers with its culture coordinator as it starts, starting in the pair the server rendered and following every switch on the page, its events and timers in the new pair; a host chooses the culture arrangement on both registrars, coordinated or the client's own, and a server that renders WebAssembly components under coordinated pages hands its culture over and refuses to start with that handoff turned off; a standalone app's runtime starts its page's coordinator itself.
- [Culture addresses](docs/culture-design.md#culture-addresses): a culture pair in the address, switched with a new load or in place, the state following the address a navigation lands at, and [routing in coordination](src/Forma.Framework/Forma.Framework.Culture.Router/README.md): a switch is a navigation to its pair's address that the page's coordinator accepts once it lands, an interactive landing included, and `FormaPageHead` takes the document's base from routing.
- [Locales bound to cultures](src/Forma.Framework/Forma.Framework.Localization/README.md#cultures), so a component's strings follow the state rather than the ambient culture.
- [A host's strings in the browser](docs/localization-design.md#host-files): a WebAssembly client's build embeds the host's folder of locale files, its own or its server's, and the client reads them where a host's folder stands.
- [Measurements](src/Forma.Framework/Forma.Framework.Resources/README.md#formatting-measurements), sizes, durations and rates, written in a culture.
- [The DOM watcher and engine](docs/dom-watcher-engine-design.md): what a component observes in the browser and acts on there goes through one watcher and one engine, its focus and its scrolling among it.
- [Currencies](src/Forma.Framework/Forma.Framework.Currencies/README.md): the ISO 4217 catalog, with each currency's decimals and each culture's symbol, from CLDR.
- [Debouncing](src/Forma.Framework/Forma.Framework.Events/README.md) joins the events assembly.
- [An interop wrapper](src/Forma.Framework/Forma.Framework.JavaScript.Interop/README.md) releases what an attach gets once a later attach, a detach or the disposal overtook it, and imports a module again after its import failed.
- [The markup policy](src/Forma.Framework/Forma.Framework.Sanitization/README.md) keeps an address relative to the document, as a catalog writes its files, where it names no scheme.
- A circuit that reconnects is back at once: its culture state registers with the page's coordinator again past the reconnection, which waited a minute for it.

**Hosting**

- [The product's render-mode halves](docs/webassembly-design.md#registration): [`Forma.Blazor.Server`](src/Forma.Blazor.Server/README.md), with `AddFormaUIServer` and [`FormaPageHead`](docs/component-catalog.md#formapagehead), and [`Forma.Blazor.WebAssembly`](src/Forma.Blazor.WebAssembly/README.md), with `AddFormaUIWebAssembly`, beside `AddFormaUI`, which registers what every render mode needs.
- [Every client library](docs/webassembly-design.md#what-a-client-loads) references the packages it compiles against rather than the shared framework, and [is trimmed](docs/webassembly-design.md#the-clients-build) with a WebAssembly client's publish, [Forma's analyzers](eng/Forma.Analyzers/README.md) keeping each type that crosses the interop serializable once trimmed and its generator keeping those types' members with their library.

**Build and verification**

- [The browser check](docs/repository-structure.md#tests): the components driven headless in a real browser under Server, WebAssembly and Auto, the culture's behavior and each router's navigation checked, and each client's download held to a budget.
- [The component checks](docs/repository-structure.md#tests): every call a component makes to a script, and every call a script makes back, driven and asserted in the browser check under Server, WebAssembly and Auto, Auto both on a first visit, on the Server, and once WebAssembly has downloaded.
- [The example hosts](tests/Forma.Examples/README.md): a Web App for each culture arrangement, built from Forma's packages as the README sets a host up and driven by the browser check, which holds the README's setup to them.
- [The screen reader check](docs/screen-reader-checks-design.md): NVDA driven through the Showcase, recording what it speaks.
- The gate shows each test project as it finishes, can load the machine while a test repeats and list the waits that needed time, and fails a component that reads the ambient culture.
- Every Framework assembly is a package of its own.
- The interop analyzer checks a type deriving from one that crosses the JavaScript interop boundary as crossing it too.

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
- `LocalizationOptions.WithRecordedPacks` is `WithRecordedLocales`, and the localization package's targets are `buildTransitive`.
- `AddFormaUI` registers no culture switch: `AddFormaUIServer` and `AddFormaUIWebAssembly` register the switch of their render mode, and a WebAssembly client's culture services are one per runtime.
- Culture routing is `Forma.Framework.Culture.Router`, its ASP.NET Core assembly keeping the middleware: a route names its pair, `UICulture` and an optional `Culture`; `CultureRouter` reads paths as strings; `CultureSwitcher.SwitchTo` is `SwitchToAsync`; `CultureBaseHref` and `Forma.Framework.Culture.Router.Interop` are gone, `FormaPageHead` writing the base routing gives and refusing a `BaseHref` beside it; and the `forma-culture` cookie is no longer read.
- The composer's dialog results, `ResourceInsertion` and selection reports, `TextRange`, and the data view's `ColumnFitReport` and `ColumnWidth` are records with `init` properties rather than positional ones, and `InputRoute`'s `Action` and `Bindings` and `KeyBinding`'s `Key` are `required` `init` properties beside the constructors that set them.
- `Pointer` is gone; a drag captures its pointer in the browser.
- `FormaIcon` hides a brand mark as it hides every icon, and a host names a mark it places alone through `Label`.
- `RichTextEditorPlugin.RequestCommand` takes the text or the number a command takes, where it took any object.
- `Asset.LoadScriptAsync` is gone.

<a id="2026-09"></a>

## September 2026

*September 1–30 · 68 components*

**Components**

- [`FormaPopover`](docs/component-catalog.md#formapopover): an anchored panel, opening toward the wider side and dismissed through the input router, as the [popover design](docs/popover-design.md) sets out.
- [`FormaDialog`](docs/component-catalog.md#formadialog): the modal, the input router's modal scope, opening on its first field.
- [`FormaWindow`](docs/component-catalog.md#formawindow), [`FormaFrame`](docs/component-catalog.md#formaframe), [`FormaChrome`](docs/component-catalog.md#formachrome) and [`FormaRestArea`](docs/component-catalog.md#formarestarea): managed windows, moved, resized, minimized, locked and stacked, as the [window design](docs/window-design.md) sets out.
- [`FormaBackdrop`](docs/component-catalog.md#formabackdrop) and [`FormaStackLayout`](docs/component-catalog.md#formastacklayout): a page composed of regions laid on a canvas.
- [`FormaFreezePane`](docs/component-catalog.md#formafreezepane): a region frozen beside one that scrolls, resized at its edge.
- [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker) and `FormaCalendar`: a range of dates chosen over two months.
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
