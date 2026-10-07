# Forma.Framework.Composer

The engine `FormaComposer` runs its plugins with: the description of a composer's plugins and the
builders that write it, the catalog that picks the editor and the plugins in play, the registry
that routes a command to the layer that runs it, the toolbar layout, the undo history, the status
items and gutter markers, text operations, and document conversion. It is plain .NET, and it ships
inside the `formaui-net.Blazor.Composer` package, whose
[README](https://github.com/FormaUI/Forma/blob/main/src/Forma.Blazor.Composer/README.md) covers
installing and using the composer. The contracts these types read, the plugin interfaces, the
document and the context, are
[`Forma.Framework.Composer.Extensibility`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Composer.Extensibility/README.md).

## Describing a composer

`ComposerDefinition.Create` describes a composer's plugins once, and `Activate` builds them for
one composer into a `PluginScope` that disposes them. A host writes the same description through
`AddFormaComposer`, or hands one composer its own through the `Plugins` parameter.

```csharp
using Forma.Framework.Composer.Plugins;

var definition = ComposerDefinition.Create(composer => composer
    .AddEditorPlugin<RichTextEditorPlugin>(editor => editor
        .AddFeaturePlugin<BasicFormattingPlugin>()
        .AddFeaturePlugin<TablePlugin, TablePluginOptions>(options => options.MaxRows = 12)
        .AddViewPlugin<RichTextPreviewViewPlugin>())
    .TryAddFeaturePlugin<MaximizePlugin>());

await using var plugins = definition.Activate(services);
```

| Builder | What it registers |
| --- | --- |
| `ComposerPluginBuilder` | The composer's editors with `AddEditorPlugin`, and with `AddFeaturePlugin` and `TryAddFeaturePlugin` the capabilities that reach every editor and view. |
| `ComposerEditorBuilder<TEditor>` | The capabilities on one editor's surface, and its views with `AddViewPlugin`. |
| `ComposerViewBuilder` | The capabilities on one view's bar. |

Each `AddFeaturePlugin` comes in three forms: by type, by type with the options record it
declares, and through a factory. The registry beneath the builders is
[`Forma.Framework.Plugins`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Plugins/README.md).

## Running one composer

```csharp
using Forma.Framework.Composer.Editing;

var catalog = await ComposerPluginCatalog.CreateAsync(plugins.Instances, new PlainTextEditorPlugin());
var editor = catalog.ResolveEditor(activeEditorId, options.DefaultEditorId, document.Format);

var commands = new ComposerCommandRegistry();
commands.RegisterEngineCommand(ComposerCommands.Undo, UndoAsync);
commands.RegisterPlugins(catalog.GetSurfacePlugins(editor));

var result = await commands.ExecuteAsync(command, context, interaction);
```

| Type | What the composer uses it for |
| --- | --- |
| `ComposerPluginCatalog` | The plugins that report themselves available, the first of each `Id` under each owner, with the fallback editor when no editor is available. `ResolveEditor` picks the editor to activate: the requested one, then the default, then the first for the document's format. `GetViews`, `GetFeaturePlugins` and `GetSurfacePlugins` answer what is in play on one surface. |
| `ComposerCommandRegistry` | Every command the composer can run. `ExecuteAsync` runs the engine's own commands first, then the plugin that named the command or its prefix, and answers `ComposerCommandResult.Engine`, `Plugin` or `Unhandled`. |
| `ComposerKeyBindingSource` | `Default` carries the engine's shortcuts: Ctrl+Z for undo, Ctrl+Y and Ctrl+Shift+Z for redo. |
| `ComposerToolbarLayout` | `Create` for an editor's toolbar and `CreateForView` for a view's bar lay contributions out as `Rows` of `ComposerToolbarRowLayout`, localized through the function they are given. `Coalesce` folds groups sharing an identifier into the first of them. |
| `ComposerHistory` | The `IComposerHistory` undo and redo stack, holding at most the number of states it is created with. |
| `ComposerRegionRegistry` | The `IComposerRegionRegistry` the status strip and the gutter draw from, raising `Changed` when an item or marker is created, changed or disposed. |
| `ComposerText` | Operations on a document's raw content: `GetWordAt`, `GetSelected` and `GetLines` answer `ComposerTextSpan`s, and `Replace` and `ReplaceAll` answer a `ComposerEdit`, the new content with the selection to restore. |
| `DocumentConverterRegistry` | The registered `IComposerDocumentConverter`s by the pair of formats: `Register`, `Find`, `CanConvert`, and `Convert`, which answers a `ComposerConversionResult`. |
| `HtmlSourceFormatter` | `Format` lays HTML out one block per line, indented by nesting, for the rich text editor's source view. |
| `NullSyntaxHighlighter` | `Instance` classifies no code, for a composer whose host registered no highlighter. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
