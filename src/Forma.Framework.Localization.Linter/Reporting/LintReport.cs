namespace Forma.Framework.Localization.Linter.Reporting;

/// <summary>The JSON report a run writes.</summary>
/// <param name="FileCount">How many files the run read.</param>
/// <param name="DiagnosticCount">How many diagnostics the run produced.</param>
/// <param name="Diagnostics">The diagnostics themselves.</param>
public sealed record LintReport(int FileCount, int DiagnosticCount, IReadOnlyList<LintReportEntry> Diagnostics);
