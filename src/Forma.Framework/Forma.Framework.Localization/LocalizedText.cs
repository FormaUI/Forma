namespace Forma.Framework.Localization;

/// <summary>A translated string together with the key it was looked up under.</summary>
/// <param name="Name">The key the text was looked up under.</param>
/// <param name="Value">The translated text, or the key itself when no translation exists.</param>
/// <param name="ResourceNotFound">Whether the lookup fell back to the key.</param>
/// <param name="SearchedLocation">The resource base name that was searched.</param>
public readonly record struct LocalizedText(
    string Name,
    string Value,
    bool ResourceNotFound = false,
    string? SearchedLocation = null)
{
    /// <summary>The translated text.</summary>
    public override string ToString()
        => Value;

    /// <summary>Yields the translated text, so a localized value can be used wherever a string is expected.</summary>
    public static implicit operator string(LocalizedText text)
    {
        return text.Value;
    }
}
