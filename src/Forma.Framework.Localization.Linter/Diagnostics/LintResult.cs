namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>What a run looked at and what it found.</summary>
/// <param name="Diagnostics">Every diagnostic, ordered by file and then by position.</param>
/// <param name="FileCount">How many files the run read.</param>
public sealed record LintResult(IReadOnlyList<LintDiagnostic> Diagnostics, int FileCount);
