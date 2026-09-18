namespace Forma.Framework.Localization.Files;

/// <summary>The settings in force for the names beneath one folder of a source: the folder's own <c>.locali.json</c> laid over every farther one's.</summary>
/// <param name="Folder">The folder the settings apply beneath, written with the source's separator, or empty for the whole source.</param>
/// <param name="Depth">How many settings files are laid over one another here, so a nearer folder's settings are deeper than a farther one's.</param>
/// <param name="LocalesDir">The name <c>$LocalesDir</c> stands for.</param>
/// <param name="Paths">The paths a file beneath the folder is read by.</param>
internal sealed record LocaleFolderSettings(string Folder, int Depth, string LocalesDir, IReadOnlyList<LocalePath> Paths)
{
    /// <summary>The settings a source applies when no <c>.locali.json</c> says otherwise.</summary>
    /// <param name="paths">The paths the source reads by default.</param>
    public static LocaleFolderSettings CreateDefault(IReadOnlyList<LocalePath> paths)
        => new(string.Empty, 0, LocaliSettings.DefaultLocalesDir, paths);

    /// <summary>These settings with a nearer file's keys laid over them, a key the file leaves out keeping its value here.</summary>
    /// <param name="folder">The folder the nearer file sits in.</param>
    /// <param name="settings">What the nearer file says.</param>
    public LocaleFolderSettings Apply(string folder, LocaliSettings settings)
        => new(folder, Depth + 1, settings.LocalesDir ?? LocalesDir, settings.Paths ?? Paths);
}
