namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;

/// <summary>Checks the keys written at a project's localization call sites.</summary>
public interface ISourceLinter
{
    /// <summary>Every diagnostic the project's call sites produce.</summary>
    /// <param name="project">The project to read.</param>
    /// <param name="files">The source files the run asked for, or empty for all of them.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    ValueTask<IReadOnlyList<LintDiagnostic>> LintAsync(
        LoadedProject project,
        IReadOnlyCollection<string> files,
        CancellationToken cancellationToken = default);
}
