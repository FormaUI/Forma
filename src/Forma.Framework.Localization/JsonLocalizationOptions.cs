namespace Forma.Framework.Localization;

/// <summary>Settings for <see cref="JsonStringLocalizerFactory"/>.</summary>
public sealed class JsonLocalizationOptions
{
    /// <summary>Folder holding the translation files, empty when they sit beside the types they translate.</summary>
    public string ResourcesPath { get; init; } = string.Empty;
}
