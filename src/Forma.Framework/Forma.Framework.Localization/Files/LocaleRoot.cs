namespace Forma.Framework.Localization.Files;

/// <summary>What one assembly's <c>.locale.json</c> puts in force in a source, or the defaults where it has none: where its files begin, the root namespace their anchors are counted from, the name of a folder of locale files, and the paths its files are read by.</summary>
/// <param name="RootDir">Where the source's names begin, written with the source's separator.</param>
/// <param name="RootNamespace">The root namespace anchors are counted from.</param>
/// <param name="LocalesDir">The name <c>$LocalesDir</c> stands for.</param>
/// <param name="Paths">The paths the assembly's files are read by.</param>
internal sealed record LocaleRoot(string RootDir, string RootNamespace, string LocalesDir, IReadOnlyList<LocalePath> Paths)
{
    /// <summary>Whether a name sits beneath a folder, which every name does beneath the empty one.</summary>
    /// <param name="folder">The folder, written with the separator.</param>
    /// <param name="name">The name.</param>
    /// <param name="separator">The character written between folders.</param>
    /// <param name="comparison">How names compare.</param>
    public static bool IsBeneath(string folder, string name, char separator, StringComparison comparison)
        => folder.Length == 0
            || (name.Length > folder.Length && name[folder.Length] == separator && name.StartsWith(folder, comparison));
}
