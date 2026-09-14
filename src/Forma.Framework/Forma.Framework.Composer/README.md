# Forma.Framework.Composer

The vocabulary `FormaComposer` speaks: the document and the format it is written in, the plugin
contracts every editor, view and capability arrives through, the toolbar items, key bindings and
commands a plugin contributes, the context it reads and the interaction it acts through, the
converters that carry a document between formats, and the regions, assets, syntax and widgets a
plugin can reach. A host writes its own plugins and converters on these contracts.

The assembly ships inside `2Site.Forma.Blazor.Pro.Composer`; nothing is installed or registered
for it. It references nothing from ASP.NET Core, so a feature plugin or a converter is written in
a plain class library. How the component itself is consumed — its parameters, the plugins it
ships and the registrations it reads — is in
[`Forma.Blazor.Composer`](../../Forma.Blazor.Pro/Forma.Blazor.Composer/README.md).

## Namespaces

| Namespace | What it holds |
| --- | --- |
| `Forma.Framework.Composer.Documents` | `ComposerDocument` and `ComposerDocumentFormat`; `IComposerDocumentConverter`, `ComposerConversionResult` and the `DocumentConverterRegistry` converters are held in; `HtmlSourceFormatter`. |
| `.Plugins` | The plugin contracts — `IComposerPlugin`, `IComposerEditorPlugin`, `IComposerFeaturePlugin`, `IComposerViewPlugin`, `IComposerDesignerPlugin`, `IComposerKeyBindingSource` — and what a plugin contributes: `ComposerToolbarItem`, `ComposerToolbarRow`, `ComposerKeyBinding`, `ComposerInputTrigger`; the `ComposerViews` kinds; `ComposerPluginCatalog` and `ComposerPluginDiscovery`. |
| `.Editing` | What a plugin acts through — `ComposerContext`, `IComposerInteraction`, `ComposerSelection` — the engine's `ComposerCommands` and `ComposerRows`, the history, the key binding catalog, the region registry with its status items and gutter markers, the `ComposerText` operations, and the command registry and toolbar layout. |
| `.Assets` | `ComposerAsset`, its kind and loading, and `IComposerAssetLoader`. |
| `.Syntax` | `IComposerSyntaxHighlighter`, `ComposerSyntaxToken`, and `NullSyntaxHighlighter`. |
| `.Widgets` | `ComposerWidget`, `ComposerWidgetNode` and `ComposerWidgetTree`: the widgets a designer places, and the tree they are saved as. |

## The document

`ComposerDocument(Format, Content)` is the content together with the format it is written in, and
`Attachments`, the `ResourceInfo`s carried beside the content rather than written into it.
`ComposerDocumentFormat` is a format named by a lowercase identifier and compared
case-insensitively; `Html`, `Markdown`, `PlainText` and `WidgetTree` are declared on it, and
`new ComposerDocumentFormat("…")` names a format of the host's own. `HtmlSourceFormatter.Format(html)`
lays markup out one block per line, indented by nesting, for reading rather than rendering.

## The plugin contracts

Every capability is an `IComposerPlugin`, and a plugin is one of four kinds by the interface it
adds:

| Kind | Adds | What it is |
| --- | --- | --- |
| `IComposerEditorPlugin` | `Title`, `Icon`, `Format`, `EditorComponent` | One way of authoring: the component filling the canvas, and the format it reads and writes. |
| `IComposerFeaturePlugin` | `SupportedEditorIds`, `AppliesToViews` | A capability around the editors, scoped to the editors it names or to every editor when it names none. `AppliesToViews` puts its items on a view's bar as well, for a command that acts on the surface rather than the document. |
| `IComposerViewPlugin` | `EditorId`, `Kind`, `Title`, `Icon`, `ViewComponent` | A read-only way of looking at one editor's document, of kind `ComposerViews.Preview` or `ComposerViews.Source`, with a command bar of its own. |
| `IComposerDesignerPlugin` | `Widgets` | The widgets it contributes to a designer's palette. |

A `Title` is written in English and is the localization key.

### What every plugin has

| Member | What it is |
| --- | --- |
| `Id` | The unique identifier. |
| `Items` | `ComposerToolbarItem`s, each naming the row its `FormaToolbarItem`s land on. |
| `Rows` | `ComposerToolbarRow`s of the plugin's own, an `Id` and an English `Label`, laid out after the rows the composer provides. |
| `KeyBindings` | `ComposerKeyBinding`s, from `IComposerKeyBindingSource`. |
| `InputTriggers` | `ComposerInputTrigger`s: the `Text` watched for as the author types, and the `Command` it fires. |
| `CommandPrefixes` | Prefixes of commands the plugin owns whose full form is not enumerable ahead of time, such as one per entry a picker offers. |
| `Assets` | `ComposerAsset`s the plugin needs before it can run: a `Module`, a `Stylesheet` or a `Font` at a `Url`, fetched `Eager` when the plugin is registered or `OnDemand` when it is first activated. |
| `ActiveStates` | The identifiers of the plugin's toggles and options currently active. |
| `IsAvailableAsync` | Whether every dependency the plugin needs is present; a plugin answering false is left out. |
| `ExecuteAsync(command, context, interaction)` | Runs one of the commands the plugin named. |

On `IComposerPlugin` every member but `Id` and `IsAvailableAsync` has a default, so a plugin
writes only what it contributes.

**Items and rows.** An item is a `FormaToolbarItem` from
[`Forma.Framework.Toolbar`](../Forma.Framework.Toolbar/README.md), and each `ComposerToolbarItem`
names the row its items land on: one of `ComposerRows` — `BuiltIn`, `Formatting`, `Paragraph`,
`Plugins`, and `Selection`, the bar shown over a text selection — or a row the plugin declares in
`Rows`. On an editor's toolbar an item naming a row nobody declared lands on `Plugins`; on a
view's bar it is left out. Groups sharing an `Id` on one row fold into one, so several plugins
fill one named cluster.

**Commands.** A command is a string. Every command an item, a menu option, a key binding or an
input trigger names is routed to the `ExecuteAsync` of the plugin that named it, and a command
starting with one of a plugin's `CommandPrefixes` to that plugin. `ComposerCommands` names the
commands the composer runs itself — `Undo`, `Redo`, `Maximize`, `SelectionBar` and `Attach` —
which a plugin places on an item as it would its own.

**Key bindings.** A `ComposerKeyBinding` is a `Key` with `Control`, `Alt` and `Shift`, bound to a
`Command`; `Display` writes it for a tooltip and `AriaKeyShortcuts` for `aria-keyshortcuts`.
`ComposerKeyBindingCatalog` merges every source's bindings, and `FindByCommand` answers the
earliest source's binding for a command bound more than once.

## What a plugin acts through

`ComposerContext` is the composer as a plugin reads it:

| Member | What it is |
| --- | --- |
| `ActiveEditorId` | The identifier of the editor rendering. |
| `Document` | The document as the composer holds it. |
| `Selection` | The caret and range as the browser last reported them: `Start`, `End`, `IsCollapsed`, the `BlockType` the caret sits in, and the `ActiveStyles` across the range. |
| `IsDisabled`, `IsReadOnly`, `IsMaximized`, `IsSelectionBarShown` | The composer's state. |
| `History` | The `IComposerHistory` shared by every editor: `Record`, `Undo`, `Redo` and `Clear`, with `CanUndo` and `CanRedo`. |
| `Sanitizer` | The `IHtmlSanitizer` every path into and out of the composer runs through. |
| `AssetLoader` | The `IComposerAssetLoader` for the assets plugins declare, loading each once per circuit. |
| `Regions` | The `IComposerRegionRegistry` of status items and gutter markers. |
| `SyntaxHighlighter` | The `IComposerSyntaxHighlighter` code classification runs through. |
| `KeyBindings` | The `ComposerKeyBindingCatalog` merged from every source. |

`IComposerInteraction` is what a plugin asks of the composer:

| Member | What it does |
| --- | --- |
| `ChangeDocumentAsync(document)` | Reports a changed document, which the composer records, sanitizes and raises. |
| `ReportSelectionAsync(selection)` | Reports the caret or range an editor observed. |
| `ExecuteCommandAsync(command)` | Runs a named command through the composer. |
| `RequestRenderAsync()` | Asks the composer to re-render. |

`ComposerText` operates on a document's raw content: `GetWordAt`, `GetSelected` and `GetLines`
answer `ComposerTextSpan`s, and `Replace` and `ReplaceAll` answer a `ComposerEdit` of the new
content and the selection to restore once it is applied.

**Regions.** `IComposerRegionRegistry.CreateStatusItem(text, tooltip, command)` pushes a readout
onto the status strip and `CreateGutterMarker(line, icon, tooltip, command)` a marker beside a
line. Each returns a handle, an `IComposerStatusItem` or an `IComposerGutterMarker`, whose
members stay mutable for as long as it is held and which is disposed to remove it. Every member
is called from the composer's own dispatcher thread.

**Syntax.** `IComposerSyntaxHighlighter.Tokenize(code, language)` classifies code into
`ComposerSyntaxToken`s of `Start`, `Length` and `Scope`, for the `SupportedLanguages` it names. A
host registers one and `FormaComposer` takes it; `NullSyntaxHighlighter.Instance` stands in where
none is registered.

## Converters

`IComposerDocumentConverter` carries content from its `Source` format to its `Target`:
`Convert(content)` answers `ComposerConversionResult.Success(content)` or
`Refused(unsupportedFeatures)`, naming what the target format cannot represent. A host registers
each as `IComposerDocumentConverter`. `DocumentConverterRegistry` holds them keyed by the pair of
formats, a later converter replacing an earlier for the same pair, and its
`Convert(document, targetFormat)` answers the document's own content when the formats match.

## The catalog and discovery

`ComposerPluginCatalog.CreateAsync(candidates, fallbackEditor)` keeps the first plugin of each
`Id` that reports itself available, sorted into `Editors`, `Views` and `FeaturePlugins`, and takes
the fallback editor when no editor remains. `ResolveEditor(requestedId, defaultId, format)` picks
the editor to activate, preferring an explicit request, then a configured default, then the
document's format, and `GetViewsForEditor(editorId)` the views belonging to one editor.

`ComposerPluginDiscovery.Discover(plugins, assembly, locations, logger)` builds every public
feature and view plugin in an assembly — or in every loaded assembly referencing this one — and
appends them after the plugins given; `locations` are namespace suffixes narrowing the search. A
found plugin has one public constructor, and each of its parameters is satisfied by the first
given plugin of that type or by the parameter's default. A plugin whose type is among those given
is not built again, and one that takes what no given plugin can stand in for is constructed by
the caller and passed among them.

## Widgets

`ComposerWidget` describes a widget a designer can place: `Id`, `Title`, `Category`, `Icon`, the
`Component` rendering it on the canvas, the `SettingsComponent` and `SettingsType` its settings
are edited and deserialized with, and whether it `IsContainer`. `ComposerWidgetNode` is one
placement — `InstanceId`, `WidgetId`, `Content`, `Settings` as JSON, and `Children` — and
`ComposerWidgetTree` the `Widgets` at the root with a `SchemaVersion`, the serialized tree the
`WidgetTree` format holds.

## Example

A feature plugin inserting the time at the caret, built against the plain text editor because it
edits the content as raw text; it contributes one command on the plugins row and a shortcut for
it, and hands the changed document and the caret back through the interaction:

```csharp
using System.Globalization;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

public sealed class TimeStampPlugin(IComposerEditorPlugin editor) : IComposerFeaturePlugin
{
    public const string InsertTimeCommand = "insert.time";

    public string Id => "time-stamp";

    public IReadOnlyList<string> SupportedEditorIds => [editor.Id];

    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new FormaToolbarCommand { Id = "time", Title = "Insert Time", Icon = Glyph.Clock, Command = InsertTimeCommand },
        ]),
    ];

    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
    [
        new() { Key = "t", Command = InsertTimeCommand, Control = true, Shift = true },
    ];

    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    public async ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command != InsertTimeCommand)
        {
            return;
        }

        var time = DateTime.Now.ToString("t", CultureInfo.CurrentCulture);
        var edit = ComposerText.Replace(context.Document.Content, context.Selection.Start, context.Selection.End, time);

        await interaction.ChangeDocumentAsync(context.Document with { Content = edit.Content });
        await interaction.ReportSelectionAsync(edit.Selection);
    }
}
```

The plugin is handed to `FormaComposer` beside the editor it was built against, as
`[plainText, new TimeStampPlugin(plainText)]`, or registered as `IComposerPlugin`.

A converter, registered so the composer can switch an editor authoring Markdown to one authoring
HTML:

```csharp
using Forma.Framework.Composer.Documents;

public sealed class MarkdownToHtmlConverter(IMarkdownRenderer renderer) : IComposerDocumentConverter
{
    public ComposerDocumentFormat Source => ComposerDocumentFormat.Markdown;

    public ComposerDocumentFormat Target => ComposerDocumentFormat.Html;

    public ComposerConversionResult Convert(string content)
        => ComposerConversionResult.Success(renderer.Render(content));
}
```

```csharp
builder.Services.AddSingleton<IComposerDocumentConverter, MarkdownToHtmlConverter>();
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md). The assembly ships inside
`2Site.Forma.Blazor.Pro.Composer`, which is licensed as Forma Pro under
[`Forma.Blazor.Pro/LICENSE.md`](../../Forma.Blazor.Pro/LICENSE.md).
