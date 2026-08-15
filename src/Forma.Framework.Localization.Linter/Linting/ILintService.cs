namespace Forma.Framework.Localization.Linter.Linting;

using Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Runs the linter over everything the options name.</summary>
public interface ILintService
{
    /// <summary>Lints every file found, ordering what it finds.</summary>
    /// <param name="cancellationToken">Cancels the run.</param>
    ValueTask<LintResult> RunAsync(CancellationToken cancellationToken = default);
}
