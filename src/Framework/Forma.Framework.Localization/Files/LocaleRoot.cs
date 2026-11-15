namespace Forma.Framework.Localization.Files;

/// <summary>The locale settings in force for one assembly in a source, from its <c>.locale.json</c> or the defaults.</summary>
/// <param name="RootDir">Where the source's names begin, written with <c>/</c> between folders.</param>
/// <param name="RootNamespace">The root namespace anchors are counted from.</param>
/// <param name="LocalesDir">The name <c>$LocalesDir</c> stands for.</param>
/// <param name="Paths">The paths the assembly states its files are read by, or null for the default reading.</param>
internal sealed record LocaleRoot(string RootDir, string RootNamespace, string LocalesDir, IReadOnlyList<LocalePath>? Paths);
