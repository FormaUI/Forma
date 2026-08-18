namespace Forma.UI.Blazor.Showcase.Features.Upload;

using Forma.Framework.Resources;

/// <summary>A file store for the Showcase that writes accepted files into its sandbox.</summary>
internal sealed class DefaultFileStore(SandboxAssets sandbox) : IFileStore
{
    private const int BufferSize = 64 * 1024;

    private const int PacedBufferSize = 1024;

    /// <summary>Pauses between reads so a transfer is slow enough to watch and to cancel.</summary>
    public TimeSpan ChunkDelay { get; init; }

    /// <summary>Whether the upload directory is emptied before the next file is saved.</summary>
    public bool ClearBeforeSave { get; set; } = true;

    /// <summary>The files currently present in the sandbox upload directory.</summary>
    public IReadOnlyCollection<ResourceInfo> Held
        => Directory
            .EnumerateFiles(sandbox.UploadDirectory)
            .Select(path => new ResourceInfo(SandboxAssets.GetUploadSource(path))
            {
                Size = new FileInfo(path).Length
            })
            .ToList();

    /// <summary>Refuses any file past this many bytes, standing in for a store with a quota.</summary>
    public long? RefusesOver { get; init; }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        await sandbox.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var path = sandbox.GetUploadPath(resource.Name);

            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);

            return true;
        }
        finally
        {
            sandbox.Gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        await sandbox.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (ClearBeforeSave)
            {
                sandbox.ClearUploads();
                ClearBeforeSave = false;
            }

            if (RefusesOver is { } ceiling && content.Size > ceiling)
            {
                throw new InvalidOperationException($"{content.Name} is {content.Size} bytes, past this store's {ceiling} byte limit.");
            }

            var destinationPath = sandbox.GetUploadPath(content.Name);

            try
            {
                var destination = new FileStream(
                    destinationPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    BufferSize,
                    FileOptions.Asynchronous | FileOptions.SequentialScan);

                await using (destination.ConfigureAwait(false))
                {
                    var buffer = new byte[ChunkDelay > TimeSpan.Zero ? PacedBufferSize : BufferSize];
                    int read;

                    while ((read = await content.Stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);

                        if (ChunkDelay > TimeSpan.Zero)
                        {
                            await Task.Delay(ChunkDelay, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
            }
            catch
            {
                Discard(destinationPath);

                throw;
            }

            return new ResourceInfo(SandboxAssets.GetUploadSource(content.Name))
            {
                Size = content.Size
            };
        }
        finally
        {
            sandbox.Gate.Release();
        }
    }

    // Removing the partial file must not replace the failure that caused it.
    private static void Discard(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
