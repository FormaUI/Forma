namespace Forma.Tests.Linter.Linting;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Linting;

using Microsoft.Extensions.Options;

/// <summary>Builds a lint service over the real file system.</summary>
public static class LintServiceTestFactory
{
    /// <summary>A service that lints the file or directory named.</summary>
    /// <param name="path">What to lint.</param>
    public static LintService Create(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new LintService(
            Options.Create(new LinterOptions { Path = path }),
            new LocalizationFileFinder(),
            new LocalizationFileLinter());
    }
}
