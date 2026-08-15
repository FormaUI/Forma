namespace Forma.Framework.Localization.Linter.Cli;

/// <summary>Turns what the command line named into the files a run reads.</summary>
public interface ILintTargetResolver
{
    /// <summary>The files the path names, by extension.</summary>
    /// <param name="path">The file or directory the command line named.</param>
    LintTarget Resolve(string path);
}
