namespace Forma.Framework.Localization.Linter;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Linting;
using Forma.Framework.Localization.Linter.Reporting;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>Drives one lint run and stops the host when it is done.</summary>
/// <param name="service">Runs the checks.</param>
/// <param name="reporters">Writes what the run found.</param>
/// <param name="lifetime">Stops the application once the run has been reported.</param>
/// <param name="logger">Records a failure the run could not report.</param>
public sealed partial class LintHostedService(
    ILintService service,
    IEnumerable<IDiagnosticReporter> reporters,
    IHostApplicationLifetime lifetime,
    ILogger<LintHostedService> logger) : BackgroundService
{
    private readonly ILintService _service = service ?? throw new ArgumentNullException(nameof(service));
    private readonly IEnumerable<IDiagnosticReporter> _reporters = reporters ?? throw new ArgumentNullException(nameof(reporters));
    private readonly IHostApplicationLifetime _lifetime = lifetime ?? throw new ArgumentNullException(nameof(lifetime));
    private readonly ILogger<LintHostedService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>The code the process should exit with.</summary>
    public ExitCode ExitCode { get; private set; } = ExitCode.Clean;

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var result = await _service.RunAsync(stoppingToken).ConfigureAwait(false);

            foreach (var reporter in _reporters)
            {
                await reporter.ReportAsync(result, stoppingToken).ConfigureAwait(false);
            }

            ExitCode = result.Diagnostics.Count == 0 ? ExitCode.Clean : ExitCode.Diagnostics;
        }
        catch (OperationCanceledException)
        {
            ExitCode = ExitCode.Usage;
        }
        catch (IOException exception)
        {
            LogReadFailure(exception);
            ExitCode = ExitCode.Usage;
        }
        catch (UnauthorizedAccessException exception)
        {
            LogAccessFailure(exception);
            ExitCode = ExitCode.Usage;
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "The run could not read or write a file.")]
    private partial void LogReadFailure(Exception exception);

    [LoggerMessage(Level = LogLevel.Error, Message = "The run was refused access to a file.")]
    private partial void LogAccessFailure(Exception exception);
}
