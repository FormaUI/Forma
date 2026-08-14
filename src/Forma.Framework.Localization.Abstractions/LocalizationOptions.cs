namespace Forma.Framework.Localization;

/// <summary>Settings shared by every localization backend.</summary>
public class LocalizationOptions
{
    /// <summary>Language used by the localization keys and their language-specific rules when no translation is available.</summary>
    public string SourceLanguage { get; init; } = "en";
}
