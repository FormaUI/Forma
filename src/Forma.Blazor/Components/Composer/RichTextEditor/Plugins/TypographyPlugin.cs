namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using System.Globalization;

using Forma.Features.Composer.Assets;
using Forma.Features.Composer.Editing;
using Forma.Features.Composer.Plugins;
using Forma.Framework.Icons;
using Forma.Framework.Toolbar;

/// <summary>Contributes font family, font size, text color, highlight color, and line height operations.</summary>
public sealed class TypographyPlugin(
    RichTextEditorPlugin editor,
    TypographyPluginOptions? options = null) : IComposerFeaturePlugin
{
    /// <summary>The command applying font family styling.</summary>
    public const string FontFamilyCommand = "typography.font-family";

    /// <summary>The command applying font size styling.</summary>
    public const string FontSizeCommand = "typography.font-size";

    /// <summary>The command applying text color styling.</summary>
    public const string TextColorCommand = "typography.text-color";

    /// <summary>The command applying background highlight styling.</summary>
    public const string HighlightColorCommand = "typography.highlight-color";

    /// <summary>The command applying line height styling.</summary>
    public const string LineHeightCommand = "typography.line-height";

    private readonly RichTextEditorPlugin _editor = editor;

    /// <summary>The options configuring available typography choices.</summary>
    public TypographyPluginOptions Options { get; } = options ?? TypographyPluginOptions.Default;

    /// <inheritdoc />
    public string Id => "typography";

    /// <inheritdoc />
    public IReadOnlyList<string> SupportedEditorIds => [RichTextEditorPlugin.EditorId];

    /// <inheritdoc />
    public IReadOnlyList<ComposerToolbarItem> Items =>
    [
        new(ComposerRows.Formatting,
        [
            new FormaToolbarGroup
            {
                Id = "typography",
                Title = "Typography",
                Items =
                [
                    new FormaToolbarMenu
                    {
                        Id = "font-family",
                        Title = "Font Family",
                        Icon = Glyph.Font,
                        Options = [.. Options.FontFamilies.Select(family => new FormaToolbarOption
                        {
                            Id = $"font-family:{family}",
                            Title = family,
                            Command = $"{FontFamilyCommand}:{family}",
                            PreviewStyle = $"font-family:{family}",
                        })],
                    },
                    new FormaToolbarMenu
                    {
                        Id = "font-size",
                        Title = "Font Size",
                        Icon = Glyph.FontSize,
                        Options = [.. Options.FontSizes.Select(size => new FormaToolbarOption
                        {
                            Id = $"font-size:{ToCssSize(size)}",
                            Title = size,
                            Command = $"{FontSizeCommand}:{ToCssSize(size)}",
                        })],
                    },
                    new FormaToolbarMenu
                    {
                        Id = "line-height",
                        Title = "Line Height",
                        Icon = Glyph.LineHeight,
                        Options = [.. Options.LineHeights.Select(height => new FormaToolbarOption
                        {
                            Id = $"line-height:{height}",
                            Title = height,
                            Command = $"{LineHeightCommand}:{height}",
                        })],
                    },
                ],
            },
            new FormaToolbarGroup
            {
                Id = "color",
                Title = "Color",
                Items =
                [
                    new FormaToolbarMenu
                    {
                        Id = "text-color",
                        Title = "Text Color",
                        Icon = Glyph.FontColor,
                        Options = [.. Options.TextColors.Select(color => new FormaToolbarOption
                        {
                            Id = $"text-color:{color}",
                            Title = color,
                            Command = $"{TextColorCommand}:{color}",
                            Swatch = color,
                        })],
                    },
                    new FormaToolbarMenu
                    {
                        Id = "highlight-color",
                        Title = "Highlight Color",
                        Icon = Glyph.Highlight,
                        Options = [.. Options.HighlightColors.Select(color => new FormaToolbarOption
                        {
                            Id = $"highlight-color:{color}",
                            Title = color,
                            Command = $"{HighlightColorCommand}:{color}",
                            Swatch = color,
                        })],
                    },
                ],
            },
        ]),
    ];

    /// <inheritdoc />
    public ValueTask<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public ValueTask ExecuteAsync(string command, ComposerContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);
        ArgumentNullException.ThrowIfNull(context);

        var separator = command.IndexOf(':', StringComparison.Ordinal);
        var name = separator < 0 ? command : command[..separator];
        var value = separator < 0 ? null : command[(separator + 1)..];

        var (jsCommand, fallback) = name switch
        {
            FontFamilyCommand => ("fontName", First(Options.FontFamilies)),
            FontSizeCommand => ("fontSize", First(Options.FontSizes) is { } size ? ToCssSize(size) : null),
            TextColorCommand => ("foreColor", First(Options.TextColors)),
            HighlightColorCommand => ("hiliteColor", First(Options.HighlightColors)),
            LineHeightCommand => ("lineHeight", First(Options.LineHeights)),
            _ => (null, null),
        };

        if (jsCommand is not null)
        {
            _editor.RequestCommand(jsCommand, value ?? fallback);
        }

        return ValueTask.CompletedTask;

        static string? First(IReadOnlyList<string> values)
        {
            return values.Count > 0 ? values[0] : null;
        }
    }

    // The labels name a pixel size at the reader's default, which is the rem the same number divides to.
    private string ToCssSize(string size)
        => Options.FontSizeUnit is ComposerFontSizeUnit.Points
            ? $"{size}pt"
            : FormattableString.Invariant($"{double.Parse(size, CultureInfo.InvariantCulture) / 16:0.####}rem");
}
