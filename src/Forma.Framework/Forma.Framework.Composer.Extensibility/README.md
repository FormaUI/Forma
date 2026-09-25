# Forma.Framework.Composer.Extensibility

The contracts a `FormaComposer` plugin is written against: what a plugin contributes, what it
reads of the composer while it runs, and what it can ask the composer to do. A package that
extends the composer — an editing surface, a capability, a read-only view over a document —
compiles against this one alone and never against the engine behind it.

Everything here is free of Blazor, so a plugin's contracts and its tests need no renderer. A
plugin that draws something surfaces a `Type` and the composer hosts it.

The engine that reads these contracts ships inside `Forma.Blazor.Composer`, so a host
installs that for a working composer and a plugin author installs this to write one.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Composer.Extensibility
```

The package has no registrar and constructs nothing. A host registers plugins with the composer;
this package only describes them.

## The plugin families

Every plugin is an `IComposerPlugin` — an `IPlugin` from `Forma.Framework.Plugins`, which
the composer's registry is built on — and one of three kinds.

| Type | What it is |
| --- | --- |
| `IComposerEditorPlugin` | One way of authoring — rich text, Markdown, code — naming the document format it reads and writes and the component that fills its canvas. |
| `IComposerFeaturePlugin` | A capability around the editors rather than an editor of its own: a formatting command, a dialog, an upload. |
| `IComposerViewPlugin` | A read-only way of looking at an editor's document, such as a rendered preview or its source, with a command bar of its own. |

### `IComposerPlugin`

| Member | What it does |
| --- | --- |
| `Id` | The plugin's identifier, from `IPlugin`, unique among the plugins registered beside it. |
| `LocalizationSourceType` | The type whose locale files hold the plugin's titles and labels. The composer reads a title from them first and from its own after, so a plugin from another package ships its strings with it; null, the default, reads the composer's alone. |
| `Items` | The toolbar entries it contributes, each naming the row it lands on. |
| `Rows` | Command rows it declares of its own, laid out after the composer's. |
| `InputTriggers` | Typed-input triggers, so typing a sequence runs a command. |
| `CommandPrefixes` | Prefixes of commands whose full form cannot be listed ahead of time, such as one per entry a picker offers. |
| `Assets` | Browser assets the plugin needs before it can run. |
| `ActiveStates` | The identifiers of its own toggles and options that are currently on. |
| `IsAvailableAsync` | Whether every dependency the plugin needs is present; a plugin answering `false` is left out. |
| `ExecuteAsync` | Runs one of the commands the plugin named. |

A plugin also carries key bindings through `IComposerKeyBindingSource`, which the composer merges
with its own and with every other plugin's.

### Where a contribution applies

A plugin never declares where it applies: the host says that by where it registers the plugin.
Registered on the composer, a plugin reaches every editor and every view; registered on an
editor, it reaches that editor's surface alone and not the views over its document; registered
on a view, it reaches that view's bar. So a plugin shipped from another package need not name an
editor whose type it cannot reference, and the same plugin type serves two editors as two
instances, each with its own options.

## What a plugin contributes

| Type | What it is |
| --- | --- |
| `ComposerToolbarItem` | A row identifier and the `ToolbarItem`s to put on it, so a plugin contributes toolbar entries as data rather than markup. |
| `ComposerToolbarRow` | A row a plugin declares of its own: an identifier and the label it is announced by. |
| `ComposerRows` | The rows the composer provides — `BuiltIn`, `Plugins`, `Formatting`, `Paragraph`, `Selection`. |
| `ComposerKeyBinding` | A shortcut: the key, the modifiers, the command, and the display and ARIA spellings of it. |
| `ComposerInputTrigger` | Text that runs a command when typed. |
| `ComposerAsset` | A browser asset the plugin needs: a `Module`, a `Stylesheet` or a `Font`, loaded on demand or eagerly. |
| `ComposerViews` | The view kinds the composer knows — `Preview` and `Source`. |

## What a plugin reads

`ComposerContext` is cascaded to every plugin component and rebuilt whenever any of it changes,
so a changed context is also the signal to re-render.

| Member | What it is |
| --- | --- |
| `ActiveEditor` | The editor plugin currently rendering. |
| `ActiveView` | The view over that editor's document currently showing, or null while the editor itself is. |
| `Document` | The document as the composer currently holds it. |
| `Selection` | The caret and range as the browser last reported them. |
| `IsDisabled`, `IsReadOnly`, `IsMaximized`, `IsSelectionBarShown` | The composer's chrome state. |
| `History` | The undo and redo stack shared by every editor. |
| `Sanitizer` | The sanitizer every path into and out of the composer runs through. |
| `AssetLoader` | The loader for the assets plugins declare. |
| `Regions` | The status items and gutter markers plugins have pushed. |
| `SyntaxHighlighter` | The highlighter code classification runs through. |
| `KeyBindings` | The shortcuts the composer and its plugins answer. |

An editor's component is also handed a `ComposerSelectionBar`: the items plugins placed on the
selection row, already in the reader's language, and the key bindings their shortcuts are read
from. An editor with a text selection draws them over it; one without ignores them.

## What a plugin can ask for

`IComposerInteraction` is implemented by the composer and cascaded beside the context.

| Member | What it does |
| --- | --- |
| `ChangeDocumentAsync` | Reports a document the editor has changed, so the engine records, sanitizes and raises it. |
| `ReportSelectionAsync` | Reports the caret or range an editor observed, so the engine shares it with every plugin. |
| `ExecuteCommandAsync` | Runs a named command, resolved against the engine's own commands first and the plugins after. |
| `RequestRenderAsync` | Asks the composer shell to re-render. |
| `OpenDialogAsync` | Opens a dialog the plugin describes, inside the composer's own shell. |
| `TryInsertAsync` | Hands an insertion to the active editor, reporting whether it took one of that kind. |

`ComposerCommands` names the commands the engine owns: `Undo`, `Redo`, `Maximize`,
`SelectionBar` and `Attach`.

## Dialogs and insertion

A plugin that collects something from the author supplies a dialog **body** and the composer
supplies the shell, so a plugin shipped from another package gets the same modal as a built-in one
without referencing the component that draws it.

| Type | What it is |
| --- | --- |
| `ComposerInsertionRequest` | Which component fills the dialog, the parameters it is handed, and how much room it gets. |
| `IComposerInsertionDialog` | What the body answers: its `Title`, an optional `Notice` while it can do nothing, the `Messages` confirming refused, and `TryConfirmAsync`. |
| `ComposerInsertion` | What goes into the document. `ResourceInsertion` carries a stored file's `Source`, `Name` and `Kind`, and whether it is embedded, linked, or both. |
| `MediaInsertMode` | `Embed`, `Link` or `Both`, for a file the browser can render. |

The body resolves its own title, notice and messages, because those strings ship with the body
rather than with the composer hosting it. Confirming asks the body for an insertion and hands it to
the active editor through `IComposerEditorPlugin.TryInsertAsync`, which an editor overrides for the
insertions it understands and which answers false by default. An editor that takes none of that kind
answers false and the dialog stays open saying so; that answer is the whole compatibility check,
since an editor never declares what it accepts anywhere else.

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

## The document

| Type | What it is |
| --- | --- |
| `ComposerDocument` | Content together with the format it is written in, and the `Attachments` it carries beside the content the way an email carries its files. |
| `ComposerDocumentFormat` | A format, compared case-insensitively by its identifier. `Html`, `Markdown` and `PlainText` are provided, and a new format costs a caller nothing but a new identifier. |
| `IComposerDocumentConverter` | Converts content from one format to another, reporting through `ComposerConversionResult` what it could not represent. |

Binding a whole `ComposerDocument` rather than a string is what lets a Markdown author's source
survive a round trip: the format travels with the content, so nothing is rewritten through HTML
on the way past.

## Regions, history and highlighting

| Type | What it is |
| --- | --- |
| `IComposerRegionRegistry` | Where a plugin pushes a status item or a gutter marker, and the `Changed` event the composer redraws on. |
| `IComposerStatusItem` | A handle to one status readout; a plugin mutates its `Text`, `Tooltip` and `Command` and disposes it when done. |
| `IComposerGutterMarker` | The same for a marker beside a line. |
| `IComposerHistory` | The undo and redo stack: `Record`, `Undo`, `Redo`, `Clear`, with `CanUndo` and `CanRedo`. |
| `IComposerSyntaxHighlighter` | Classifies code into `ComposerSyntaxToken`s for the languages it declares. |
| `IComposerAssetLoader` | Loads the browser assets a plugin declared, so a capability that needs a script or a stylesheet gets one before it runs. |

A status item and a gutter marker are pushed rather than polled, and the handle is disposed when
the plugin is done with it, the way VS Code's own status items behave.

## Example

A feature plugin contributing one toolbar command, which counts the words in the document
source and puts the count on the status strip:

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

            // The handle is kept, because disposing it is what takes the readout off the strip.
            _status ??= context.Regions.CreateStatusItem(text);
            _status.Text = text;
        }

        return ValueTask.CompletedTask;
    }

    public void Dispose()
        => _status?.Dispose();
}
```

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
