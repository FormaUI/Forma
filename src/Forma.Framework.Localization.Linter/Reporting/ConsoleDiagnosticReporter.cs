namespace Forma.Framework.Localization.Linter.Reporting;

using Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Writes every diagnostic, then how many there were.</summary>
/// <param name="writer">Where the lines go.</param>
public sealed class ConsoleDiagnosticReporter(TextWriter writer) : IDiagnosticReporter
{
    private readonly TextWriter _writer = writer ?? throw new ArgumentNullException(nameof(writer));

    /// <inheritdoc />
    public async ValueTask ReportAsync(LintResult result, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(result);

        foreach (var diagnostic in result.Diagnostics)
        {
            await _writer.WriteLineAsync(diagnostic.ToString().AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        var summary = result.Diagnostics.Count == 0
            ? $"Linted {result.FileCount} file(s); no diagnostics."
            : $"Linted {result.FileCount} file(s); {result.Diagnostics.Count} diagnostic(s).";

        await _writer.WriteLineAsync(summary.AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}
