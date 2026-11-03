namespace Forma.Tests.Shared;

/// <summary>A temporary directory, removed with everything written into it on disposal.</summary>
public sealed class TempDirectory : IDisposable
{
    /// <summary>Creates the directory under the system temporary path.</summary>
    public TempDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), $"forma-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Root);
    }

    /// <summary>The directory's full path.</summary>
    public string Root { get; }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
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
