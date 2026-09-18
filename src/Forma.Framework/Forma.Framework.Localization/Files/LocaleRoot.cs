namespace Forma.Framework.Localization.Files;

/// <summary>Where one assembly's files begin in a source, the root namespace their anchors are counted from, and the settings in force beneath each folder.</summary>
/// <param name="rootDir">Where the source's names begin, written with the source's separator.</param>
/// <param name="rootNamespace">The root namespace anchors are counted from.</param>
/// <param name="rootSettings">The settings in force at the root, and for every name no nested settings file holds.</param>
/// <param name="nestedSettings">The settings each nested <c>.locali.json</c> puts in force beneath its folder.</param>
/// <param name="separator">The character written between folders in the source's names.</param>
/// <param name="comparison">How the source compares names.</param>
internal sealed class LocaleRoot(
    string rootDir,
    string rootNamespace,
    LocaleFolderSettings rootSettings,
    IReadOnlyList<LocaleFolderSettings> nestedSettings,
    char separator,
    StringComparison comparison)
{
    /// <summary>Where the source's names begin, written with the source's separator.</summary>
    public string RootDir { get; } = rootDir;

    /// <summary>The root namespace anchors are counted from.</summary>
    public string RootNamespace { get; } = rootNamespace;

    /// <summary>The settings a name is read under: the nearest nested folder's holding it, or the root's.</summary>
    /// <param name="name">The file's name in the source.</param>
    public LocaleFolderSettings GetSettings(string name)
    {
        var nearestSettings = rootSettings;
        foreach (var folderSettings in nestedSettings)
        {
            if (folderSettings.Depth > nearestSettings.Depth && IsBeneath(folderSettings.Folder, name, separator, comparison))
            {
                nearestSettings = folderSettings;
            }
        }

        return nearestSettings;
    }

    /// <summary>Whether a name sits beneath a folder, which every name does beneath the empty one.</summary>
    /// <param name="folder">The folder, written with the separator.</param>
    /// <param name="name">The name.</param>
    /// <param name="separator">The character written between folders.</param>
    /// <param name="comparison">How names compare.</param>
    public static bool IsBeneath(string folder, string name, char separator, StringComparison comparison)
        => folder.Length == 0
            || (name.Length > folder.Length && name[folder.Length] == separator && name.StartsWith(folder, comparison));
}
