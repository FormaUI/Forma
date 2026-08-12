namespace Forma.Framework.Localization;

/// <summary>Settings for <see cref="JsonStringLocaleFactory"/>.</summary>
public sealed class JsonLocalizationOptions
{
    /// <summary>Folder holding the translation files, empty when they sit beside the types they translate.</summary>
    public string ResourcesPath { get; init; } = string.Empty;

    /// <summary>Language the keys are written in, whose plural rules resolve a key that has no translation.</summary>
    public string PrimaryCulture { get; init; } = "en";
}
