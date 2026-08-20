namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Projects;

/// <summary>Checks the keys written at a project's localization call sites.</summary>
public interface ISourceLinter
{
    /// <summary>What the project's call sites produced, and the sites themselves.</summary>
    /// <param name="project">The project to read.</param>
    /// <param name="files">The source files the run asked for, or empty for all of them.</param>
    /// <param name="cancellationToken">Cancels the walk.</param>
    SourceLintResult Lint(
        LoadedProject project,
        IReadOnlyCollection<string> files,
        CancellationToken cancellationToken = default);
}
