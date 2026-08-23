namespace Forma.UI.Blazor.Showcase.Components.Pages.Features;

using System.Globalization;

using Forma.UI.Blazor.Components.Composer.PlainTextEditor;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;
using Forma.UI.Style.Glyphs;

/// <summary>A source-oriented surface, numbering its lines in a gutter and renaming a symbol from a row of its own.</summary>
public sealed class SourceEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The command opening the rename bar over the editing surface.</summary>
    public const string RenameCommand = "source.rename";

    private const string RefactorRow = "source.refactor";

    /// <summary>Raised when the rename command asks the editing surface to open its rename bar.</summary>
    public event Action? RenameRequested;

    /// <inheritdoc />
    public string Id => "source";

    /// <inheritdoc />
    public string Title => "Source";

    /// <inheritdoc />
    public Glyph Icon => Glyph.Code;

    /// <inheritdoc />
    public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;

    /// <inheritdoc />
    public Type EditorComponent => typeof(DemoSourceEditor);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> Rows =>
        [new() { Id = RefactorRow, Label = "Refactor" }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(RefactorRow, [new FormaToolbarCommand { Id = "rename", Title = "Rename Symbol", Icon = Glyph.PenToSquare, Command = RenameCommand }]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
        [new() { Key = "F2", Command = RenameCommand }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (command == RenameCommand)
        {
            RenameRequested?.Invoke();
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>A prose-oriented surface, rewriting whole lines and reporting the document's size in the status strip.</summary>
public sealed class TextEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The identifier a plugin names to scope itself to this surface.</summary>
    public const string EditorId = "text";

    /// <summary>The command prefixing every line with its position.</summary>
    public const string NumberLinesCommand = "text.number-lines";

    /// <summary>The command putting the lines in ascending order.</summary>
    public const string SortAscendingCommand = "text.sort-ascending";

    /// <summary>The command putting the lines in descending order.</summary>
    public const string SortDescendingCommand = "text.sort-descending";

    /// <summary>The command showing or hiding the word count readout.</summary>
    public const string WordCountCommand = "text.word-count";

    private const string SortingRow = "text.sorting";

    private IComposerStatusItem? _wordCount;

    /// <inheritdoc />
    public string Id => EditorId;

    /// <inheritdoc />
    public string Title => "Plain Text";

    /// <inheritdoc />
    public Glyph Icon => Glyph.FileLines;

    /// <inheritdoc />
    public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;

    /// <inheritdoc />
    public Type EditorComponent => typeof(DemoTextEditor);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> Rows =>
        [new() { Id = SortingRow, Label = "Sorting" }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.BuiltIn,
        [
            new FormaToolbarCommand { Id = "number-lines", Title = "Number Lines", Icon = Glyph.ListOl, Command = NumberLinesCommand },
            new FormaToolbarCommand { Id = "word-count", Title = "Word Count", Icon = Glyph.TextWidth, Command = WordCountCommand },
        ]),
        new(SortingRow,
        [
            new FormaToolbarCommand { Id = "sort-ascending", Title = "Sort A to Z", Icon = Glyph.ArrowDownAZ, Command = SortAscendingCommand },
            new FormaToolbarCommand { Id = "sort-descending", Title = "Sort Z to A", Icon = Glyph.ArrowUpZA, Command = SortDescendingCommand },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (command == WordCountCommand)
        {
            ToggleWordCount(context);
            return;
        }

        var lines = context.Document.Content.Split('\n');

        var content = command switch
        {
            NumberLinesCommand => string.Join('\n', lines.Select((line, index) => $"{(index + 1).ToString(CultureInfo.CurrentCulture)}. {line}")),
            SortAscendingCommand => string.Join('\n', lines.Order(StringComparer.CurrentCulture)),
            SortDescendingCommand => string.Join('\n', lines.OrderDescending(StringComparer.CurrentCulture)),
            _ => null,
        };

        if (content is null)
        {
            return;
        }

        await context.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
    }

    /// <summary>Brings the readout back in step with a document that has changed, whoever changed it.</summary>
    public void RefreshReadout(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _wordCount?.Text = Describe(content);
    }

    private static string Describe(string content)
    {
        var words = content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        var lines = content.Split('\n').Length;

        return $"{Count(words, "word")}, {Count(lines, "line")}";

        static string Count(int value, string noun)
        {
            return $"{value.ToString(CultureInfo.CurrentCulture)} {noun}{(value == 1 ? string.Empty : "s")}";
        }
    }

    private void ToggleWordCount(ComposerContext context)
    {
        if (_wordCount is not null)
        {
            _wordCount.Dispose();
            _wordCount = null;
            return;
        }

        _wordCount = context.Regions.CreateStatusItem(Describe(context.Document.Content), "Words and lines in the document");
    }
}

/// <summary>An external plugin joining the shared plugin row with one command.</summary>
public sealed class TimestampPlugin : IComposerPlugin
{
    /// <summary>The command appending the current date and time.</summary>
    public const string TimestampCommand = "demo.timestamp";

    /// <inheritdoc />
    public string Id => "timestamp";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [TextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
        [new(ComposerRows.Plugins, [new FormaToolbarCommand { Id = "timestamp", Title = "Insert Timestamp", Icon = Glyph.Clock, Command = TimestampCommand }])];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
        [new() { Key = "t", Command = TimestampCommand, Control = true, Shift = true }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (command != TimestampCommand)
        {
            return;
        }

        var stamp = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        var content = string.IsNullOrEmpty(context.Document.Content) ? stamp : $"{context.Document.Content}\n{stamp}";

        await context.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
    }
}

/// <summary>An external plugin that declares a row of its own rather than joining the shared plugin row.</summary>
public sealed class CleanupPlugin : IComposerPlugin
{
    /// <summary>The command dropping lines that hold nothing but whitespace.</summary>
    public const string RemoveBlankLinesCommand = "demo.remove-blank-lines";

    /// <summary>The command emptying the document.</summary>
    public const string ClearCommand = "demo.clear";

    /// <inheritdoc />
    public string Id => "cleanup";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [TextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new FormaToolbarGroup
            {
                Id = "cleanup",
                Title = "Cleanup",
                Items =
                [
                    new FormaToolbarCommand { Id = "remove-blank-lines", Title = "Remove Blank Lines", Icon = Glyph.SquareMinus, Command = RemoveBlankLinesCommand },
                    new FormaToolbarCommand { Id = "clear", Title = "Clear", Icon = Glyph.Trash, Command = ClearCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerInputTrigger> InputTriggers => [];

    /// <inheritdoc />
    public IReadOnlyList<ComposerAsset> Assets => [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var content = command switch
        {
            RemoveBlankLinesCommand => string.Join('\n', context.Document.Content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))),
            ClearCommand => string.Empty,
            _ => null,
        };

        if (content is not null)
        {
            await context.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
        }
    }
}
