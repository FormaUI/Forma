namespace Forma.Framework.Localization.Linter.Linting;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Diagnostics;

using Microsoft.Extensions.Options;

/// <summary>Lints every file the finder yields for the configured path.</summary>
/// <param name="options">What the command line asked for.</param>
/// <param name="finder">Finds the files to read.</param>
/// <param name="linter">Checks one file.</param>
public sealed class LintService(
    IOptions<LinterOptions> options,
    ILocalizationFileFinder finder,
    ILocalizationFileLinter linter) : ILintService
{
    private readonly LinterOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    private readonly ILocalizationFileFinder _finder = finder ?? throw new ArgumentNullException(nameof(finder));
    private readonly ILocalizationFileLinter _linter = linter ?? throw new ArgumentNullException(nameof(linter));

    /// <inheritdoc />
    public async ValueTask<LintResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var diagnostics = new List<LintDiagnostic>();
        var files = 0;

        foreach (var file in _finder.Find(_options.Path))
        {
            cancellationToken.ThrowIfCancellationRequested();
            files++;

            diagnostics.AddRange(await _linter.LintAsync(file, cancellationToken).ConfigureAwait(false));
        }

        return new LintResult(
            [.. diagnostics
                .OrderBy(diagnostic => diagnostic.Path, StringComparer.Ordinal)
                .ThenBy(diagnostic => diagnostic.Line)
                .ThenBy(diagnostic => diagnostic.Column)],
            files);
    }
}
