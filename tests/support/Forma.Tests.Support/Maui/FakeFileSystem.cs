namespace Forma.Tests.Maui;

using Microsoft.Maui.Storage;

/// <summary>An application's file system whose cache is a folder the test names.</summary>
/// <param name="cacheDirectory">The folder the cache stands in.</param>
public sealed class FakeFileSystem(string cacheDirectory) : IFileSystem
{
    /// <inheritdoc />
    public string CacheDirectory
        => cacheDirectory;

    /// <inheritdoc />
    public string AppDataDirectory
        => cacheDirectory;

    /// <inheritdoc />
    public Task<Stream> OpenAppPackageFileAsync(string filename)
        => throw new FileNotFoundException("The test's application packages no file.", filename);

    /// <inheritdoc />
    public Task<bool> AppPackageFileExistsAsync(string filename)
        => Task.FromResult(false);
}
