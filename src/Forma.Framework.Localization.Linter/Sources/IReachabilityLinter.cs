namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Projects;

/// <summary>Checks that a project's translation files can still be found by the type they were named for.</summary>
public interface IReachabilityLinter
{
    /// <summary>Every translation file under the project that no type answers to.</summary>
    /// <param name="project">The project the files are checked against.</param>
    /// <param name="localizationFiles">Every translation file the run named; files outside this project are ignored.</param>
    IReadOnlyList<LintDiagnostic> Lint(LoadedProject project, IReadOnlyCollection<string> localizationFiles);
}
