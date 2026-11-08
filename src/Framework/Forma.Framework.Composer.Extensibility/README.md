# Forma.Framework.Composer.Extensibility

The contracts a `FormaComposer` plugin is written against: what a plugin contributes, what it reads
of the composer while it runs, and what it can ask the composer to do. A package that adds an
editor, a capability or a view to the composer references this one. The contracts are plain .NET;
a plugin that draws something names its component as a `Type`, and the composer renders it.

## Install

```pwsh
dotnet add package formaui-net.Framework.Composer.Extensibility --prerelease
```

A host running the composer installs
[`formaui-net.Blazor.Composer`](https://github.com/FormaUI/Forma/blob/main/src/Blazor/Forma.Blazor.Composer/README.md),
which brings this package with it.

## Writing a plugin

A feature plugin contributing one toolbar command, which counts the words in the document and
puts the count on the status strip:

```csharp
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

public sealed class WordCountPlugin : IComposerFeaturePlugin, IDisposable
{
    public const string CountCommand = "word-count.show";

    private IComposerStatusItem? _status;

    public string Id => "word-count";

    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new ToolbarCommand
            {
                Id = "word-count",
                Title = "Word Count",
                Icon = Glyph.Count,
                Command = CountCommand,
            },
        ]),
    ];

    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (command == CountCommand)
        {
            var words = context.Document.Content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
            var text = $"{words} words";

            // Disposing the handle takes the readout off the strip.
            _status ??= context.Regions.CreateStatusItem(text);
            _status.Text = text;
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
        => _status?.Dispose();
}
```

The host registers it with the composer, and where it registers the plugin is where it applies:
on the composer it reaches every editor and view, under an editor that editor's surface, under a
view that view's bar.

```csharp
using Forma.Blazor.Components.Composer;

builder.Services.AddFormaComposer(composer => composer
    .AddRichTextEditor(editor => editor
        .TryAddDefaultFeatures()
        .AddFeaturePlugin<WordCountPlugin>()));
```

Each composer builds its own instance of every plugin, and disposes it with the composer. A
plugin's constructor takes the other plugins that composer built, such as the editor it works
on, the options record it declares through `IPluginConfigurable<TOptions>`, and the host's
services.

## The plugin kinds

Every plugin is an `IComposerPlugin`, an `IPlugin` from
[`Forma.Framework.Plugins`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Plugins/README.md),
and one of three kinds.

| Type | What it is |
| --- | --- |
| `IComposerEditorPlugin` | One way of authoring, such as rich text, Markdown or code: its `Title`, `Icon`, the `Format` it reads and writes, the `EditorComponent` filling its canvas, and `TryInsertAsync` for the insertions it takes. |
| `IComposerFeaturePlugin` | A capability around the editors, such as a formatting command, a dialog or an upload. |
| `IComposerViewPlugin` | A read-only way of looking at an editor's document, such as a rendered preview or its source, with a command bar of its own: its `Kind` from `ComposerViews`, `Title`, `Icon` and `ViewComponent`. |

`IComposerPlugin` carries what every kind contributes. Only `Id` and `IsAvailableAsync` are
required.

| Member | What it is |
| --- | --- |
| `Id` | The plugin's identifier, unique among the plugins registered beside it. |
| `IsAvailableAsync` | Whether every dependency the plugin needs is present; the composer leaves out a plugin answering `false`. |
| `ExecuteAsync` | Runs one of the commands the plugin named. |
| `Items` | The toolbar entries it contributes, as `ComposerToolbarItem`s naming the row each lands on. |
| `Rows` | Command rows of its own, as `ComposerToolbarRow`s. |
| `KeyBindings` | Its shortcuts, as `ComposerKeyBinding`s. |
| `InputTriggers` | Typed text that runs a command, as `ComposerInputTrigger`s. |
| `CommandPrefixes` | Prefixes of commands it owns, such as one per entry a picker offers. |
| `Assets` | Browser assets it needs, as `ComposerAsset`s: a `Module`, `Stylesheet` or `Font`, loaded `OnDemand` or `Eager`. |
| `ActiveStates` | The identifiers of its toggles and options that are on. |
| `LocalizationSourceType` | The type whose locale files hold its titles and labels, so a plugin from another package ships its strings with it. |

Titles and labels are English, and the composer localizes them. `ComposerRows` names the rows the
composer provides: `BuiltIn`, `Plugins`, `Formatting`, `Paragraph` and `Selection`.

## What a plugin reads

`ComposerContext` is the composer as a plugin reads it, handed to `ExecuteAsync` and cascaded to
every plugin component.

| Member | What it is |
| --- | --- |
| `ActiveEditor`, `ActiveView` | The editor rendering, and the view showing over its document. |
| `Document` | The document as the composer holds it. |
| `Selection` | The caret and range as the browser last reported them, with the block type and active styles. |
| `IsDisabled`, `IsReadOnly`, `IsMaximized`, `IsSelectionBarShown` | The composer's state. |
| `History` | The `IComposerHistory` undo and redo stack every editor shares. |
| `Sanitizer` | The HTML sanitizer every path into and out of the composer runs through. |
| `AssetLoader` | The `IComposerAssetLoader` that loads the assets plugins declare. |
| `Regions` | The `IComposerRegionRegistry` a plugin pushes status items and gutter markers to. |
| `SyntaxHighlighter` | The `IComposerSyntaxHighlighter` that classifies code into `ComposerSyntaxToken`s. |
| `KeyBindings` | The `ComposerKeyBindingCatalog` of shortcuts the composer and its plugins answer. |

A status item or gutter marker stays shown while its handle is held: a plugin changes the
handle's `Text`, `Tooltip` or `Command` and disposes it to remove it.

## What a plugin can ask for

`IComposerInteraction` is the composer, handed to `ExecuteAsync` and cascaded beside the context.

| Member | What it does |
| --- | --- |
| `ChangeDocumentAsync` | Reports a document the editor changed, which the composer records, sanitizes and raises. |
| `ReportSelectionAsync` | Reports the caret or range an editor observed. |
| `ExecuteCommandAsync` | Runs a named command, the engine's own first and the plugins' after. |
| `RequestRenderAsync` | Re-renders the composer. |
| `OpenDialogAsync` | Opens a dialog the plugin describes in the composer's own shell. |
| `TryInsertAsync` | Hands an insertion to the active editor, reporting whether it took it. |
| `JoinEdits` | Takes the edits of the editor in the surface, which the composer settles before a command, a form's action or a switch of editors reads the document, until the registration is disposed. |
| `ReportTextFault` | Shows on the composer's field that the editor's text can't follow the reader's edits, and takes it off once it can. |

`ComposerCommands` names the commands the engine runs: `Undo`, `Redo`, `Maximize`,
`SelectionBar`, `Attach` and `Shortcuts`.

## Dialogs and insertion

A plugin that collects something from the author supplies the body of a dialog, and the composer
supplies the shell. The body is a component implementing `IComposerInsertionDialog`: its `Title`,
a `Notice` while it can do nothing, the `Messages` saying what confirming refused, and
`TryConfirmAsync`, which answers the `ComposerInsertion` to put into the document. The composer
hands that to the active editor's `TryInsertAsync`, and the dialog stays open when the editor
refuses it.

```csharp
await interaction.OpenDialogAsync(new ComposerInsertionRequest
{
    Body = typeof(MyPickerDialog),
    Size = ComposerDialogSize.Large,
    Parameters = new Dictionary<string, object?>(StringComparer.Ordinal)
    {
        [nameof(MyPickerDialog.Options)] = Options,
    },
});
```

`ResourceInsertion` is the insertion of a stored file: its `Source`, `Name` and `Kind`, and
whether it is embedded, linked or both. `MediaInsertMode` names those three choices, `Embed`,
`Link` and `Both`, for a plugin's options.

## The document

| Type | What it is |
| --- | --- |
| `ComposerDocument` | The `Content` with the `Format` it is written in, and the `Attachments` carried beside it. |
| `ComposerDocumentFormat` | A format, compared by its identifier: `Html`, `Markdown`, `PlainText`, or a new one made with `new ComposerDocumentFormat("…")`. |
| `IComposerDocumentConverter` | Converts content from its `Source` format to its `Target`, answering a `ComposerConversionResult` that names what the target could not represent. |

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
