namespace Forma.Framework.Localization.Linter.Linting;

using Forma.Framework.Localization.Linter.Diagnostics;

/// <summary>Checks one file.</summary>
public interface ILocalizationFileLinter
{
    /// <summary>Every diagnostic a file produces, which is none for JSON that is not a localization file.</summary>
    /// <param name="file">The file to read.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    ValueTask<IReadOnlyList<LintDiagnostic>> LintAsync(string file, CancellationToken cancellationToken = default);
}
