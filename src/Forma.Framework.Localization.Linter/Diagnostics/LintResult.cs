namespace Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>What a run looked at and what it found.</summary>
/// <param name="Diagnostics">Every diagnostic the run reports, ordered by file and then by position.</param>
/// <param name="FileCount">How many files the run read.</param>
/// <param name="SuppressedCount">How many diagnostics fell below the severity the run reports at.</param>
public sealed record LintResult(IReadOnlyList<LintDiagnostic> Diagnostics, int FileCount, int SuppressedCount = 0);
