namespace Forma.Framework.Localization.Linter.Reporting;

using Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Writes what a run found.</summary>
public interface IDiagnosticReporter
{
    /// <summary>Reports the run.</summary>
    /// <param name="result">What the run found.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    ValueTask ReportAsync(LintResult result, CancellationToken cancellationToken = default);
}
