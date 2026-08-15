namespace Forma.Framework.Localization.Linter.Cli;

/// <summary>Reads the arguments the process was started with.</summary>
public static class CommandLine
{
    private const string DefaultReportName = "localization-report.json";

    /// <summary>How to invoke the linter.</summary>
    public static string Usage
        => """
           Usage: lintloc --path <file-or-directory> [--report [<path>]]

             --path    The file or directory to lint. A directory is searched recursively.
             --report  Write a JSON report. Defaults to localization-report.json in the
                       current directory; a path ending in .json names the file, and any
                       other path names the directory to write the default name into.
           """;

    /// <summary>Reads the arguments, reporting the reason rather than throwing when they do not make sense.</summary>
    public static bool TryParse(string[] args, out LinterOptions? options, out string? error)
    {
        ArgumentNullException.ThrowIfNull(args);

        options = null;
        error = null;

        string? path = null;
        string? report = null;
        var wantsReport = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--path":
                    if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        error = "'--path' needs a file or directory.";

                        return false;
                    }

                    path = args[++index];

                    break;

                case "--report":
                    wantsReport = true;

                    if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        report = args[++index];
                    }

                    break;

                default:
                    error = $"'{args[index]}' is not an argument the linter takes.";

                    return false;
            }
        }

        if (path is null)
        {
            error = "'--path' is required.";

            return false;
        }

        options = new LinterOptions
        {
            Path = Path.GetFullPath(path),
            ReportPath = wantsReport ? ResolveReport(report) : null,
        };

        return true;
    }

    // Every path resolves against the current directory, so a report never lands somewhere the caller did not name.
    private static string ResolveReport(string? report)
    {
        if (report is null)
        {
            return Path.GetFullPath(DefaultReportName);
        }

        return report.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFullPath(report)
            : Path.Combine(Path.GetFullPath(report), DefaultReportName);
    }
}
