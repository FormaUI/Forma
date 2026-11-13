namespace Forma.Blazor.Showcase.Features.Upload;

using Forma.Framework.Resources;

/// <summary>A file store for the Showcase that writes accepted files into its sandbox.</summary>
internal sealed class DefaultFileStore(SandboxAssets sandbox) : IFileStore
{
    private const int BufferSize = 64 * 1024;

    private const int PacedBufferSize = 1024;

    /// <summary>The pause between reads.</summary>
    public TimeSpan ChunkDelay { get; init; }

    /// <summary>Whether the upload directory is emptied before the next file is saved.</summary>
    public bool ClearBeforeSave { get; set; } = true;

    /// <summary>The files in the sandbox upload directory.</summary>
    public IReadOnlyCollection<ResourceInfo> HeldResources
        => Directory
            .EnumerateFiles(sandbox.UploadDirectory)
            .Select(path => new ResourceInfo(SandboxAssets.GetUploadSource(path))
            {
                Size = new FileInfo(path).Length
            })
            .ToList();

    /// <summary>The size in bytes past which the store refuses a file, where null is no limit.</summary>
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
    public async ValueTask<FileSaveResult> SaveAsync(FileContent content, CancellationToken cancellationToken)
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
                return FileSaveResult.Refused(new FileRefusal(FileRefusalReason.TooLarge, ceiling));
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
                    int bytesRead;

                    while ((bytesRead = await content.Stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
                    {
                        await destination.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);

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

            return FileSaveResult.Saved(new ResourceInfo(SandboxAssets.GetUploadSource(content.Name))
            {
                Size = content.Size
            });
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
