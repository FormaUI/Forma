namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Css;

/// <summary>Options configuring font families, sizes, colors, and line heights offered by <see cref="TypographyPlugin"/>.</summary>
public sealed record TypographyPluginOptions
{
    /// <summary>The default typography options.</summary>
    public static TypographyPluginOptions Default => new();

    /// <summary>The font families offered in the font picker.</summary>
    public IReadOnlyList<string> FontFamilies { get; set; } =
    [
        "Arial", "Helvetica", "Verdana", "Tahoma", "Trebuchet MS", "Segoe UI",
        "Georgia", "Times New Roman", "Garamond", "Palatino", "Book Antiqua",
        "Courier New", "Consolas", "Lucida Console", "monospace",
    ];

    /// <summary>The font sizes offered in the size picker, labeled by their pixel size at the reader's default.</summary>
    public IReadOnlyList<string> FontSizes { get; set; } =
        ["8", "9", "10", "11", "12", "14", "16", "18", "24", "30", "36", "48", "72"];

    /// <summary>The unit a chosen font size is written in.</summary>
    public ComposerFontSizeUnit FontSizeUnit { get; set; } = ComposerFontSizeUnit.Rem;

    /// <summary>The line heights offered in the line height picker.</summary>
    public IReadOnlyList<string> LineHeights { get; set; } =
        ["1.0", "1.15", "1.5", "1.6", "2.0"];

    /// <summary>The text colors offered in the color picker.</summary>
    public IReadOnlyList<TypographyColor> TextColors { get; set; } =
    [
        new("Black", CssColor.FromHex(0x000000)),
        new("Red", CssColor.FromHex(0xd32f2f)),
        new("Blue", CssColor.FromHex(0x1976d2)),
        new("Green", CssColor.FromHex(0x388e3c)),
        new("Orange", CssColor.FromHex(0xf57c00)),
        new("Purple", CssColor.FromHex(0x7b1fa2)),
        new("Gray", CssColor.FromHex(0x616161)),
    ];

    /// <summary>The highlight background colors offered in the highlight picker.</summary>
    public IReadOnlyList<TypographyColor> HighlightColors { get; set; } =
    [
        new("Yellow", CssColor.FromHex(0xffff00)),
        new("Green", CssColor.FromHex(0xa5d6a7)),
        new("Blue", CssColor.FromHex(0x90caf9)),
        new("Orange", CssColor.FromHex(0xffcc80)),
        new("Purple", CssColor.FromHex(0xce93d8)),
        new("Gray", CssColor.FromHex(0xe0e0e0)),
    ];
}
