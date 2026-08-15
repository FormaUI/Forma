namespace Forma.Framework.Localization.Linter.Cli;

/// <summary>The files a run was asked to read, split by how each is checked.</summary>
/// <param name="LocalizationFiles">The JSON files to check against the schema.</param>
/// <param name="SourceFiles">The C# and Razor files whose call sites are checked.</param>
/// <param name="Projects">The projects the source files belong to.</param>
public sealed record LintTarget(
    IReadOnlyList<string> LocalizationFiles,
    IReadOnlyList<string> SourceFiles,
    IReadOnlyList<string> Projects)
{
    /// <summary>A target that names nothing.</summary>
    public static LintTarget Empty { get; } = new([], [], []);

    /// <summary>How many files the target names in total.</summary>
    public int FileCount
        => LocalizationFiles.Count + SourceFiles.Count;
}
