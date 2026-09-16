namespace Forma.Blazor.Components.Composer.RichTextEditor.Views;

using System.Globalization;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>The rich text document as a reader will see it, laid out at a chosen paper or screen size, printed or exported from there.</summary>
public sealed class RichTextPreviewViewPlugin : IComposerViewPlugin
{
    /// <summary>The identifier of this view.</summary>
    public const string ViewId = "rich-text.preview";

    /// <summary>The row carrying the page's size, orientation and zoom.</summary>
    public const string PageRow = "rich-text.preview.page";

    /// <summary>The row carrying what leaves the preview: print and export.</summary>
    public const string OutputRow = "rich-text.preview.output";

    /// <summary>The prefix of the commands choosing a page size, completed by a <see cref="PreviewPage"/> identifier.</summary>
    public const string PageCommandPrefix = "preview.page:";

    /// <summary>The prefix of the commands choosing a zoom, completed by a percentage.</summary>
    public const string ZoomCommandPrefix = "preview.zoom:";

    /// <summary>The command turning a sheet between portrait and landscape.</summary>
    public const string LandscapeCommand = "preview.landscape";

    /// <summary>The command printing the document.</summary>
    public const string PrintCommand = "preview.print";

    /// <summary>The command saving the document as an HTML file.</summary>
    public const string ExportHtmlCommand = "preview.export-html";

    private static readonly IReadOnlyList<int> Zooms = [50, 75, 100, 125, 150];

    /// <summary>Raised when the document is to be printed.</summary>
    public event Action? PrintRequested;

    /// <summary>Raised when the document is to be saved as an HTML file.</summary>
    public event Action? ExportHtmlRequested;

    /// <summary>The size the document is laid out at.</summary>
    public PreviewPage Page { get; private set; } = PreviewPage.Fit;

    /// <summary>Whether a sheet is turned on its side.</summary>
    public bool IsLandscape { get; private set; }

    /// <summary>The magnification, as a percentage.</summary>
    public int Zoom { get; private set; } = 100;

    /// <inheritdoc />
    public string Id => ViewId;

    /// <inheritdoc />
    public string Kind => ComposerViews.Preview;

    /// <inheritdoc />
    public string Title => "Preview";

    /// <inheritdoc />
    public Icon Icon => Glyph.Preview;

    /// <inheritdoc />
    public Type ViewComponent => typeof(FormaRichTextPreview);

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarRow> Rows =>
    [
        new() { Id = PageRow, Label = "Page" },
        new() { Id = OutputRow, Label = "Output" },
    ];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(PageRow,
        [
            new FormaToolbarGroup
            {
                Id = "page",
                Title = "Page",
                Items =
                [
                    new FormaToolbarMenu
                    {
                        Id = "page-size",
                        Title = "Page Size",
                        Icon = Glyph.Ruler,
                        Options = [.. PreviewPage.All.Select(page => new FormaToolbarOption
                        {
                            Id = $"page:{page.Id}",
                            Title = page.Title,
                            Command = PageCommandPrefix + page.Id,
                        })],
                    },
                    new FormaToolbarToggle
                    {
                        Id = "landscape",
                        Title = "Landscape",
                        Icon = Glyph.Tablet,
                        Command = LandscapeCommand,
                    },
                    new FormaToolbarMenu
                    {
                        Id = "zoom",
                        Title = "Zoom",
                        Icon = Glyph.ZoomIn,
                        Options = [.. Zooms.Select(zoom => new FormaToolbarOption
                        {
                            Id = $"zoom:{zoom}",
                            Title = zoom.ToString(CultureInfo.InvariantCulture) + "%",
                            Command = ZoomCommandPrefix + zoom.ToString(CultureInfo.InvariantCulture),
                        })],
                    },
                ],
            },
        ]),
        new(OutputRow,
        [
            new FormaToolbarGroup
            {
                Id = "output",
                Title = "Output",
                Items =
                [
                    new FormaToolbarCommand { Id = "print", Title = "Print", Icon = Glyph.Print, Command = PrintCommand },
                    new FormaToolbarCommand { Id = "export-html", Title = "Export HTML", Icon = Glyph.ShareBox, Command = ExportHtmlCommand },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public IReadOnlyList<string> CommandPrefixes => [PageCommandPrefix, ZoomCommandPrefix];

    /// <inheritdoc />
    public IReadOnlyCollection<string> ActiveStates
    {
        get
        {
            var states = new List<string> { $"page:{Page.Id}", $"zoom:{Zoom.ToString(CultureInfo.InvariantCulture)}" };

            if (IsLandscape)
            {
                states.Add("landscape");
            }

            return states;
        }
    }

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, IComposerInteraction interaction, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(interaction);

        if (command.StartsWith(PageCommandPrefix, StringComparison.Ordinal))
        {
            Page = PreviewPage.Find(command[PageCommandPrefix.Length..]);
        }
        else if (command.StartsWith(ZoomCommandPrefix, StringComparison.Ordinal))
        {
            if (int.TryParse(command[ZoomCommandPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoom) && Zooms.Contains(zoom))
            {
                Zoom = zoom;
            }
        }
        else
        {
            switch (command)
            {
                case LandscapeCommand:
                    IsLandscape = !IsLandscape;
                    break;
                case PrintCommand:
                    PrintRequested?.Invoke();
                    break;
                case ExportHtmlCommand:
                    ExportHtmlRequested?.Invoke();
                    break;
            }
        }

        return ValueTask.CompletedTask;
    }
}
