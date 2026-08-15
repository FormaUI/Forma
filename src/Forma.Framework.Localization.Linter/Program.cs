namespace Forma.Framework.Localization.Linter;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Linting;
using Forma.Framework.Localization.Linter.Reporting;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal static class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (!CommandLine.TryParse(args, out var options, out var error))
        {
            await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
            await Console.Error.WriteLineAsync(CommandLine.Usage).ConfigureAwait(false);

            return (int)ExitCode.Usage;
        }

        using var host = Build(args, options!);

        try
        {
            // RunAsync disposes the provider before it returns, so the service has to be held from before the run.
            var lint = host.Services.GetRequiredService<LintHostedService>();

            await host.RunAsync().ConfigureAwait(false);

            return (int)lint.ExitCode;
        }
        catch (OptionsValidationException exception)
        {
            await Console.Error.WriteLineAsync(string.Join(Environment.NewLine, exception.Failures)).ConfigureAwait(false);

            return (int)ExitCode.Usage;
        }
    }

    private static IHost Build(string[] args, LinterOptions options)
    {
        // An empty builder keeps the linter from reading whatever configuration happens to sit in the working directory.
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings { Args = args });

        builder.Logging.AddSimpleConsole(console => console.SingleLine = true).SetMinimumLevel(LogLevel.Warning);
        builder.Services.Configure<ConsoleLifetimeOptions>(lifetime => lifetime.SuppressStatusMessages = true);

        builder.Services
            .AddOptions<LinterOptions>()
            .Configure(bound =>
            {
                bound.Path = options.Path;
                bound.ReportPath = options.ReportPath;
            })
            .Validate(
                bound => File.Exists(bound.Path) || Directory.Exists(bound.Path),
                $"'{options.Path}' is neither a file nor a directory.")
            .ValidateOnStart();

        builder.Services.AddSingleton<ILocalizationFileFinder, LocalizationFileFinder>();
        builder.Services.AddSingleton<ILocalizationFileLinter, LocalizationFileLinter>();
        builder.Services.AddSingleton<ILintService, LintService>();

        builder.Services.AddSingleton<IDiagnosticReporter>(_ => new ConsoleDiagnosticReporter(Console.Out));

        // The report writer only exists when one was asked for, so nothing has to check for a missing path later.
        if (options.ReportPath is { } report)
        {
            builder.Services.AddSingleton<IDiagnosticReporter>(_ => new JsonDiagnosticReporter(report));
        }

        builder.Services.AddSingleton<LintHostedService>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<LintHostedService>());

        return builder.Build();
    }
}
