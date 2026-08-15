namespace Forma.Framework.Localization.Linter.Tests.Support.Linting;

using Forma.Framework.Localization.Linter.Cli;
using Forma.Framework.Localization.Linter.Linting;
using Forma.Framework.Localization.Linter.Sources;

using Microsoft.Extensions.Options;

/// <summary>Builds a lint service over the real file system.</summary>
public static class LintServiceTestFactory
{
    /// <summary>A service that lints the file or directory named, without loading any project.</summary>
    /// <param name="path">What to lint.</param>
    public static LintService Create(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        return new LintService(
            Options.Create(new LinterOptions { Path = path }),
            new LintTargetResolver(),
            new LocalizationFileLinter(),
            new StubProjectLoader(),
            new SourceLinter(new SemanticCallSiteFinder(), new SyntacticCallSiteFinder()));
    }
}
