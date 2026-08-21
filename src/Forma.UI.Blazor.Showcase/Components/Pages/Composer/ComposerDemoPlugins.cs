namespace Forma.UI.Blazor.Showcase.Components.Pages.Composer;

using System.Globalization;

using Forma.UI.Blazor.Components.Composer.PlainTextEditor;
using Forma.UI.Features.Composer.Assets;
using Forma.UI.Features.Composer.Documents;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Style.Glyphs;

/// <summary>A source-oriented surface, numbering lines from the shared row and sorting them from a row of its own.</summary>
public sealed class SourceEditorPlugin : IComposerEditorPlugin
{
    /// <summary>The command prefixing every line with its position.</summary>
    public const string NumberLinesCommand = "source.number-lines";

    /// <summary>The command putting the lines in ascending order.</summary>
    public const string SortAscendingCommand = "source.sort-ascending";

    /// <summary>The command putting the lines in descending order.</summary>
    public const string SortDescendingCommand = "source.sort-descending";

    private const string SortingRow = "source.sorting";

    /// <inheritdoc />
    public string Id => "source";

    /// <inheritdoc />
    public string Title => "Source";

    /// <inheritdoc />
    public Glyph Icon => Glyph.FileLines;

    /// <inheritdoc />
    public ComposerDocumentFormat Format => ComposerDocumentFormat.PlainText;

    /// <inheritdoc />
    public Type EditorComponent => typeof(FormaPlainTextEditor);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> ToolbarRows =>
        [new() { Id = SortingRow, Label = "Sorting" }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> ToolbarItems =>
    [
        new() { Id = "number-lines", Title = "Number Lines", Icon = Glyph.List, Command = NumberLinesCommand },
        new() { Id = "sort-ascending", Title = "Sort A to Z", Icon = Glyph.ArrowDownAZ, Command = SortAscendingCommand, Row = SortingRow },
        new() { Id = "sort-descending", Title = "Sort Z to A", Icon = Glyph.ArrowUpZA, Command = SortDescendingCommand, Row = SortingRow },
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

        var lines = context.Document.Content.Split('\n');

        var content = command switch
        {
            NumberLinesCommand => string.Join('\n', lines.Select((line, index) => $"{(index + 1).ToString(CultureInfo.CurrentCulture)}. {line}")),
            SortAscendingCommand => string.Join('\n', lines.Order(StringComparer.CurrentCulture)),
            SortDescendingCommand => string.Join('\n', lines.OrderDescending(StringComparer.CurrentCulture)),
            _ => null,
        };

        if (content is not null)
        {
            await context.ChangeDocumentAsync(context.Document with { Content = content }).ConfigureAwait(false);
        }
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
    public IReadOnlyList<ComposerToolbarItem> ToolbarItems =>
        [new() { Id = "timestamp", Title = "Insert Timestamp", Icon = Glyph.Clock, Command = TimestampCommand }];

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

    private const string CleanupRow = "demo.cleanup";

    /// <inheritdoc />
    public string Id => "cleanup";

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> ToolbarRows =>
        [new() { Id = CleanupRow, Label = "Cleanup" }];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> ToolbarItems =>
    [
        new() { Id = "remove-blank-lines", Title = "Remove Blank Lines", Icon = Glyph.SquareMinus, Command = RemoveBlankLinesCommand, Row = CleanupRow },
        new() { Id = "clear", Title = "Clear", Icon = Glyph.Trash, Command = ClearCommand, Row = CleanupRow },
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
