namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring the special character palette.</summary>
public sealed record SpecialCharacterPluginOptions
{
    /// <summary>The characters available in the palette dropdown.</summary>
    public IReadOnlyList<string> Characters { get; init; } =
    [
        "©", "®", "™", "€", "£", "¥", "§", "°", "±", "—", "–", "«", "»",
        "×", "÷", "µ", "¶", "•", "…", "‰", "¼", "½", "¾", "¿", "¡", "†", "‡",
        "∞", "≈", "≠", "≤", "≥"
    ];

    /// <summary>Default options for the special character plugin.</summary>
    public static SpecialCharacterPluginOptions Default { get; } = new();
}
