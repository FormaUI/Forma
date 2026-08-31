namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring font families, sizes, colors, and line heights offered by <see cref="TypographyPlugin"/>.</summary>
public sealed record TypographyPluginOptions
{
    /// <summary>The default typography options.</summary>
    public static TypographyPluginOptions Default { get; } = new();

    /// <summary>The font families offered in the font picker.</summary>
    public IReadOnlyList<string> FontFamilies { get; init; } =
    [
        "Arial", "Helvetica", "Verdana", "Tahoma", "Trebuchet MS", "Segoe UI",
        "Georgia", "Times New Roman", "Garamond", "Palatino", "Book Antiqua",
        "Courier New", "Consolas", "Lucida Console", "monospace",
    ];

    /// <summary>The font sizes offered in the size picker, labelled by their pixel size at the reader's default.</summary>
    public IReadOnlyList<string> FontSizes { get; init; } =
        ["8", "9", "10", "11", "12", "14", "16", "18", "24", "30", "36", "48", "72"];

    /// <summary>The unit a chosen font size is written in.</summary>
    public ComposerFontSizeUnit FontSizeUnit { get; init; } = ComposerFontSizeUnit.Rem;

    /// <summary>The line heights offered in the line height picker.</summary>
    public IReadOnlyList<string> LineHeights { get; init; } =
        ["1.0", "1.15", "1.5", "1.6", "2.0"];

    /// <summary>The text colors offered in the color picker.</summary>
    public IReadOnlyList<string> TextColors { get; init; } =
        ["#000000", "#d32f2f", "#1976d2", "#388e3c", "#f57c00", "#7b1fa2", "#616161"];

    /// <summary>The highlight background colors offered in the highlight picker.</summary>
    public IReadOnlyList<string> HighlightColors { get; init; } =
        ["#ffff00", "#a5d6a7", "#90caf9", "#ffcc80", "#ce93d8", "#e0e0e0"];
}
