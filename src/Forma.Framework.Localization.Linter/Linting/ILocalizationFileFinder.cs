namespace Forma.Framework.Localization.Linter.Linting;

/// <summary>Finds the files a run should read.</summary>
public interface ILocalizationFileFinder
{
    /// <summary>The files under a path, which may name a single file.</summary>
    /// <param name="path">The file or directory to search.</param>
    IEnumerable<string> Find(string path);
}
