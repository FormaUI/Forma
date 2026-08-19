namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;

/// <summary>Checks that a project's translation files can still be found by the type they were named for.</summary>
public interface IReachabilityLinter
{
    /// <summary>Every one of the project's translation files that no type answers to.</summary>
    /// <param name="project">The project the files are checked against.</param>
    /// <param name="localizationFiles">The translation files this project owns, which the target attributed.</param>
    IReadOnlyList<LintDiagnostic> Lint(LoadedProject project, IReadOnlyCollection<string> localizationFiles);
}
