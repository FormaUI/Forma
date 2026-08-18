namespace Forma.UI.Blazor.Showcase.Features.Upload;

internal sealed class SandboxAssets
{
    private const string AssetsFolder = "Assets";

    private const string SandboxFolder = "Sandbox";

    private const string UploadFolder = "Upload";

    private static readonly (string Name, long Size)[] AssetDefinitions =
    [
        ("sample-4-kb.bin", 4L * 1024),
        ("sample-256-kb.bin", 256L * 1024),
        ("sample-2-mb.bin", 2L * 1024 * 1024),
        ("sample-100-mb.bin", 100L * 1024 * 1024),
        ("sample-1-gb.bin", 1L * 1024 * 1024 * 1024)
    ];

    /// <summary>Creates the upload sandbox and its generated fixture files.</summary>
    public SandboxAssets(string contentRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentRootPath);

        var root = Path.GetFullPath(contentRootPath);
        var projectFolder = Path.GetFileName(Path.TrimEndingDirectorySeparator(root));

        AssetDirectory = Path.Combine(root, SandboxFolder, AssetsFolder);
        UploadDirectory = Path.Combine(root, SandboxFolder, UploadFolder);
        AssetDisplayPath = Path.Combine(projectFolder, SandboxFolder, AssetsFolder);
        UploadDisplayPath = Path.Combine(projectFolder, SandboxFolder, UploadFolder);

        Directory.CreateDirectory(AssetDirectory);
        Directory.CreateDirectory(UploadDirectory);

        foreach (var (name, size) in AssetDefinitions)
        {
            EnsureAsset(name, size);
        }
    }

    /// <summary>Directory containing generated files for picker exercises.</summary>
    public string AssetDirectory { get; }

    /// <summary>The asset directory written from the project folder down, for display.</summary>
    public string AssetDisplayPath { get; }

    /// <summary>Serializes changes to the shared upload directory.</summary>
    public SemaphoreSlim Gate { get; } = new(1, 1);

    /// <summary>Directory receiving files written by the Showcase uploader.</summary>
    public string UploadDirectory { get; }

    /// <summary>The upload directory written from the project folder down, for display.</summary>
    public string UploadDisplayPath { get; }

    /// <summary>Removes every file from the upload directory.</summary>
    public void ClearUploads()
    {
        foreach (var path in Directory.EnumerateFiles(UploadDirectory))
        {
            File.Delete(path);
        }
    }

    /// <summary>Resolves a file name beneath the upload directory.</summary>
    public string GetUploadPath(string name)
    {
        var fileName = Path.GetFileName(name);

        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var path = Path.GetFullPath(Path.Combine(UploadDirectory, fileName));

        // A browser-supplied name such as ".." survives GetFileName, so the resolved path decides.
        if (!path.StartsWith(UploadDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new ArgumentException($"{name} does not name a file inside the upload directory.", nameof(name));
        }

        return path;
    }

    /// <summary>Writes an upload's address relative to the content root.</summary>
    public static string GetUploadSource(string name)
        => Path.Combine(SandboxFolder, UploadFolder, Path.GetFileName(name));

    private void EnsureAsset(string name, long size)
    {
        var path = Path.Combine(AssetDirectory, name);

        if (File.Exists(path) && new FileInfo(path).Length == size)
        {
            return;
        }

        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);

        stream.SetLength(size);
    }
}
