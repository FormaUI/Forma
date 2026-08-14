namespace Forma.Framework.Localization;

/// <summary>Settings for <see cref="JsonStringLocaleFactory"/>.</summary>
public sealed class JsonLocalizationOptions : LocalizationOptions
{
    /// <summary>Folder holding the translation files, empty when they sit beside the types they translate.</summary>
    public string ResourcesPath { get; init; } = string.Empty;
}
