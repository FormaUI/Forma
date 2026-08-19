namespace Forma.Framework.Localization.Linter.Linting;

using System.Collections.Concurrent;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;
using Forma.Framework.Localization.Linter.Sources;

using Microsoft.Extensions.Options;

/// <summary>Lints the localization files and the source call sites the target names.</summary>
/// <param name="options">What the command line asked for.</param>
/// <param name="resolver">Turns the path into a file set.</param>
/// <param name="linter">Checks one localization file.</param>
/// <param name="loader">Loads the projects the source files belong to.</param>
/// <param name="sources">Checks the keys written at call sites.</param>
/// <param name="reachability">Checks that a project's translation files can still be found by type.</param>
public sealed class LintService(
    IOptions<LinterOptions> options,
    ILintTargetResolver resolver,
    ILocalizationFileLinter linter,
    IProjectLoader loader,
    ISourceLinter sources,
    IReachabilityLinter reachability) : ILintService
{
    private readonly LinterOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILintTargetResolver _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
    private readonly ILocalizationFileLinter _linter = linter ?? throw new ArgumentNullException(nameof(linter));
    private readonly IProjectLoader _loader = loader ?? throw new ArgumentNullException(nameof(loader));
    private readonly ISourceLinter _sources = sources ?? throw new ArgumentNullException(nameof(sources));
    private readonly IReachabilityLinter _reachability = reachability ?? throw new ArgumentNullException(nameof(reachability));

    /// <inheritdoc />
    public async ValueTask<LintResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var target = _resolver.Resolve(_options.Path);
        var diagnostics = new ConcurrentBag<LintDiagnostic>();

        // Each file is read independently, so they overlap rather than queueing behind one another.
        await Parallel.ForEachAsync(
            target.LocalizationFiles,
            cancellationToken,
            async (file, token) =>
            {
                foreach (var diagnostic in await _linter.LintAsync(file, token).ConfigureAwait(false))
                {
                    diagnostics.Add(diagnostic);
                }
            }).ConfigureAwait(false);

        foreach (var diagnostic in await LintProjectsAsync(target, cancellationToken).ConfigureAwait(false))
        {
            diagnostics.Add(diagnostic);
        }

        return new LintResult(
            [.. diagnostics
                .OrderBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Line)
                .ThenBy(diagnostic => diagnostic.Column)],
            target.FileCount);
    }

    private async ValueTask<IReadOnlyList<LintDiagnostic>> LintProjectsAsync(LintTarget target, CancellationToken cancellationToken)
    {
        if (target.Projects.Count == 0)
        {
            return [];
        }

        // A project compiles whole because binding needs it, but only the named files are reported on.
        var wanted = target.SourceFiles.Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var diagnostics = new List<LintDiagnostic>();

        foreach (var project in await _loader.LoadAsync(target.Projects, cancellationToken).ConfigureAwait(false))
        {
            diagnostics.AddRange(_sources.Lint(project, wanted, cancellationToken));
            diagnostics.AddRange(_reachability.Lint(project, target.LocalizationFilesOf(project.Path)));
        }

        return diagnostics;
    }
}
