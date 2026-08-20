namespace Forma.Framework.Localization.Linter.Sources;

using Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Checks that a project's translation files can still be found by the type they were named for.</summary>
public interface IReachabilityLinter
{
    /// <summary>Every one of the project's translation files that no type answers to.</summary>
    /// <param name="localizationFiles">The project's translation files, already resolved to their types.</param>
    IReadOnlyList<LintDiagnostic> Lint(IReadOnlyList<TranslationFile> localizationFiles);
}
