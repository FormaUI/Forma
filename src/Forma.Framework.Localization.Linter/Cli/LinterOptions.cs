namespace Forma.Framework.Localization.Linter.Cli;

/// <summary>What the command line asked the linter to do.</summary>
public sealed class LinterOptions
{
    /// <summary>The file or directory to lint.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Where to write the JSON report, or null when none was asked for.</summary>
    public string? ReportPath { get; set; }
}
