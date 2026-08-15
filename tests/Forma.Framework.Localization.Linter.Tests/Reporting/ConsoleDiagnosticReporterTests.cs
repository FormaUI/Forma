namespace Forma.Framework.Localization.Linter.Tests.Reporting;

public sealed class ConsoleDiagnosticReporterTests
{
    [Fact]
    public async Task Should_write_a_clean_summary_when_nothing_was_found()
    {
        // Arrange
        await using var writer = new StringWriter();
        var reporter = new ConsoleDiagnosticReporter(writer);

        // Act
        await reporter.ReportAsync(new LintResult([], 3), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("Linted 3 file(s); no diagnostics.", writer.ToString().Trim());
    }

    [Fact]
    public async Task Should_write_every_diagnostic_before_the_summary()
    {
        // Arrange
        await using var writer = new StringWriter();
        var reporter = new ConsoleDiagnosticReporter(writer);
        var diagnostic = new LintDiagnostic("File.he.json", 2, 5, LintSeverity.Error, LintCodes.MissingMarker, "no marker");

        // Act
        await reporter.ReportAsync(new LintResult([diagnostic], 1), TestContext.Current.CancellationToken);

        // Assert
        var lines = writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal([diagnostic.ToString(), "Linted 1 file(s); 1 diagnostic(s)."], lines);
    }
}
