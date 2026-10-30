namespace Forma.Blazor.Components.Composer.RichTextEditor.Views;

using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>The rich text document's HTML, laid out for reading, copied or saved from there.</summary>
public sealed class RichTextSourceViewPlugin : IComposerViewPlugin
{
    /// <summary>The identifier of this view.</summary>
    public const string ViewId = "rich-text.source";

    /// <summary>The row carrying line wrapping, copy and download.</summary>
    public const string SourceRow = "rich-text.source.view";

    /// <summary>The command turning line wrapping on or off.</summary>
    public const string WrapCommand = "source.wrap";

    /// <summary>The command placing the source on the clipboard.</summary>
    public const string CopyCommand = "source.copy";

    /// <summary>The command saving the source as an HTML file.</summary>
    public const string DownloadCommand = "source.download";

    /// <summary>Raised when the source is to be placed on the clipboard.</summary>
    public event Action? CopyRequested;

    /// <summary>Raised when the source is to be saved as a file.</summary>
    public event Action? DownloadRequested;

    /// <summary>Whether long lines wrap.</summary>
    public bool IsWrapped { get; private set; } = true;

    /// <inheritdoc />
    public string Id => ViewId;

    /// <inheritdoc />
    public string Kind => ComposerViews.Source;

    /// <inheritdoc />
    public string Title => "Source";

    /// <inheritdoc />
    public Icon Icon => Glyph.FileCode;

    /// <inheritdoc />
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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
            new ToolbarGroup
            {
                Id = "source-view",
                Title = "Source View",
                Items =
                [
                    new ToolbarToggle { Id = "wrap", Title = "Wrap Lines", Icon = Glyph.ArrowTurnDownRight, Command = WrapCommand },
                    new ToolbarCommand { Id = "copy", Title = "Copy Source", Icon = Glyph.Copy, Command = CopyCommand },
                    new ToolbarCommand { Id = "download", Title = "Download Source", Icon = Glyph.Download, Command = DownloadCommand },
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
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

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
