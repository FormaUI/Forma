namespace Forma.Tests.Shared;

/// <summary>A directory of a test's own under the repository's ignored <c>.sandbox/tmp</c>, removed with everything written into it on disposal.</summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Creates the directory, its name starting with <paramref name="prefix"/>.</summary>
    /// <param name="prefix">The start of the directory's name, which names what made it.</param>
    public TempDirectory(string prefix)
    {
        ArgumentException.ThrowIfNullOrEmpty(prefix);

        Root = Path.Combine(RepositoryFolder.Find(), ".sandbox", "tmp", $"{prefix}{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>The directory's full path.</summary>
    public string Root { get; }

    /// <inheritdoc />
    /// <remarks>
    /// A process that has just let go of a file can hold it a moment longer, as a stopped browser does its profile, so a
    /// removal that fails is tried again for five seconds; what is still held then stays in <c>.sandbox/tmp</c>, which the
    /// build's clean step removes.
    /// </remarks>
    public void Dispose()
    {
        for (var attempt = 1; attempt <= 50; attempt++)
        {
            try
            {
                Directory.Delete(Root, recursive: true);

                return;
            }
            catch (DirectoryNotFoundException)
            {
                return;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }
        }
    }

    /// <summary>Writes a file into the directory and returns its full path.</summary>
    /// <param name="name">The file's path relative to the directory.</param>
    /// <param name="content">The file's text.</param>
    public string Write(string name, string content)
    {
        var file = Path.Combine(Root, name);
        var directory = Path.GetDirectoryName(file);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(file, content);

        return file;
    }
}
