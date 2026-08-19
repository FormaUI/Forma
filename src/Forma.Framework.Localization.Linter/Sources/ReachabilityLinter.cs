namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Documents;
using Forma.Framework.Localization.Linter.Projects;

/// <summary>
/// Checks that a translation file's own location still names a type. `JsonStringLocaleFactory` resolves a
/// locale by the owning type's full name, not by where the file sits on disk, so a type renamed or moved
/// away leaves its translations unreachable while every other check stays green.
/// </summary>
public sealed class ReachabilityLinter : IReachabilityLinter
{
    // A generic's metadata name carries its arity, which the file name drops, so each plausible one is tried.
    private const int HighestArity = 8;

    /// <inheritdoc />
    public IReadOnlyList<LintDiagnostic> Lint(LoadedProject project, IReadOnlyCollection<string> localizationFiles)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(localizationFiles);

        if (project.Compilation is not { AssemblyName.Length: > 0 } compilation)
        {
            return [];
        }

        var projectDirectory = Path.GetDirectoryName(Path.GetFullPath(project.Path))!;

        // The SDK embeds under RootNamespace, which a project may declare apart from its assembly name.
        var root = project.RootNamespace ?? compilation.AssemblyName!;
        var diagnostics = new List<LintDiagnostic>();

        foreach (var file in localizationFiles)
        {
            if (LocalizationFileName.CultureOf(file) is null)
            {
                continue;
            }

            var fullPath = Path.GetFullPath(file);
            var candidates = Candidates(root, projectDirectory, fullPath);

            if (candidates.Any(candidate => compilation.GetTypeByMetadataName(candidate) is not null))
            {
                continue;
            }

            diagnostics.Add(new LintDiagnostic(
                fullPath,
                1,
                1,
                LintCodes.UnreachableTranslation.DefaultSeverity,
                LintCodes.UnreachableTranslation,
                $"No type named '{candidates[0]}' exists. JsonStringLocaleFactory resolves this file by that full name, so its translations are unreachable."));
        }

        return diagnostics;
    }

    // The SDK embeds a file as {RootNamespace}.{RelativeFolderPath}.{Name}, dropping the culture segment for
    // a satellite resource, so the type a file serves is named for its folder position, not its culture suffix.
    // ResourcesPath then inserts leading folder segments that no project file records, so each may be shed in
    // turn; a type in the global namespace carries no root at all. Only the first candidate is reported,
    // because it is the one the default convention names.
    private static IReadOnlyList<string> Candidates(string root, string projectDirectory, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath)!;
        var relative = Path.GetRelativePath(projectDirectory, directory);
        string[] segments = relative is "." ? [] : relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var withoutJson = Path.GetFileNameWithoutExtension(filePath);
        var baseName = Path.GetFileNameWithoutExtension(withoutJson);

        var rooted = Enumerable
            .Range(0, segments.Length + 1)
            .Select(dropped => string.Join('.', new[] { root }.Concat(segments[dropped..]).Append(baseName)));

        return [.. rooted.Append(baseName).SelectMany(WithArities)];
    }

    private static IEnumerable<string> WithArities(string name)
        => [name, .. Enumerable.Range(1, HighestArity).Select(arity => $"{name}`{arity}")];
}
