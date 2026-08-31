namespace Forma.Framework.Localization;

/// <summary>Settings shared by every localization backend.</summary>
public class LocalizationOptions
{
    /// <summary>Language used by the localization keys and their language-specific rules when no translation is available.</summary>
    public string SourceLanguage { get; init; } = "en";

    /// <summary>Whether a key with no translation for the culture on screen throws rather than falling back to itself.</summary>
    public bool RequireMatchingLocalization { get; init; }
}
