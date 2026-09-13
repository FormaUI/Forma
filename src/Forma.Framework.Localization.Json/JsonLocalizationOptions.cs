namespace Forma.Framework.Localization.Json;

using Forma.Framework.Localization;

/// <summary>Settings for <see cref="JsonStringLocaleFactory"/>.</summary>
public sealed class JsonLocalizationOptions : LocalizationOptions
{
    /// <summary>Folder holding the translation files, empty when they sit beside the types they translate.</summary>
    public string ResourcesPath { get; init; } = string.Empty;

    /// <summary>Folder holding the host's own translation files, resolved against the application's base directory when relative; empty to read none.</summary>
    public string LocalesPath { get; init; } = "Locales";
}
