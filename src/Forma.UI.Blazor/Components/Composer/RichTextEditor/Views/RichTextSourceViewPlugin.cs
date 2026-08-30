namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Views;

using Forma.UI.Design.Icons;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Plugins;
using Forma.UI.Features.Toolbar;

/// <summary>The rich text document's HTML, laid out for reading, copied or saved from there.</summary>
public sealed class RichTextSourceViewPlugin(RichTextEditorPlugin editor) : IComposerViewPlugin
{
    /// <summary>The identifier of this view.</summary>
    public const string ViewId = "rich-text.source";

    /// <summary>The row carrying how the source reads and where it goes.</summary>
    public const string SourceRow = "rich-text.source.view";

    /// <summary>The command turning line wrapping on or off.</summary>
    public const string WrapCommand = "source.wrap";

    /// <summary>The command placing the source on the clipboard.</summary>
    public const string CopyCommand = "source.copy";

    /// <summary>The command saving the source as an HTML file.</summary>
    public const string DownloadCommand = "source.download";

    private readonly RichTextEditorPlugin _editor = editor ?? throw new ArgumentNullException(nameof(editor));

    /// <summary>Raised when the source is to be placed on the clipboard.</summary>
    public event Action? CopyRequested;

    /// <summary>Raised when the source is to be saved as a file.</summary>
    public event Action? DownloadRequested;

    /// <summary>Whether long lines wrap rather than scroll.</summary>
    public bool IsWrapped { get; private set; } = true;

    /// <inheritdoc />
    public string Id => ViewId;

    /// <inheritdoc />
    public string EditorId => _editor.Id;

    /// <inheritdoc />
    public string Kind => ComposerViews.Source;

    /// <inheritdoc />
    public string Title => "Source";

    /// <inheritdoc />
    public Icon Icon => Glyph.Code;

    /// <inheritdoc />
    public Type ViewComponent => typeof(FormaRichTextSource);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> Rows =>
    [
        new() { Id = SourceRow, Label = "Source View" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(SourceRow,
        [
            new FormaToolbarGroup
            {
                Id = "source-view",
                Title = "Source View",
                Items =
                [
                    new FormaToolbarToggle { Id = "wrap", Title = "Wrap Lines", Icon = Glyph.ArrowTurnDownRight, Command = WrapCommand },
                    new FormaToolbarCommand { Id = "copy", Title = "Copy Source", Icon = Glyph.Copy, Command = CopyCommand },
                    new FormaToolbarCommand { Id = "download", Title = "Download Source", Icon = Glyph.Download, Command = DownloadCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyCollection<string> ActiveStates
        => IsWrapped ? ["wrap"] : [];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        switch (command)
        {
            case WrapCommand:
                IsWrapped = !IsWrapped;
                break;
            case CopyCommand:
                CopyRequested?.Invoke();
                break;
            case DownloadCommand:
                DownloadRequested?.Invoke();
                break;
        }

        return ValueTask.CompletedTask;
    }
}
