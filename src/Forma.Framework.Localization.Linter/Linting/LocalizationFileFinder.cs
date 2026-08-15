namespace Forma.Framework.Localization.Linter.Linting;

/// <summary>Finds JSON files on disk, ignoring build output.</summary>
public sealed class LocalizationFileFinder : ILocalizationFileFinder
{
    /// <inheritdoc />
    public IEnumerable<string> Find(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);

        if (File.Exists(path))
        {
            return [path];
        }

        // The build copies translations beside the binaries, where linting them would report every file twice.
        return Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories)
            .Where(static file => !file.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Any(static segment => segment is "bin" or "obj"));
    }
}
