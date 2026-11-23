# Changelog

Each release of Forma's packages, newest first, every package of a release at its version. An entry is dated when its
release is cut, and reads *Unreleased* until then. It lists every change a consumer can notice, and a change that
breaks what the release before it allowed says how to migrate, as [Versioning's rules](https://github.com/FormaUI/Versioning/blob/v1.0.0-preview.5/docs/releases.md#the-release-record)
set out. What Forma gained before its first release, month by month, is in [the feature history](FEATURE-HISTORY.md).

## `1.0.0-preview.14`

*Unreleased*

- `FormaComboBox` chooses one value from `Options`, a list of `SelectOption`s that narrows as the reader types: every
  word must start or appear in an option's label, ignoring case and accents, and a label starting with the text comes
  first. Typing changes only the text and the arrows only the active option; a value is committed when an option is
  chosen, or when the field is left on text naming exactly one option that can be chosen, and leaving on no text clears
  a value that can be empty. An `OptionFactory` adds text no option names: the list offers it, the factory creates the
  option or refuses it, and the field commits and shows the option it answers for as long as it holds that value. A
  value no option holds, such as a free name a saved record binds, shows the text `ValueLabel` gives it, else its own
  text in the culture. A polite status says how many options match, and a statically rendered page chooses in the
  browser's own list, which offers a value no option holds as it stands, so the form posts it back.
- `FormaLinkTabs` draws `Items`, a list of `LinkTab`s, as a strip of tabs that link to pages, in a navigation landmark
  named by `Label` and drawn as `FormaTabs` draws its strip. The tab of the page the reader is on is marked
  `aria-current="page"`, and it follows the page as the reader moves on. A tab is current where the page's address,
  without its query and fragment, is the tab's or, for a tab whose `Match` is `NavLinkMatch.Prefix`, lies beneath it; of
  several, the tab with the longest address is current. A tab carries an optional `Icon`, and a `Trailing` tab stands
  at the strip's far end. Every tab is a link, so a statically rendered page goes between pages through them.
- A `SelectOption`'s `IsDisabled` shows it without letting it be chosen: `FormaSelect` and `FormaMultiSelect` draw it
  in the disabled tone and mark it `aria-disabled` or disable its box, the arrows and the type-ahead pass over it, a press
  chooses nothing, and the browser's own list a static page draws disables it unless it's the option chosen.
- `FormaTextArea` takes `ReadOnly`, as the text box and the number, currency, date and time fields do, and every
  labeled field draws a read-only element with the read-only fill. The fill's hook is `--forma-input-readonly-bg-color`,
  beside `--forma-input-disabled-bg-color`. To migrate, rename `--forma-text-box-readonly-bg-color` where a theme sets it.
- A `SelectOption` carries an optional `Icon`, drawn ahead of its label in `FormaSelect`'s list and trigger and in
  `FormaMultiSelect`'s list and tags, and an `IconLabel`, which describes the option, or the trigger showing it, to
  assistive technology and follows the label in the browser's own list a static page draws.
- [`FormaTabs`](docs/component-catalog.md#formatabs) keeps its tabs on one line, the strip scrolling behind an arrow at
  each end they continue past, named Scroll Tabs Back and Scroll Tabs Forward. Tabs past the strip's width wrapped onto
  a second line.
- `FormaTabs` and `FormaLinkTabs` keep their current tab in view: a strip whose active or current tab lies past its
  width scrolls it into view as the strip is drawn, as an arrow appearing narrows it, and as another tab becomes
  current, while a strip the reader scrolled stays put. A freeze pane keeps any element of its content in view this way
  through `FreezePaneOptions.CurrentSelector`, and a DOM engine's scroll interaction through
  `ScrollInteractionOptions.CurrentSelector`.
- A [`FormaFreezePane`](docs/component-catalog.md#formafreezepane), and every strip of tabs or commands built on one,
  keeps the element holding the focus inside it in view as an arrow appearing beside it narrows it, where the focused
  element ended under the arrow.
- `CssColorValue` is a color as a style takes it: a `CssColor`, which converts to one implicitly, or the custom
  property a theme defines one in, `CssColorValue.FromProperty("--success-subtle-color")`, rendered as `var(--name)`.
  A `StatusBadgeStyle`'s colors and `FormaStatusBadge`'s `Background` and `Foreground` are `CssColorValue`s, so a
  status follows the theme. Code passing a `CssColor` compiles unchanged; to migrate code reading a style's
  `Background` or `Foreground` as a `CssColor`, read it as a `CssColorValue`.
- A [`FormaExplorer`](docs/component-catalog.md#formaexplorer) held short keeps its header whole while its upload
  panel is open, the media beneath it giving way first and the panel then scrolling what it holds. The
  panel kept its full height, pushing the media out of the explorer.
- A [`FormaDataView`](docs/data-view-design.md#accessibility-keyboard-and-direction) hands the focus of an item taken away, held by the
  item or by a control inside it, to the item after it, else the one before it, else the first control its empty
  state draws, as a command run from a row's menu deleting the row takes the row. The focus fell to the page.
- [`FormaShareMenu`](docs/component-catalog.md#formasharemenu) shares from a statically rendered page: its trigger
  opens the browser's own popover, where the networks, the email and the download are links, and the commands a script
  runs are drawn disabled. Its trigger was disabled.
- A host's own routes, those its `InputRouterOptions` declares and its `IInputActionHandler` performs, answer on every
  interactive page that renders a Forma component, from that component's first render. They answered only once a
  component on the page had registered with the [input router](docs/input-router-design.md).
- `FormaCommandPalette` searches the commands a host hands it as `PaletteCommand`s, each a name, the `Place` it lives,
  an optional `Icon` and the `Url` it goes to or the action it `Run`s, in a dialog the host opens through `Open`, from a
  button or from a shortcut it routes. Every word typed narrows the commands to those whose name or place holds it,
  ignoring case and accents, a name starting with the query first; the arrows move through them, Enter or a press runs
  one, closing the palette first, and Escape closes it, the focus going back to where it was opened from. A polite
  status says how many match, and on a phone the palette fills the screen.
- A [`FormaDialog`](docs/component-catalog.md#formadialog) given `FullScreenOnPhone` fills the viewport, without
  corners, where the viewport is a phone's.
- `FormaStepper` draws the steps of an ordered flow as a numbered strip, marking the one the reader is on with
  `aria-current="step"`, its number filled and its name strong. A `StepperStep` carries a step's label, a `Url` it
  links back to on every step but the current one, and `IsCompleted`, which draws a check in place of its number and
  says it is completed. Where its names wouldn't fit the width it is given, the steps not current show their numbers
  alone, their names still read by assistive technology, so the row never wraps.
- A [`FormaToastContainer`](docs/component-catalog.md#formatoastcontainer) dismisses a toast, and a
  [`FormaFileUploader`](docs/component-catalog.md#formafileuploader) redraws its time measurements, by the
  `TimeProvider` the host registers, as a data view times its announcement of a slow load. They used the system clock
  whatever the host registered.
- An element's size is a [DOM watcher](src/Framework/Forma.Framework.Browser.Interop/README.md#the-dom-watcher-and-engine)
  observation, `Geometry.WatchSizeAsync`, which reports the element's border box along its writing mode's axes as a
  `SizeState`, its `InlineSize` and `BlockSize` in CSS pixels, as the layout changes it. An implementation of
  `IDomElementObservation`, which now declares `Geometry`, implements it.
- A [`FormaExplorer`](docs/component-catalog.md#formaexplorer) draws the still an item's host gives as its
  `ThumbnailSource`, whatever its kind, and a video's first frame where its item offers `View`, each marked in its end
  corner with its kind's glyph, which a video also shows while its frame loads and wherever the frame can't be drawn.
  A video and every kind but an image drew their glyph alone, and an image drew its own source whatever its
  `ThumbnailSource` named.
- `FormaExplorerThumbnail` takes the `Item` it depicts in place of its `Resource`. To migrate, pass the catalog item
  the resource came from.
- A virtualizing [`FormaDataView`](docs/data-view-design.md#virtualization-and-the-scroll-region) shows the items it
  hasn't drawn as loading, a muted shape pulsing where each will stand: a bar for each of a grid row's values and its
  box, a bar for a list's row, and as many cards as a row of cards holds. The items a scroll uncovers stand that way
  at once, before a circuit's round trip has drawn them, and the range it reaches while its slice is on its way. A
  column's `PlaceholderTemplate` still takes the bar's place in its cells. They stood empty, the scrolled-to range
  showing nothing until the slice answered.
- [`FormaVirtualize`](docs/virtualization-design.md#sources) draws the range a scroll reaches as placeholders while its
  slice is on its way, where it kept the range it left drawn until the slice answered, and each spacer carries the
  room it counts an item at as `--forma-virtualize-item-size`, so an owner can draw placeholders in it.
- `--loading-bg-color` is the tone of a shape standing for content still loading, a placeholder's.
- `CurrencyCatalog.FormatAmount` writes an amount in a named currency as the reader's culture writes money, with the
  currency's symbol in that culture and its minor units, a symbol ending in a letter standing a no-break space from
  the digits. The `C` format string writes the culture's own currency instead, and `¤` for a culture such as `en`
  that names no country.

## `1.0.0-preview.13`

*November 21, 2026*

- A form input takes the validation messages it reports out of its form as it is removed, such as a number past its
  `Max` or a password drawing on a forbidden term, so the form's `GetValidationMessages()` holds only those of the
  inputs it shows, and a host can hold its save while any stands. A removed input left them in the form for good, and
  still followed the form's validation.
- A [`FormaTextArea`](docs/component-catalog.md#formatextarea) scrolls its text beneath its floating label, which holds
  the band across the area's top, so a line past its `Rows` and the caret's line stay clear of the label, and its
  scrollbar starts below the band. The lines scrolled under the label, which was drawn over them.
- A `FormaTextArea` showing its character count floats the count inside its end corner, over the strip it keeps beneath
  its text, and scrolls the line being typed above that strip. The count stood below the area, and the strip stayed
  empty.
- A [`FormaExplorer`](docs/component-catalog.md#formaexplorer) keeps its header's rows whole however short it is held,
  so only its media scroll. The header shrank with the media once they overflowed the explorer, clipping its address
  bar and its toolbar, or on a narrow page its one row.

## `1.0.0-preview.12`

*November 20, 2026*

- A gender expression chooses a string's words by whom it is about, as a plural expression chooses them by a count:
  `[$ [Rider] gender; female: Escaladora; other: Escalador; $]`. Its fields are `female`, `male` and `other`, and an
  enum value or a string selects the field its name spells, whatever its case, so `Gender.Female` and `"female"`
  select `female`; a null, or a name without a field of its own, selects `other`.
- A key's values pair with the arguments its expressions name as well as its holes, in the order each name first
  appears, so a key can carry a value no hole writes, such as a gender:
  `L["[$ [Rider] gender; other: {Name} is the best climber; $]", rider.Gender, rider.Name]`. A key whose expression
  names an argument ahead of that argument's first hole, with another hole between them, pairs the argument earlier
  than it did; to migrate, pass the values in the order the names first appear.
- `PluralExpressionEvaluator` is `LocaleExpressionEvaluator`, which evaluates gender expressions too, and its
  `Keyword` is `PluralKeyword`, beside `GenderKeyword` and the set of both, `Keywords`. To migrate, rename the type
  and the property.
- The Spanish locale pack is written throughout in Latin American Spanish, addressing the reader as *tú*: the table
  menu's *Agregar Fila Arriba* and the three like it, the inputs' *Agrega un número.* and *Agrega un símbolo.*, the
  composer's six prompts starting *Ingresa…*, the explorer's *Video*, and the input router's *Presiona una tecla, haz
  clic o desplázate…*. They read *Añadir…*, *Añade…*, *Introduce…*, *Vídeo* and *Pulse una tecla, haga clic o
  desplácese…*, Spain's words, and the last one addressed the reader as *usted*.

## `1.0.0-preview.11`

*November 18, 2026*

- `ToolbarOption.IsTitleVerbatim` marks a title that is a value shown as written, such as a font's name or a size, and
  the composer leaves such a title as it is. Its font families, font sizes, line heights, special characters and zoom
  levels are verbatim. It looked each of them up as a key, so the rich text editor's toolbar threw under
  `RequireMatchingLocalization` in a language other than English, and `Diagnostics` reported every one missing.
- `TypographyPluginOptions.TextColors` and `HighlightColors` are `TypographyColor`s, each a color's English name and
  its `CssColor`, and the color menus label each color by its name, which Hebrew and Spanish localize. They labeled
  each by its hex code, which a screen reader reads out character by character. To migrate, name each color:
  `new TypographyColor("Red", CssColor.FromHex(0xd32f2f))`.

## `1.0.0-preview.10`

*November 17, 2026*

- The composer's selection bar is named by the selection row's label, *Selection Formatting*, which Hebrew and
  Spanish localize. The row's label had no entry in either language, and the bar carried a label of its own,
  *Selection formatting*. `ComposerSelectionBar` takes the label as its first argument, `Label`; to migrate, pass the
  row's label ahead of the items.
- `ComposerGroups` names the toolbar groups the composer provides, `Media` the first, and the composer titles one in
  its own words whichever plugin declares it, as it labels a row `ComposerRows` names. A plugin's other titles are read
  from the locale its `LocaleSourceType` names alone. The composer read every title there first and fell back to its
  own, so a plugin from another package joining the media group, as `ExplorerPlugin` does, threw under
  `RequireMatchingLocalization` in a language other than English, and `Diagnostics` reported the title missing. The
  image, video and explorer plugins put their commands in `ComposerGroups.Media`, whose identifier is
  `composer.media`; to migrate, a plugin that named the group `media` names `ComposerGroups.Media`.
- An application a test runner hosts, as `WebApplicationFactory` does, reads the locale packs and files its build
  recorded: where the entry assembly's build didn't mark it as the application, every loaded assembly whose build did
  is read before it. A test hosting the application read only the test assembly's, so the application's components
  rendered in English unless the test referenced the packs as well.

## `1.0.0-preview.9`

*November 17, 2026*

- `LocalizationOptions.Diagnostics`, a `LocalizationDiagnostics`, checks every lookup the JSON localization makes, in
  the lookup's culture and in each culture its `Cultures` names, and logs each key it doesn't find an entry for as a
  warning, once per factory: `AssemblyNotLocalized` where none of the sources holds an entry for any of the
  assembly's types in the culture or a culture it falls back to, `TypeNotLocalized` where the assembly holds some and
  the type doesn't, and `KeyNotLocalized` otherwise, each carrying `Assembly`, `Type`, `Key` and `Culture` as fields.
  A culture of the source language is left unchecked, and a file a checked culture can't read is logged as
  `LocaleFileNotRead`, leaving the lookup's text as it is. Under the checks, a plural argument that isn't a count is a
  warning in place of `Debug`, and `PluralExpressionEvaluator` takes them as its third argument.
- Localization's log messages carry stable event ids: 1 for a plural argument that isn't a count, 2 for a locale pack
  that could not be loaded, 3 for a host locale assembly that could not be loaded, 4 for an entry two files give
  different texts, and 5 to 8 for `AssemblyNotLocalized`, `TypeNotLocalized`, `KeyNotLocalized` and
  `LocaleFileNotRead`.
- `RequireMatchingLocalization` accepts the key itself in a culture of the source language and in the invariant
  culture, where the key is the text a reader sees. It threw there for every key that no file listed, so an English
  host requiring localization threw on its first lookup.
- `LocaleEntries.IsEmpty` says whether a source holds anything for an assembly's types in a culture.
- `FormaPopover` registers its input scope before the focus moves into its content, so a key typed while it opens is
  routed to it. A key that landed on the content before the scope was in place was routed nowhere, and a space typed
  into `FormaSelect`'s label as its list opened chose the option it landed on.

## `1.0.0-preview.8`

*November 17, 2026*

- The build embeds a locale file under the project's root namespace, a `/` and the file's path, as
  `Contoso.App/Uploads/.locales/he.json`, and a project's `.locale.json` as `Contoso.App/.locale.json`; a locale pack
  embeds its files under their paths. It wrote a path with every folder made a dot, as
  `Contoso.App.Uploads..locales.he.json`, so two files a folder and a dot tell apart, `Uploads/UploadStatus.he.json`
  and `Uploads.UploadStatus.he.json`, took one name and failed the build; they are embedded side by side and read
  together.
- A path a `.locale.json` states reads an assembly's names as paths: a `/` the path writes is a folder, and the
  segments `$AnchorName` and `$ParentName` stand for are separated by a folder or a dot, as the default reading
  separates a name's parts, so `strings-es/Accounts.Admin.json` and `strings-es/Accounts/Admin.json` both stand for
  `Accounts.Admin`. A dot anywhere in an embedded name stood for a folder. To migrate, give a file a project embeds
  through an item of its own, and a path reads, a `LogicalName` with the folders the path writes.
- An application's files for other assemblies are read beneath its root and a `/`, as the build names them, so a file
  the application embeds through an item of its own for another assembly is read only under a name of that form; the
  SDK's name for it, with every folder made a dot, was read as well. To migrate, give the item a `LogicalName` of that
  form, or leave the file to the build.
- An application with no `.locale.json` whose root namespace isn't its name has its files for other assemblies read:
  its build records the root namespace it names its files under on `LocaleApplicationAttribute`, which takes it as an
  argument, and the runtime reads the files beneath it. They were looked for beneath the application's name and went
  unread.
- A file a satellite assembly holds under the name its item gave it, culture and all, is read and reported under that
  name, where the source added its culture a second time, and a satellite's file named for another culture is left
  out.
- `formaui-net.Framework.Browser` holds the browser layer's abstractions, which `Forma.Framework.Browser.Interop`
  implements: the observation interfaces, the options and states the watcher and the engine take and report, the
  registrations a session holds with `DomRoot`, and `INativeOperations`. They were in the interop assembly's
  namespaces, `IDomRegistration`, `IDomRegistrationHost<TOptions>` and `DomRoot` were internal, a watch returned a
  `DomObservation<TOptions, TState>`, and `TextRange` was `TextEntry.TextRange`. To migrate, import
  `Forma.Framework.Browser`, hold an observation as the `IDomObservation<TOptions, TState>` a watch now returns, and
  name `TextRange` alone.
- MAUI's native operations ship as `formaui-net.Framework.Browser.Maui`, in the namespace `Forma.Framework.Browser.Maui`,
  built on `formaui-net.Framework.Browser` alone. They shipped as `formaui-net.AddOns.Operations.Maui`, in
  `Forma.Framework.Browser.Interop.Operations.Maui`, with the browser interop assembly behind them. To migrate,
  reference the new package, import its namespace and register it with `AddFormaBrowserMauiIntegration()`, which
  was `AddFormaMauiOperations()`.
- `FormaShareMenu`'s Print reads in Hebrew and Spanish. It read in English.
- `formaui-net.Blazor` ships `.locali-hints.json`, which tells the Locali linter that `FormaComponentBase.L` and
  `FormaInputBase<TValue>.L` read their strings for the type `LocaleSourceType` names, so an application that adds
  `"hints": { "packages": ["formaui-net.Blazor"] }` to its `.locali.json` has each key its components look up through
  `L` checked in that type's files.
- `LocaleKeyAttribute` is gone, the Locali linter reading the key `LocalizableText.For<TSource>` is given itself. It
  marked a parameter whose argument is a key looked up in the locale of the call's type argument. To migrate, remove
  `[LocaleKey]` from a parameter. Locali 1.0.0-preview.21 checks the key a method takes as a parameter and passes to a
  lookup or to `LocalizableText.For` at each call of the method that writes it as text, against the type that call
  passes; a key the method looks up only on some paths, or changes first, stays reported as `LOC0208`.
- The type whose locale files a component's `L` reads is `LocaleSourceType`, on `FormaComponentBase` and
  `FormaInputBase<TValue>`, and the type whose locale files hold a composer plugin's titles and labels is
  `IComposerPlugin.LocaleSourceType`. Each was `LocalizationSourceType`. To migrate, override or implement
  `LocaleSourceType`.
- `IComponentContextFactory` makes the context each Forma component is given, from the component's type and the type
  its strings are read for, and `AddFormaUIServices` registers `ComponentContextFactory` for each user scope, so a host
  that registers its own first makes every component's context its way. A component's context is made with its first
  parameters.
- `formaui-net.Framework.Localization.StringLocalizerAdapter` puts Forma's localization behind Microsoft's
  `IStringLocalizer`, registered with `AddFormaStringLocalizerAdapter()`, so code written against `IStringLocalizer<T>`
  reads Forma's locale files. A key naming a Forma hole or expression is filled by Forma, and any other by
  `string.Format` in the formatting culture, as Microsoft's localizer fills it.
- `ICultureScopeInitializer` initializes culture support for a user scope where the render mode does not, through
  `Initialize()`. It was `ICultureScopeStart`, through `Start()`. To migrate, implement or resolve
  `ICultureScopeInitializer` and call `Initialize()`.
- `LocaleSource.FolderSeparator` is gone, every source's names being paths, and `LocalePathMatcher` and
  `LocalePathBinder` take no separator. A source written against them drops its override, and a caller the argument.
  `LocaleSource.GetRootDir` answers a root a `.locale.json` names beneath its folder with `/` between the two, as
  `Contoso.Widgets/Strings`.
- The type-ahead of `FormaSelect`, `FormaDropDownMenu` and `FormaDataTree` measures its pause between the times the
  keys were pressed, which `InputKeyInfo.Timestamp` carries, so a key that reaches .NET late, behind a slow connection
  or a list still opening, extends the label it was typed into. The pause was measured as each key was handled, and
  such a key started the label again. `TypeAhead` takes no clock, and its `Find` takes the `InputKeyInfo` typed in
  place of its character. To migrate, create a `TypeAhead` with no argument and pass `Find` the key.

## `1.0.0-preview.7`

*November 15, 2026*

- A project whose `.locale.json` names a folder of locale files of its own, as `"localesDir": ".strings"` does, has
  the files in it embedded, and an application reads its files there for other assemblies' strings. The build looked
  in `.locales` folders alone, so a folder whose name starts with a dot embedded nothing, and an application read its
  files for other assemblies with `.locales` left out whatever its config named.
- A locale pack embeds the files in a `.locales` folder too, as `Forma.Blazor.Explorer/.locales/he.json`, and holds
  them to the cultures it declares; it left them out, unchecked. A pack embeds the JSON files that carry the
  `$localization` entry, as a project does, where it embedded every JSON file beneath it.
- Two locale files whose paths differ only where one has a folder and the other a dot, `Uploads/UploadStatus.he.json`
  and `Uploads.UploadStatus.he.json`, fail the build naming both files and the resource name they would share. The
  compiler refused them, naming the resource alone.
- A locale file in a culture folder may name its culture again, so `.locales/es/es.json` is the root's file, as
  `.locales/es.json` is, and `.locales/es/Pages.Home.es.json` names `Pages.Home`, as `.locales/es/Pages.Home.json` and
  `.locales/Pages.Home.es.json` do. A part repeating the culture went on the name, so such a file was read for a
  namespace or type named for the culture.
- A `LocaleSource` answers where an assembly's names begin through `GetRootDir`, and the folder of locale files they
  leave out through `GetLocalesDir`, as it answers the assembly's root namespace through `GetRootNamespace`. Both were
  internal.

## `1.0.0-preview.6`

*November 15, 2026*

- A [`FormaTextBox`](docs/component-catalog.md#formatextbox), `FormaPasswordBox`, `FormaTimePicker`,
  `FormaDateTimePicker`, `FormaWindow`, `FormaDataTree`, `FormaComposer` or `FormaEmojiPicker` taken away while the work
  of its first render still waits on the browser, as a page left the moment it appears takes what it shows, goes without
  an error. It went on to reach the browser through a context already disposed, whose `ObjectDisposedException` ended an
  interactive server circuit and showed WebAssembly's error bar.
- A [`FormaToolbar`](docs/component-catalog.md#formatoolbar), a `FormaComposer` or a composer's plain or rich text
  editor taken away while its shortcuts still register with the input router releases them once they do. They stayed
  registered, so a shortcut pressed on a toolbar's target afterward was still taken from the page, and the call into the
  toolbar gone logged an error to the console.
- `HandleInputActionAsync` is gone from `FormaToolbar`, `FormaComposer`, `FormaPlainTextEditor` and
  `FormaRichTextEditor`; only the input router called it. A host runs a toolbar's or a composer's command through its
  `HandleCommandAsync`.
- A [`FormaTabs`](docs/component-catalog.md#formatabs) inside a form leaves it unsubmitted as a tab is chosen, by a
  press, Enter or Space. Its tab buttons were the form's submit buttons, so choosing a tab submitted the form.
- A [`FormaExplorer`](docs/component-catalog.md#formaexplorer) whose store answers its first query asynchronously lists
  what the store answered. On an interactive server and in a web view, an answer arriving before the page had taken the
  explorer's first render in left it showing that it loads until a refresh.
- An item of a [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu) that doesn't keep the menu open
  closes it, handing the focus back to its trigger, before its command runs. The command ran first, so one opening a
  dialog kept the menu open behind the dialog, and one removing the menu's row left the focus on an element gone from the
  page.
- A [`FormaPopover`](docs/component-catalog.md#formapopover) closing after a press inside it in a WebKit browser, as a
  menu does once an item is pressed or a select once an option is, hands the focus back to its trigger. WebKit focuses
  no button it presses, so the focus fell to the page and stayed there.
- Every input takes a `Description`, instructions drawn beneath it and named in its `aria-describedby` after the host's
  own ids and ahead of the form's messages. Its [`FormaField`](docs/component-catalog.md#formafield) draws it, and an
  unlabeled `FormaTextBox` or `FormaNumberInput` given one draws a field to hold it; a `FormaCheckBox` draws it under its
  label, and a `FormaComposer` beneath its frame, where an editor plugin names it on its editable through
  `ComposerContext.DescribedBy`. `FormaField`'s `FooterContent` stays for anything else beneath a field, which no input
  reads as its description.
- Every input has a `FocusAsync`, which focuses the control it is worked in, a `FormaDateTimePicker`'s date or a
  `FormaComposer`'s editable among them, leaving the page where it stands when asked, and answers whether it asked an
  element for the focus. Before the input is drawn, once it is disposed, under a static render, while it is disabled and
  once its circuit is gone, it asks nothing and answers false. A `FormaCurrencyInput` hands it to the number input it
  draws.
- `IComposerInteraction` takes an editor's focus through `JoinFocus`, which an implementation of its own adds: it holds
  the focus the editor hands it until the registration it answers with is disposed.
- A paged `FormaDataView` holds a full page's height only while another page is known to exist, by its count, by its
  source's `HasMore` or by a page before the one shown, and lets a height it held go once a filter or a deletion leaves
  one page. A set that fits one page, or that its source neither counts nor says more of, takes its natural height; it
  was held at a full page's height whenever the view paged.
- A [`FormaToastContainer`](docs/component-catalog.md#formatoastcontainer) stands as far from the viewport's block-start
  and inline-end edges as `--forma-toast-container-inset-block-start` and `--forma-toast-container-inset-inline-end`
  set, `--space-5` unless a host sets them, so a page with a sticky header moves its toasts below it, and a toast is
  never wider than the viewport between those offsets.
- A [`FormaPageTitle`](docs/component-catalog.md#formapagetitle) moves its actions beneath its heading, still at the
  end, where the two don't fit one line, and its heading shrinks, a word too long for the line breaking; its row didn't
  wrap, so a long heading widened the page. A [`FormaDialog`](docs/component-catalog.md#formadialog)'s title shrinks
  beside its close button the same way.
- A [`FormaImagePicker`](docs/component-catalog.md#formaimagepicker) and a
  [`FormaFilePicker`](docs/component-catalog.md#formafilepicker) have a `ClearAsync`, which lets go of the files chosen,
  the image picker's previews and the refusals shown, and empties the file input, so a file dialog choosing the same
  file again hands it over again. A host clears a picker once it has taken the files it was handed.
- A [`FormaButton`](docs/component-catalog.md#formabutton) given an `Href` is a link in the button's look, opening its
  address in the `Target` and with the `Rel` it is given, so a reader can copy it, open it in another tab or follow it
  with a middle click. Disabled or busy, the link names no address and stays a link, and it keeps its address under a
  static render; a link given a submit or reset `Type`, or `ReadsForm`, is refused.
- `AllowlistHtmlSanitizer` reads an attribute's value with its character references resolved, as a browser reads it,
  judges what it reads and escapes it once, so a second pass over what it kept changes nothing. Each pass escaped a
  reference's ampersand again, so an address holding `&amp;`, a quoted font name in a style or an embed's settings came
  back corrupted from every save; an address spelling its scheme in references is now judged by the scheme it spells.
- `Glyph.Pause`, two solid upright bars, joins the icon set, the pause to set beside `TriangleRight`'s solid play
  triangle.
- A [`FormaSelect`](docs/component-catalog.md#formaselect) moves to the option whose label a reader types: characters
  typed within half a second of each other build a prefix, a space among them, a character typed again on its own
  moves on through the options it starts, and labels compare in the culture in force. Typed on the closed select, they
  open its list on that option, and nothing is chosen until the reader chooses it. A `FormaDataTree` searches its nodes
  the same way, where it matched a single letter, and a [`FormaDropDownMenu`](docs/component-catalog.md#formadropdownmenu)
  its items, each by the label it is read by.
- A `TypingBinding` also matches a space typed within `TypeAhead.Pause` of the last character a typing binding matched,
  and the input router leaves that space to the typing route rather than to the button holding the focus, so a label
  typed into a type-ahead can hold one. `ElementLabels`, among the browser operations, reads the label each element is
  read by.
- `IFileStore.SaveAsync` answers with a `FileSaveResult`: `FileSaveResult.Saved(resource)` for a file it kept, or
  `FileSaveResult.Refused(refusal)` for one it turns away, its `FileRefusal` carrying a `FileRefusalReason`,
  `DisallowedType`, `TooLarge`, `TooWide` or `TooTall`, and the `Limit` the file went past. A store of a host's own wraps
  the resource it returned in `FileSaveResult.Saved`, and answers with `FileSaveResult.Refused` where it threw to refuse a
  file, keeping exceptions for failures.
- A [`FormaFileUploader`](docs/component-catalog.md#formafileuploader) shows a file its store refused as refused, with
  a sentence beneath it in the reader's language naming the limit, and raises `OnRefused` with an `UploadRefusal`
  carrying that sentence; `StopOnFailure` and `RollbackOnFailure` treat a refusal as they treat a failure. A store
  could refuse only by throwing, and a refused file showed as failed, with nothing to say why. `UploadState.Refused`,
  between `Failed` and `Skipped`, marks such a file in an `UploadBatch`, which `RefuseFile` moves it to, its
  `UploadFile` holding the store's `Refusal`.
- A [`FormaExplorer`](docs/component-catalog.md#formaexplorer)'s upload panel stays open while it shows a file that
  failed or that its store refused. It closed once any file of the set was stored, taking with it what the rest came to.
- `CatalogConstraints` takes a `MaxFileBytes`, the most bytes one file may hold, and a `FormaExplorer` refuses a file
  picked for upload past it, raising `OnRejected` with `CatalogRejectionReason.FileBytes`; the composer's
  `ExplorerPluginOptions` forwards one.
- A `FormaExplorer`'s Hebrew refusal naming the most bytes it holds reads the size in its own direction, where the unit
  stood before the number.
- A [`FormaFilePicker`](docs/component-catalog.md#formafilepicker) refusing a file too large names the size it went
  past.
- A `FormaExplorer`'s sidebar ends every folder's count at one edge. A folder offering no actions, All Media among
  them, drew its count in the room the others keep for their actions' menu.
- A `FormaFileUploader`'s Hebrew byte count and time left read in order. The byte count read its total before what had
  been sent, and the time left its unit before its number.
- Under culture routing a page can have an address of its own in each culture. A host's `ICultureDestinationResolver`
  names where a page leads in another culture, a `CultureDestination` that is its address there, `Localized`, or a
  `Fallback` the host sends the reader to in its place, and every switch by address goes there, through
  `ICultureSwitch` or `CultureSwitcher` and as a web view follows its application's culture. `CultureSwitcher`'s
  `GetDestinationAsync` and `GetDestinationUriAsync` give a link the same destination, `IsFallback` included. A switch
  went to the current page's address in the other culture alone.
- `CultureCoordinatorRegistration` takes an `ICultureDestinationSource`, which culture routing registers, and its
  receiver answers the coordinator's `ResolveDestinationAsync`; the coordinator's `register` takes whether the scope
  names destinations.
- Two locale files of one source giving an entry different texts are a conflict: the runtime shows the text of
  the file whose name sorts last, ordinally, and logs the conflict once as a warning naming both files. The file closer
  to the type won before, so a project that laid a deeper file over a shallower one for the same key keeps the key in
  one of them, or gives both the same text. `LocaleSource.ReadEntries` reads what a source holds for an
  assembly's types in a culture once, with the `LocaleConflict`s among its files, each saying whether it is between
  shared entries, and the sections each type reads, whether their entries win or not; `LocaleSource.Read` answers one
  entry for each key; and `LocaleFileNames.TryRead` reads a file's name as the default reading does.
- Every locale file is embedded under its project's root namespace and its path with every `/` made a `.`, named by
  the build, so a culture folder such as `es-419` keeps its name and a project's root `.locales` may hold a folder per
  culture, `.locales/es/Accounts.Admin.json`. An application's files for another assembly are embedded and named as
  its own are and read from beneath its root by `ApplicationLocaleSource`, so the reserved prefix `.locales.` is gone,
  with `LocalePackSource.HostResourcePrefix`, `LocalePackSource`'s `resourcePrefix` and
  `LocaleAssemblyLoader.EmbedsHostFiles`: a project that embedded a file under `.locales.` itself embeds it under its
  root namespace instead. A library embeds its files for another assembly too, which are read only from an application;
  an application is always read for other assemblies' strings; a server records each client it references that embeds
  a locale file; and a pack's culture check reads a culture folder as well as a file's name, as the runtime does.
- `LocalizableText` carries text to localize when it is shown, its source type, its key and its holes' values, as a
  registry's labels need, and `IStringLocaleFactory.Localize` looks it up in the current cultures or a culture
  state's, localizing a value that is a `LocalizableText` in turn. `LocalizableText.For<TSource>` marks its key
  `[LocaleKey]`.

## `1.0.0-preview.5`

*November 12, 2026*

- A [`FormaDataView`](docs/data-view-design.md#accessibility-keyboard-and-direction) gives the focus back to an item
  whose box went while holding it, as turning the selection mode off takes the boxes away, in Safari too, which drops
  such a focus without saying so; it fell to the page there.
- [`FormaDialog`](docs/component-catalog.md#formadialog) hands the focus back, as it closes, to what held it as the
  dialog opened, and where that has gone, as a menu item does once its menu closes, to the nearest element around it
  that can take the focus. A dialog opened from a menu, as an explorer item's Rename is, left the focus on the page.
- [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker)'s swatch holds its color in lowercase, as the
  platform's color input does, which Safari kept in the case the value was typed in.
- The framework's members and parameters named for a bare participle or adjective, or for a plural that counts, are
  named for what they hold. Properties and constants: `CatalogRejection.Limit` was `Allowed`,
  `ExplorerHistory.CurrentSource` was `Current`, `IConfirmDialogService.CurrentRequest` was `Current`,
  `KeyBinding.MatchesRelease` was `Released`, `PluralOperands.AbsoluteValue` was `Absolute`,
  `LocaleExpressionParser.OpeningDelimiter`, `ClosingDelimiter` and `InterpolationDelimiter` were `Opening`, `Closing`
  and `Interpolated`, `WindowManager.CurrentWindow` was `Current`, and `WindowSelector.IsRecursive` was `Recursive`.
  Parameters, which a caller naming its arguments names anew: `Window.AttachAsync` and `PresentAsync` take
  `isPresented` and `isModal`, `DomReceiver.EditAsync` takes `startIndex`, `removedLength` and `insertedText`,
  `Download`, `Print` and `Share` take `nativeOperations`, `ComposerToolbarLayout.Coalesce` takes `placedItems`,
  `ICultureStateIntegration.ApplyAsync` takes `acceptedCulture`, `UploadBatch.UpdateFileProgress` takes
  `bytesTransferred`, `LocaleAssemblyLoader.GetApplicationAssemblies` takes `loadedAssemblies`,
  `LocaleExpressionParser.TryNormalize` gives `normalizedText`, `IMarkupPolicy.Allows` gives `canonicalSpelling`,
  `InputRegistration.SyncAsync` takes `shouldRegister` and `isActive`, and `IManagedWindow`'s setters take `isActive`,
  `isMinimized`, `isHidden` and `isBlocked`.
- Three component parameters are named for what they hold: `FormaCalendar.MonthCount` was `Months`,
  `FormaPagination.TotalItemCount` was `TotalItems`, and `FormaEmojiPicker.RecentEmojis` and `RecentEmojisChanged`
  were `Recent` and `RecentChanged`, bound as `@bind-RecentEmojis`. A page still passing an old name renders it as an
  attribute of the component's root rather than failing to build.
- The composer's table counts are named as counts: `TablePluginOptions.MaxRowCount`, `MaxColumnCount`,
  `DefaultRowCount` and `DefaultColumnCount` were `MaxRows`, `MaxColumns`, `DefaultRows` and `DefaultColumns`;
  `TableDialogResult.RowCount` and `ColumnCount` were `Rows` and `Columns`; and `FormaComposerTableDialog`'s
  `MaxRowCount` and `MaxColumnCount` parameters were `MaxRows` and `MaxColumns`. A composer dialog's `ReopeningResult`
  parameter was `Reopening`.
- The data view's Booleans and items say what they hold: `IDataView.IsAllSelected` and `IsPartiallySelected`, on
  `FormaDataView` too, were `AllSelected` and `PartiallySelected`, and `DataViewCurrentItemChange.PreviousItem` was
  `Previous`. Parameters, which a caller naming its arguments names anew: `IDataViewInteraction.ToggleSelectionAsync`
  takes `isSelected`, `FormaDataView`'s selection handlers take `isAdditive` and `HandleSeekAsync` takes `isLast`, and
  `DataViewSort.ThenApply` takes `orderedItems`.
- [`FormaExplorer`](docs/explorer-design.md)'s `SelectedItems` and `SelectedItemsChanged` parameters were `Selected`
  and `SelectedChanged`, bound as `@bind-SelectedItems`. A page still passing an old name renders it as an attribute of
  the explorer's root rather than failing to build.
- A [`FormaDataGrid`](docs/data-view-design.md#accessibility-keyboard-and-direction) row's box is named for its item,
  "Select Hammer", where the data view's `ItemName` names the items, as the boxes of the list, the cards and the tree
  are; it said "Select Row" in every row. `IDataView` declares `NamesItems`.
- `CatalogProviderItems` is gone: [`FormaDataView`](docs/data-view-design.md) pages and appends a provider's items, and
  `ReadAllAsync` reads a whole query where a surface needs every item.
- `CatalogHistory` is `ExplorerHistory`, in `Forma.Framework.Explorer.Navigation`, the history of the folders an explorer
  has visited.
- [`FormaExplorer`](docs/explorer-design.md#the-parts) takes its selection from the data view it browses through,
  which decides what may be selected and how many, where the explorer kept a selection of its own beside it. Its parts
  read the selection as `IExplorerSelectionView`, folders first, and `IExplorerSelectionInteraction` takes an item out
  with `DeselectAsync`. `CatalogSelection` and `ICatalogSelectionView` are gone; a part reads `IExplorerSelectionView`
  in their place.
- `ExplorerItemGroup`, `ExplorerGrouping.GroupBy`, `IExplorerContentView.VisibleGroups` and `IsCollapsed`,
  `IExplorerContentInteraction.ToggleGroupAsync`, `IExplorerItemInteraction.ActivateAsync` and
  `ExplorerItemManager.OnActivate` are gone with the explorer's own views: the data view groups, folds and activates the
  explorer's items, and `ExplorerGrouping.Define` gives the groups it gathers them into.
- A [`FormaExplorer`](docs/explorer-design.md#decision-summary) opened with `SelectOnOpen` is a picker that keeps its
  choices as the reader moves between folders, where a move cleared them. Its selection bar stands beneath its
  toolbar throughout, its count a button opening the choices for review, each with the folder it stands in and a
  button removing it; where `AcceptedResourceKinds` names folders, Choose This Folder chooses the folder the reader is
  in. `IExplorerView` declares `CanSelect` and `GetSelectedItemLocation`, and `IExplorerSelectionInteraction` declares
  `DeselectAsync`.
- [`FormaExplorer`](docs/explorer-design.md#decision-summary) follows a move to another folder itself, so a host that
  leaves `CurrentFolder` unbound, as the composer's stored-files dialog does, sees the breadcrumbs, Up and a new
  folder's place follow the reader; they stayed at All Media.
- The composer's [stored-files dialog](src/AddOns/Forma.AddOns.Composer.Plugins.Explorer/README.md#register) browses
  the folders the host's `ICatalogProvider` lists, reading every one as it opens, a reader entering a folder to reach
  the files in it; a provider that lists none browses the files flat, as before. A file activated by Enter is inserted
  at once, and a folder is never chosen. With no `AcceptedResourceKinds`, the dialog lists and chooses every file.
- A composer dialog's body confirms the dialog itself through
  [`IComposerDialogInteraction`](src/Framework/Forma.Framework.Composer.Extensibility/README.md#dialogs-and-insertion),
  which the composer cascades to it. One confirmation runs at a time, so Insert pressed while one is under way inserts
  once, and a dialog closed while its body answers inserts nothing; the body's `TryConfirmAsync` is cancelled then. A
  plugin's dialog hands the focus back to what opened it as it closes; it left the page while still open, which
  dropped the focus.
- [`FormaExplorer`](docs/explorer-design.md#decision-summary) lets a folder be chosen only where
  `CatalogConstraints.AcceptedResourceKinds` names folders, `contentType:image;folder`, and enters a folder a press
  lands on otherwise, in the selection mode too; an explorer whose accepted kinds name none chose folders all the same.
  `CatalogResourceFilter` takes `folder` as a content type, `AcceptsFolders` saying whether a filter takes folders,
  while a glob names files alone. `OnFileActivated` hands the host a file activated by a press or Enter, in place of
  opening its preview.
- [`FormaDataView`](docs/data-view-design.md#selection) activates an item a press lands on where
  `CheckedSelectionOptions.ChecksOnPress` has a press check boxes and the item cannot be selected, so a reader opens a
  folder a picker will not let them choose; such a press did nothing.
- A [`CatalogQuery`](src/Framework/Forma.Framework.Catalog/README.md#providers) names the kinds of item it asks for in
  `Kinds`, null asking for every kind and an empty set for none, and `AsksFor` answers for one. A provider applies them
  before it pages, so its slice, positions and counts describe those kinds alone; a provider that ignores them answers
  wrongly once anything asks for one kind alone; `GetKind` answers a resource's kind as it does an item's.
  `ReadAllAsync` reads every item a query asks for, a slice at a time.
  [`FormaExplorer`](docs/explorer-design.md#decision-summary) asks its `ItemsProvider` for files alone, its folders
  coming from `Folders`; a folder a provider listed among its files was shown among them.
- Forma's packages build against .NET 11.0.0 and depend on its released packages, `11.0.0` in place of the release
  candidate's `11.0.0-rc.3.26522.148`, whose ASP.NET Core packages carry published security advisories, and of
  `11.0.0-rc.2.26505.5` for the MAUI packages. A host builds with the .NET 11.0.100 SDK.
- The registrations name what they add, the shared services or a host's integration. `AddFormaUI` is
  [`AddFormaUIServices`](README.md#what-the-registration-does), the shared services used by Forma UI components;
  `AddFormaUIServer`, `AddFormaUIWebAssembly` and `AddFormaUIWebView` are `AddFormaServerIntegration`,
  `AddFormaWebAssemblyIntegration` and `AddFormaWebViewIntegration`, each still registering those services, and
  their options `UIServerOptions`, `UIWebAssemblyOptions` and `UIWebViewOptions` are `ServerIntegrationOptions`,
  `WebAssemblyIntegrationOptions` and `WebViewIntegrationOptions`. `AddFormaCultureServer`,
  `AddFormaCultureWebAssembly` and `AddFormaCultureWebView` are `AddFormaCultureServerIntegration`,
  `AddFormaCultureWebAssemblyIntegration` and `AddFormaCultureWebViewIntegration`, `AddFormaBrowserServer` is
  `AddFormaBrowserServerIntegration`, and `AddFormaDataView` is `AddFormaDataViewServices`. A host calls each by its
  new name, on the same builder or collection, and names the options types by theirs; what each registers is
  unchanged.
- `LocalePackLoader` is [`LocaleAssemblyLoader`](src/Framework/Forma.Framework.Localization/README.md#locale-packs),
  for the packs and the assemblies carrying a host's files it loads; its `Load` is `LoadPacks` and its `GetNames` is
  `GetPackNames`. A test or a host that read a pack's assembly through it calls `LocaleAssemblyLoader.LoadPacks`, one
  that read the names of an assembly's packs calls `LocaleAssemblyLoader.GetPackNames`, and a host filtering its log by
  the loader's category names `Forma.Framework.Localization.LocaleAssemblyLoader`.
- On Android and iOS, the [locale packs](src/Framework/Forma.Framework.Localization/README.md#locale-packs) read are
  the ones the application's build recorded, in its order, as on every other platform. A library that recorded a pack
  for its own use was read as well, and the assembly that loaded last put its packs first, so a pack could override
  the one the host named.
- An in-place [culture switch](src/Framework/Forma.Framework.Culture.Router/README.md#switching) whose navigation a
  handler refuses fails with `TimeoutException` once `CultureRouterOptions.SwitchTimeout`, 30 seconds by default, has
  passed, and the page still follows the navigation should it land. Under interactive routing such a switch never
  completed, and every later switch waited behind it; a newer switch now takes the place of one still waiting.
- In a form, a submit [`FormaButton`](README.md#forms-and-long-text), and a `FormaButton` or `FormaInputScope` set to
  the new `ReadsForm`, waits for the form's fields before it acts. Every other button and routed action acts at once,
  so a pager's steps, a dialog's Cancel and a composer's commands stay usable beside a field whose text didn't arrive.
  A host action that reads the whole form without submitting it, such as a preview, sets `ReadsForm`. A pager's page
  box and an explorer's name boxes no longer join the form around them.
- `AutofillTracker` is removed. A field's autofill is a [DOM watcher](src/Framework/Forma.Framework.Browser.Interop/README.md#the-dom-watcher-and-engine)
  observation, `Text.WatchAutofillAsync`, which reports whether the browser filled the field as an `AutofillState`,
  starting from a fill made before the observation began. A component that attached `AutofillTracker` to a field
  watches its autofill and draws its own class from the state, and an implementation of `IDomTextObservation`, which
  now declares `WatchAutofillAsync`, implements it.
- [`FormaTextBox`](docs/component-catalog.md#formatextbox) and `FormaPasswordBox` hold their label above a value the
  browser filled in through every render, and over a fill made before the page became interactive. A render that
  changed the field's classes dropped the mark that held the label up, and a fill made before the box attached left
  it unmarked.
- [`FormaInputInspector`](docs/component-catalog.md#formainputinspector) carries `data-forma-inspecting` on its
  element while it listens.
- Tab returns into a [`FormaDataTree`](docs/data-view-design.md#the-tree) at the node last focused in it, and into a
  sidebar at its current node, each falling back to the current node and then the first. A node Tab returns to that is
  removed or closed over while the focus is elsewhere hands the return to the node taking its place, and the page's
  focus stays where it is. A tree whose rows carry the focus returned it to the current node or the first.
- A secondary or danger [`FormaButton`](docs/component-catalog.md#formabutton) draws its label in its own
  `--button-secondary-text-color` or `--button-danger-text-color` at rest, under the pointer and pressed. It drew it in
  `--button-primary-text-color` there, so a theme giving those labels a color of their own showed it only while busy.
- A [`CatalogQuery`](src/Framework/Forma.Framework.Catalog/README.md#providers) names the folder its slice was asked
  for, `FolderKey`, its first parameter, null for every folder's items, captured when the explorer asks; and items the
  sort ties stand in the ascending ordinal order of their keys in either direction, in the explorer's held items too.
  A provider that read the host's current folder reads `query.FolderKey` instead, which keeps the folder the slice was
  asked for when the host moves on before the answer; one that ordered tied items its own way orders them by key; and
  code constructing a query passes the folder first.
- A [`CatalogPage`](src/Framework/Forma.Framework.Catalog/README.md#providers) says how many store positions its
  slice read, `ReadCount`, whether another slice follows, `HasMore`, and, from a store that applied the query's new
  `Filter` and left hidden items out, how many items the whole query lists, `ListedCount`. `FormaExplorer` reads by
  store position through a slice it leaves out whole, appended or paged, and an appending explorer says "Showing X of
  Y" only from a `ListedCount`, and "Showing X items" otherwise. It counted Y as the store's total less the items
  left out so far, which counted what the slices not yet read would leave out as listed.
  `CatalogProviderItems.FetchedCount` is `ReadCount`, and `WasFull` and `ListedTotal` give way to `HasMore` and
  `ListedCount`.
- [`CatalogConstraints`](src/Framework/Forma.Framework.Catalog/README.md#requests-and-constraints) takes
  `AcceptedUploadKinds`, the kinds an upload may add where they differ from the kinds the catalog lists. The
  composer's stored-files dialog lists every kind its `AcceptedResourceKinds` names however few its
  `AcceptedUploadKinds` allows; it listed only the kinds both named, and offered nothing past the first slice it had
  filtered.
- A [`CatalogQuery`](src/Framework/Forma.Framework.Catalog/README.md#providers) carries the `Grouping` its items
  gather into, groups whose keys hold in every culture and on every day and whose rules a store applies without the
  reader's culture or clock, and a page answers `IncludesGroupSizes` with `GroupSizes`. `FormaExplorer` groups the
  whole result rather than the items it has loaded: held items and a provider's order by the groups before the sort,
  a heading counts its whole group, the groups are defined once for each browsing sequence, so a page read after
  midnight joins the groups of the page before it, and dates are banded by the days of the `TimeProvider`'s time zone.
  A group heading counted the items on its page, and a provider's items were grouped a page at a time.
  `ExplorerGrouping.GroupBy` takes the definition `ExplorerGrouping.Define` builds, an `ExplorerItemGroup` carries its
  `Key` and its whole `Count`, and a kind group's key is its `CatalogItemKind`, which `GetKind` reads from an item,
  where it was its label.
- A [`DataViewRequest`](src/Blazor/Forma.Blazor.DataView/README.md#items-from-a-provider) says whether it reads the
  content the views beside a tree show at the current node, or a node's children for the tree, in its `Kind`, so a
  provider tells the two apart even at the roots.
- [`FormaDataView`](docs/data-view-design.md#selection) takes `IsItemDisabled`, `CanSelectItem`, `CanActivateItem`
  and `CanFocusItem`, rules each view draws and every path applies: a disabled item is muted and neither selected
  nor activated, one that cannot be selected still activates and says nothing of whether it is selected, one that
  cannot be activated never becomes the current node, and one that cannot take the focus draws no marker, the keys
  passing over it and a focus it loses going to the nearest item that has one. `IDataView` declares `IsDisabled`,
  `IsSelectable`, `CanActivate`, `CanFocus` and `IsMuted`, which a host's own view implementing it implements.
- [`FormaDataView`](docs/data-view-design.md#extending-the-data-view) takes `ItemAttributes`, the attributes and
  handlers a host gives the row, card, list item or node a view draws for an item, so the whole item can be a drag
  source or a drop target; the view's own attributes win. `IDataView` declares it.
- A [`FormaDataView`](docs/data-view-design.md#selection) whose `CheckedSelectionOptions.Maximum` is one replaces
  its choice: checking a second item chooses it in one change, a Shift range chooses the item it reached, and no
  master control is drawn. It refused the second item and muted the rest. A Shift range reaches the data view's
  owner in order from its anchor to the item reached.
- [`FormaDataView`](docs/data-view-design.md#sorting-paging-and-the-address) takes `DefaultSort`, the sort it opens
  in before any view draws, which the address leaves out, and `RequiresSort`, which keeps a sort applied: a header
  turns its direction rather than clearing it, and a cleared sort returns to the default. A column's
  `IsDefaultSortColumn` is gone; a grid that opened by a column names that column's sort as the data view's
  `DefaultSort`, by the column's sort name.
- [`FormaDataView`](docs/data-view-design.md#sorting-paging-and-the-address) takes `AppendMode`, which appends each
  page to the items from a Load More action its views draw after them, and as the reader reaches their end where it
  is `Automatic`: one request answers both, a failed page is offered again, a new run drops a late page, a page that
  read positions it left out whole reads on, and the footer says how many items show. A `DataViewPage` says how many
  positions it read, `ReadCount`, whether more follow, `HasMore`, and how many positions the source holds,
  `PositionCount`, which the paginator counts pages in. `IDataView` declares `IsVirtualizing`, `ItemCount` and
  `MoreContent`, and `IDataViewInteraction` declares `LoadMoreAsync`.
- [`FormaDataView`](docs/data-view-design.md#the-views) takes `Layout`, which hands the arrangement of its views and
  footer to the host under `Host`: the host places the views, a sidebar tree among them, inside the data view's
  content, and the footer where it places the new `FormaDataViewFooter`, the data view drawing none of its own; its
  arrangement fills the data view's region, so each of its places scrolls within itself. `BuiltIn` keeps the data
  view's own arrangement. `IDataView` declares `Layout`. `IDataViewInteraction.UnregisterView` takes the
  registration a view made, rather than its key, so a view drawn afresh under the same key keeps its registration;
  it lost it as the one it replaced went, which left the data view with no view to show.
- [`FormaDataView`](docs/data-view-design.md#accessibility-keyboard-and-direction) takes `EnableItemFocus`, which gives
  the items of a list, cards or a grid the focus where no selection is on: one tab stop, the arrows moving the focus
  alone, and Enter and a press activating the item. `CheckedSelectionOptions.ChecksOnPress` has a press check an
  item's box rather than activate it, as a selection mode does, Enter still activating it, and turning a selection
  on or off keeps the focus on the item it stood on, and a list item or a card that takes the focus is named for its
  item. `IDataView` declares `FocusesItems`. A selection turned on or off redraws every view's items; a view drew its
  old markers until something else changed.
- Under WebAssembly, a [`FormaCheckBox`](docs/component-catalog.md#formacheckbox) with `PreventDefault` is drawn in the
  state its caller sets on a click, and so is every data view's box; the browser undid the click's toggle after the
  state was drawn, so a box stayed unchecked though its item was selected.
- A selection the host hands [`FormaDataView`](docs/data-view-design.md#selection) through `SelectedItems` redraws
  every view's items it changes; a view drawn in the host's content kept drawing the selection it last drew, so a
  box the host cleared, or one it refused, stayed checked.
- A disabled item in a [`FormaDataView`](docs/data-view-design.md#selection)'s list, cards, grid or tree is drawn muted
  with no selection on, and an item that takes the focus itself, or a tree's node, is `aria-disabled`; a view muted
  it only while a selection was on, and nothing announced it disabled.
- A sidebar [`FormaDataTree`](docs/data-view-design.md#the-tree) takes `RootsItem`, an item of the host's that
  stands for the roots first among the top-level nodes, current while the roots are, so choosing it takes the
  reader back to them; a sidebar offered no way back to the roots of its own once a node was current.
- [`FormaDataView`](docs/data-view-design.md#hierarchy) raises `OnCurrentItemChanging` before a move into a node
  changes anything, with a `DataViewCurrentItemChange` naming the node and the one before it, whose `Refuse()`
  leaves the current node, the page and the items as they were; a move made while the host is still answering
  supersedes the one it answers.
- [`FormaDataView`](docs/data-view-design.md#accessibility-keyboard-and-direction) takes `TabOrder`, whose
  `ActiveItem` takes every item's own controls out of the Tab sequence but the active item's, the one holding the
  focus or a collection's tab stop, so Tab enters a collection of hundreds of items once and leaves it after one
  item's controls; `EveryItem`, the default, keeps every control in the sequence. Shift+F10 and the Menu key on an
  item press the control inside it that carries `data-item-actions`, and the focus comes back to the item as its
  actions close. A grid row being edited keeps its editors in the sequence.
- In a [`FormaDataView`](docs/data-view-design.md#the-views), a list's items, the cards and a tree's nodes draw in
  their views' styles: a row stands on the target step, a node is indented by its depth, a selected, current,
  disabled or hovered item shows it, and a group heading over the list's or the cards' groups reads from its start
  with its count in the supporting tone. They drew unstyled, each drawn outside its view's style scope.
- [`FormaDataView`](docs/data-view-design.md#hierarchy) takes `InitialOpenKeys`, the keys of the nodes that start
  open, each opened once as the level holding it first loads, so a provider is asked for the branches on the way to
  them alone and a named node the reader closes stays closed. A node that becomes current is revealed in any tree,
  plain or a sidebar, its ancestors opened once the data view knows the path from a root, which also reveals a
  current node the host names on load once the levels holding it arrive; it revealed only in a sidebar, and only
  where the way to the node was loaded as it became current. A node open while its children were on their way when
  the source changed is asked again; it stayed open with nothing under it. The content at the roots no longer
  overwrites the parent a tree learned for an item it lists.
- [`ExplorerContentProvider`](src/Framework/Forma.Framework.Explorer/README.md) answers a location's content, its
  child folders before its files, in either sort direction, under the grouping's leading group of folders, and
  `ExplorerGrouping.Define` leads every grouping with that group. Its slices split at the count of matching folders,
  so a slice of folders alone asks the file provider nothing. [`CatalogListProvider`](src/Framework/Forma.Framework.Catalog/README.md#building-a-surface)
  answers queries from a list it holds as a held collection is shaped.
- A [`CatalogFolderTree`](src/Framework/Forma.Framework.Catalog/README.md#building-a-surface) leaves out a folder whose
  key an earlier folder holds, as it found only the first by that key; it listed both among their parent's children,
  so a surface drew two folders for one key.
- [`FormaExplorer`](docs/explorer-design.md#the-parts)'s sidebar is the data view's tree over its folders, with All
  Media first among them, and chooses where the reader is without selecting: a folder is selected in the content,
  where it is listed beside the files, and the sidebar's rows draw no selection box. Every folder holding folders
  still starts open, and a folder's count, actions, drag and drop are where they were. `IExplorerSidebarView`,
  `IExplorerSidebarInteraction` and `FormaExplorerFolderNode` are gone, and a stylesheet addressing the sidebar's
  `.forma-explorer-folder-row`, `.forma-explorer-folder-target`, `.forma-explorer-folder-twisty` or
  `.forma-explorer-folder-name` finds the tree's `.forma-data-tree-node`, its disclosure and
  `.forma-data-tree-node-name`.
- [`FormaExplorer`](docs/explorer-design.md#the-parts) shows its content through a `FormaDataView`: a location lists
  its child folders before its files, All Media leading with the top-level folders; its pager, its Load More action
  and its status are the data view's, under `.forma-explorer-footer`, the action staying beside the watch of the end
  where `InfiniteScroll` is `Automatic`; and a page appended raises no `OnPageChanged`. A move into another folder
  clears the selection, and the history records the move only once it lands, so Back or Forward onto a folder that
  can't be opened leaves the history where it was. It listed files alone, kept the selection across folders, and drew
  its own `.forma-explorer-pagination`, `.forma-explorer-load-more-button`, `.forma-explorer-scroll-sentinel` and
  `.forma-explorer-scroll-status`, which a stylesheet addressing them now finds as the data view's `.forma-pagination`,
  `.forma-data-view-more` and `.forma-data-view-status`. `IExplorerContentView.Paging`, `ExplorerPagingInfo`, and
  `IExplorerContentInteraction.RequestNextPageAsync` and `ChangePageAsync` are gone, the paging being the data view's.
- A [`FormaFreezePane`](docs/component-catalog.md#formafreezepane) measures its frozen region again when a frozen part
  moves by a change of its style or class, as a grid's frozen columns do when a selection turns on; it kept the region
  it had measured, so the content came to rest beneath the frozen columns.
- [`FormaExplorer`](docs/explorer-design.md#the-parts)'s selection holds files and folders together, and `OnDelete`,
  `OnDrop` and `OnRename` each take files and folders alike. A deletion or a drop is validated whole before it is
  raised, and refused whole, `OnRejected` naming the item and `CatalogRejectionReason.NotPermitted` where it may not be
  deleted or moved, or `FolderCycle` where a folder would move into itself or a folder beneath it; a selected folder
  takes the selected folders beneath it with it. `CatalogDeleteRequest`, `CatalogDropRequest` and
  `CatalogRenameRequest` are classes a host answers with `Fail()` where it could not carry the request out, which keeps
  the selection. `OnFolderDelete`, `OnFolderDrop` and `OnFolderRename` are gone with `CatalogFolderDeleteRequest`,
  `CatalogFolderDropRequest` and `CatalogFolderRenameRequest`: a host handles a folder in `OnDelete`, `OnDrop` or
  `OnRename`, telling it apart by `ICatalogFolderItem`, and reads `Items` and `TargetFolder` or `Item` and `NewName`
  where it read `Folder`, `SourceFolder` and `TargetFolder`. `CatalogSelection` holds both kinds, `SelectionLimit`
  counting them together. Choosing a folder cleared the files selected and the reverse, a drop of a selection skipped
  the items that couldn't move, and several folders deleted raised one callback each.
- [`FormaExplorer`](docs/explorer-design.md#the-parts)'s list is the data view's list at its dense step, the names
  flowing into as many columns as the width holds and the page growing downward, an item's actions leading its
  thumbnail and the selection mode's box taking their place; the rows take the focus as the thumbnails do. The list
  scrolled further columns sideways behind arrows. `FormaExplorerListView` is gone, and a stylesheet addressing its
  `.forma-explorer-list-item` or `.forma-explorer-list-item-name` finds the list's `.forma-data-list-item` and
  `.forma-data-list-item-name`.
- [`FormaExplorer`](docs/explorer-design.md#the-parts)'s details are the data view's grid, the actions and name
  columns frozen as the others scroll, and its sort is the data view's: a column's header and the toolbar's sort
  menu set one sort, which the header marks and the menu shows. In the selection mode the grid's box column stands
  where the actions column stood, and the rows take the focus as the thumbnails do. `FormaExplorerDetailsView` is
  gone, and a stylesheet addressing the table's `.forma-explorer-details-row` or `.forma-explorer-details-table`
  finds the grid's `.forma-data-grid-row` and `.forma-data-grid`.
- [`FormaExplorer`](docs/explorer-design.md#the-parts)'s thumbnails are the data view's cards at their compact density,
  each the thumbnail and the name. They hold one tab stop with the selection mode off, the arrows moving the focus
  alone, a press or Enter opening a folder or previewing a file; in the selection mode a press or Space checks a
  card's box and Enter still opens it, the focus staying on its card as the mode changes. Their group headings and
  folding are the data view's, a group folded in one view staying folded in the others. A file's size no longer
  shows beneath its name, and a tile was a button the keyboard reached one by one. `FormaExplorerThumbnailsView` is
  gone, and a stylesheet addressing the tiles' `.forma-explorer-item`, `.forma-explorer-item-name` or
  `.forma-explorer-thumbnail-wrapper` finds the cards' `.forma-data-card`, `.forma-data-card-name` and
  `.forma-data-card-thumbnail`.
- On a statically rendered page, [`FormaNumberInput`](docs/component-catalog.md#formanumberinput),
  `FormaCurrencyInput`, `FormaDatePicker`, `FormaTimePicker` and `FormaDateTimePicker` are edited in the browser's own
  number, date, time and date-and-time inputs, and `FormaSelect` and `FormaMultiSelect` in the browser's own list,
  each posting its value in the invariant form Blazor's form binding reads, whatever culture the page renders in, as
  [the render modes](docs/component-foundations-design.md#render-modes) set out. `FormaDateRangePicker` posts its
  committed range, and a `FormaDateTimePicker` bound to a `DateTimeOffset` shows its value and posts it as it
  stands. They drew a disabled trigger or a field with no name, so a plain form sent none of their values.
- [`DateRange`](docs/component-catalog.md#formadaterangepicker) reads and writes itself as ISO 8601's interval of its
  two dates, `2026-03-14/2026-03-20`, the same in every culture, as `IParsable<DateRange>` and `IFormattable`, so
  Blazor's form binding reads a posted range back.
- [`FormaDatePicker`](docs/component-catalog.md#formadatepicker) and `FormaDateRangePicker` take `Disabled`, as the
  other pickers do. Their trigger's own `disabled` overrode one a host passed, so neither could be disabled.
- [`FormaTextBox`](docs/component-catalog.md#formatextbox), `FormaPasswordBox`, `FormaTextArea`, `FormaColorPicker`,
  `FormaCheckBox`, `FormaToggleSwitch` and `FormaSlider` post their values from a plain form rendered statically,
  under the name Blazor's own inputs write for the bound value, which the server binds back. They wrote no name, so a
  browser sent none of their values.
- A [`FormaWindow`](docs/component-catalog.md#formawindow) minimized as it opens, or right after a press or focus
  inside it, stays minimized. The activation that opening, the press or the focus asked for applied after the
  minimize and restored the window.
- [`WindowManager.ActivateVisibleAsync`](src/Framework/Forma.Framework.Windows/README.md#operations) makes a window
  current while it is visible and leaves a hidden one as it is, as a window's own press and focus do.
- A Blazor Hybrid page reloaded while a [`FormaWindow`](docs/component-catalog.md#formawindow) was being activated
  starts again. The window's teardown waited for the activation, which waited on a script call the old page would
  never answer.
- A call on a [`JSInterop`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) wrapper's
  attached instance still in flight as the wrapper detaches or is disposed reports that it didn't run.
- A [`FormaComposer`](docs/component-catalog.md#formacomposer)'s rich text editor rendered statically, or prerendered,
  shows its document and takes no typing. It showed an empty canvas that took typing nowhere. Its disabled and read-only
  states reach assistive technology as `aria-disabled="true"` and `aria-readonly="true"`, which a bare attribute
  didn't.
- An [`InputProfile`](src/Framework/Forma.Framework.Input/README.md#what-an-action-carries) takes `ItemAttribute`,
  and the `InputInfo` an action carries names, as `Item`, that attribute's value on the nearest element under the
  input that holds it, so an owner learns which item of a list a press landed on.
- A press on a row of a sidebar [`FormaDataTree`](docs/data-view-design.md#the-tree) chooses the node pressed. A grid
  beside the tree that drew twice on its own retired the tokens the tree's rows carried, so the press, and the report
  of the focus it moved, named a node the tree no longer knew and chose nothing; and a press whose focus report
  reached the tree after it chose the node before. `IDataViewInteraction.BeginItemPass` and
  `IDataView.GetItemToken` take the view drawing the items, whose passes retire only the tokens it drew.
- A text field whose edit the [input router](docs/input-router-design.md#a-fields-pending-edit) commits ahead of
  an action it routes reports its change once. The browser reported the same edit again as the field lost the
  focus, so a field's change handlers, `OnChange` among them, ran twice for one edit.
- A `Try*` call on a [`JSModule`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper)
  whose import was still under way as the module was disposed reports that it didn't run. The release could run
  first, and the call then threw `ObjectDisposedException` into the component that made it.
- A [culture switch](docs/culture-design.md#blazor-hybrid) made while a Blazor Hybrid web view's page was starting
  stands. The page took the culture it started in for a switch of its own and switched the application back to it.
- [`IsCultureFollowing`](src/Framework/Forma.Framework.Culture.Blazor.WebView/README.md#culture-routing) tells only
  the navigation that moves a web view's address into the application's culture. It also told Back or Forward onto
  the page that navigation landed at, so a handler guarding the user's work let that traversal through.
- A web view that [follows](docs/culture-design.md#culture-addresses-across-web-views) two culture switches in quick
  succession stays in the later one. The earlier switch's address, landing after the later switch, switched the
  application back.
- A navigation a web view makes to another culture's
  [address](docs/culture-design.md#culture-addresses-across-web-views) after its navigation handler refused Back or
  Forward switches the application. It took the application's culture instead, as a traversal does.
- A [`FormaSearchBox`](docs/component-catalog.md#formasearchbox) with a `DebounceDelay` raises `OnSearch` on the
  renderer under Blazor Server and in a Blazor Hybrid web view. It raised it on a timer's thread, where the search's
  render failed and ended the page's session.
- A day the keyboard focuses in `FormaCalendar`, the calendar
  [`FormaDatePicker`](docs/component-catalog.md#formadatepicker),
  [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker) and
  [`FormaDateRangePicker`](docs/component-catalog.md#formadaterangepicker) open, is drawn as any day the reader can
  choose. It was drawn as faintly as an unavailable day, its focus ring with it.
- A [composer](docs/component-catalog.md#formacomposer) dialog's confirming action waits for the text its fields are
  still receiving, such as a long inline picture's address. It went on with what had arrived, so the image dialog
  asked for an address already entered.
- A field that leaves its form while its text can't follow the reader's edits takes with it the message
  [the edit boundary](docs/browser-transfer-design.md#the-edit-boundary) shows on it. The message stayed in the form,
  which then never validated.
- A [`FormaComposer`](docs/component-catalog.md#formacomposer) whose editor's text can't follow the reader's edits
  says so on its field, as the text inputs do, and takes it off once it can. Its commands and its form's actions
  stopped without a word.
- [`IComposerInteraction`](src/Framework/Forma.Framework.Composer.Extensibility/README.md#what-a-plugin-can-ask-for)
  takes `ReportTextFault`, through which an editor shows on the composer's field that its text can't follow the
  reader's edits.
- A [`FormaNumberInput`](docs/component-catalog.md#formanumberinput)'s arrow keys step from the number the reader
  typed, and a [`FormaSearchBox`](docs/component-catalog.md#formasearchbox)'s Enter searches the text typed, where
  that text is still on its way as the key is pressed. They read the text that had arrived.
- A form's actions wait for the time a [`FormaTimePicker`](docs/component-catalog.md#formatimepicker) or a
  [`FormaDateTimePicker`](docs/component-catalog.md#formadatetimepicker) is still receiving, and the picker says on
  its field when its text can't follow the reader's edits, as the text inputs do. The form went on with the time
  that had arrived, and the picker stopped without a word.
- A text field whose [edit](docs/browser-transfer-design.md#edits) was lost on its way follows the reader again once
  the application writes a text of its own into it, and an edit still arriving as the write lands gives way to it.
  The field kept saying its text couldn't follow the reader's edits, and its form refused its actions, until the
  reader edited it again. The late edit was applied over the text written.
- A text field with a maximum length, holding a longer text the application gave it, takes its whole text again
  [through the transfer](docs/browser-transfer-design.md#the-transfer) after an edit was lost on its way. It refused
  that text as longer than it accepts, and its text couldn't follow the reader's edits.
- [`JSTransfer.ReadTextAsync`](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) reads a
  streamed value other than a text as `null`, as it reads one carried inline. It threw an
  `InvalidOperationException`.
- A text field drawn anew, or taken off its page and drawn again, goes on from the
  [edits](docs/browser-transfer-design.md#edits) it sent before and holds the text the reader left in it. Where its
  old element had already left the page, as it can on a slow Blazor Server connection, it numbered its edits afresh,
  so an edit could be dropped or applied to the wrong text, and text not yet sent was lost.
- A form's actions wait for the hex value a [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker) is still
  receiving, and the picker says on its field when its text can't follow the reader's edits, as the text inputs do.
- A [`FormaColorPicker`](docs/component-catalog.md#formacolorpicker) holding text that isn't a hex color says so by
  its field's name, in the reader's language. It said so in English, quoting the text.
- The [culture state](docs/culture-design.md#one-culture-state-per-user-scope) notifies its subscribers once the call
  that switched it has returned. Under Blazor Hybrid the application's culture coordinator was held while they ran, so
  a subscriber waiting on another native window's thread stopped that window's web view from starting or switching.
- [`Forma.AddOns.Operations.Maui`](src/Framework/Forma.Framework.Browser.Maui/README.md#what-the-reader-gets) keeps
  the file it saved last alone in the application's cache. Each print and download left its document there.
- [`INativeOperations`](src/Framework/Forma.Framework.Browser.Interop/README.md#operations) takes `CanSave`, whether
  the host keeps a file through its own surface, and `Print` composes the document it hands the host, as `Download`
  hands it a file, only where the host does. An implementation of its own adds `CanSave`, true where its
  `TrySaveAsync` keeps a file.

## `1.0.0-preview.4`

*November 7, 2026*

- [`FormaPageHead`](src/Blazor/Forma.Blazor.Server/README.md#the-document-head) writes the document's one title:
  the one a page sets with `PageTitle`, or `Title` where the page sets none. It wrote `Title` ahead of the page's
  own, and browsers and search engines showed that one.
- [`FormaPageHead`](src/Blazor/Forma.Blazor.Server/README.md#the-document-head) takes `PageRenderMode`, the render
  mode a host gives `Routes`, which its head outlet renders in, so a page's title and head content follow the page
  once it's interactive.
- A Blazor Hybrid page reloaded while its components were still
  [loading their scripts](src/Framework/Forma.Framework.JavaScript.Interop/README.md#writing-a-wrapper) starts again.
  The old page's components waited, as they were released, on a script the old page would never load, and the new
  page never started.

## `1.0.0-preview.3`

*November 7, 2026*

- Every [labeled field](docs/component-catalog.md#formafield) centers its label, while the label rests in the box, on
  the box itself, and so does what sits over the box, such as
  [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox)'s reveal toggle. Inside a form, where a field keeps
  room for a message beneath the box, they sat at its bottom edge.
- [`FormaField`](docs/component-catalog.md#formafield) takes `FooterContent`, the content beneath its box and ahead of
  its messages, such as [`FormaPasswordBox`](docs/component-catalog.md#formapasswordbox)'s strength meter.

## `1.0.0-preview.2`

*November 6, 2026*

- [`FormaPagination`](docs/component-catalog.md#formapagination) stays within the width it's given in every
  language: its navigation buttons show their glyphs only where the glyphs stand in for the words, and where a
  language's words need more room than English's, the page box or the steps give way.

## `1.0.0-preview.1`

*November 5, 2026*

- The first release of Forma's packages, each described by its README, with what Forma held at the time in
  [the features status](docs/features-status.md) and [the catalog](docs/component-catalog.md).
