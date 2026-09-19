namespace Forma.Blazor.Showcase.Components.Pages.Features;

using System.Globalization;

using Forma.Blazor.Components.Composer.PlainTextEditor;
using Forma.Framework.Composer.Assets;
using Forma.Framework.Composer.Documents;
using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>A source-oriented editor, numbering its lines in a gutter and renaming a symbol from a row of its own.</summary>
public sealed class SourceEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The command opening the rename bar over the canvas.</summary>
    public const string RenameCommand = "source.rename";

    private const string RefactorRow = "source.refactor";

    /// <summary>Raised when the rename command asks the canvas to open its rename bar.</summary>
    public event Action? RenameRequested;

    /// <inheritdoc />
    public string Id => "source";

    /// <inheritdoc />
    public string Title => "Source";

    /// <inheritdoc />
    public Icon Icon => Glyph.Code;

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
        new(RefactorRow, [new ToolbarCommand { Id = "rename", Title = "Rename Symbol", Icon = Glyph.Edit, Command = RenameCommand }]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
        [new() { Key = "F2", Command = RenameCommand }];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == RenameCommand)
        {
            RenameRequested?.Invoke();
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>A prose-oriented editor, rewriting whole lines and reporting the document's size in the status strip.</summary>
public sealed class TextEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The identifier a plugin names to scope itself to this editor.</summary>
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
    public Icon Icon => Glyph.FileText;

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
            new ToolbarCommand { Id = "number-lines", Title = "Number Lines", Icon = Glyph.ListNumbered, Command = NumberLinesCommand },
            new ToolbarCommand { Id = "word-count", Title = "Word Count", Icon = Glyph.Count, Command = WordCountCommand },
        ]),
        new(SortingRow,
        [
            new ToolbarCommand { Id = "sort-ascending", Title = "Sort A to Z", Icon = Glyph.SortAlphaAscending, Command = SortAscendingCommand },
            new ToolbarCommand { Id = "sort-descending", Title = "Sort Z to A", Icon = Glyph.SortAlphaDescending, Command = SortDescendingCommand },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command == WordCountCommand)
        {
            ToggleWordCount(context);
            return;
        }

        var lines = context.Document.Content.Split('\n');

        var content = command switch
        {
            NumberLinesCommand => lines.All(IsNumbered)
                ? string.Join('\n', lines.Select(StripNumber))
                : string.Join('\n', lines.Select((line, index) => $"{(index + 1).ToString(CultureInfo.CurrentCulture)}. {line}")),
            SortAscendingCommand => string.Join('\n', lines.Order(StringComparer.CurrentCulture)),
            SortDescendingCommand => string.Join('\n', lines.OrderDescending(StringComparer.CurrentCulture)),
            _ => null,
        };

        if (content is null)
        {
            return;
        }

        await interaction.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
    }

    /// <summary>Brings the readout back in step with a document that has changed, whoever changed it.</summary>
    public void RefreshReadout(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        _wordCount?.Text = Describe(content);
    }

    private static bool IsNumbered(string line)
        => line.Length > 0 && char.IsDigit(line[0]) && line.IndexOf(". ", StringComparison.Ordinal) is > 0 and var dot && line[..dot].All(char.IsDigit);

    private static string StripNumber(string line)
        => line[(line.IndexOf(". ", StringComparison.Ordinal) + 2)..];

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
public sealed class TimestampPlugin : IComposerFeaturePlugin
{
    /// <summary>The command appending the current date and time.</summary>
    public const string TimestampCommand = "demo.timestamp";

    /// <inheritdoc />
    public string Id => "timestamp";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
        [new(ComposerRows.Plugins, [new ToolbarCommand { Id = "timestamp", Title = "Insert Timestamp", Icon = Glyph.Clock, Command = TimestampCommand }])];

    /// <inheritdoc />
    public IReadOnlyList<ComposerKeyBinding> KeyBindings =>
        [new() { Key = "t", Command = TimestampCommand, Control = true, Shift = true }];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command != TimestampCommand)
        {
            return;
        }

        var stamp = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);
        var content = string.IsNullOrEmpty(context.Document.Content) ? stamp : $"{context.Document.Content}\n{stamp}";

        await interaction.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
    }
}

/// <summary>An external plugin that declares a row of its own rather than joining the shared plugin row.</summary>
public sealed class CleanupPlugin : IComposerFeaturePlugin
{
    /// <summary>The command dropping lines that hold nothing but whitespace.</summary>
    public const string RemoveBlankLinesCommand = "demo.remove-blank-lines";

    /// <summary>The command emptying the document.</summary>
    public const string ClearCommand = "demo.clear";

    /// <inheritdoc />
    public string Id => "cleanup";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Plugins,
        [
            new ToolbarGroup
            {
                Id = "cleanup",
                Title = "Cleanup",
                Items =
                [
                    new ToolbarCommand { Id = "remove-blank-lines", Title = "Remove Blank Lines", Icon = Glyph.Collapse, Command = RemoveBlankLinesCommand },
                    new ToolbarCommand { Id = "clear", Title = "Clear", Icon = Glyph.Eraser, Command = ClearCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        var content = command switch
        {
            RemoveBlankLinesCommand => string.Join('\n', context.Document.Content.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l))),
            ClearCommand => string.Empty,
            _ => null,
        };

        if (content is not null)
        {
            await interaction.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
        }
    }
}
