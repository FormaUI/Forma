namespace Forma.Tests.Resources;

using Forma.Framework.Resources;

/// <summary>A file store that reads content in small chunks and resumes off the caller's context.</summary>
public sealed class ChunkedFileStore(int chunkSize = 4) : IFileStore
{
    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
        => ValueTask.FromResult(true);

    /// <inheritdoc />
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        var buffer = new byte[chunkSize];

        while (await content.Stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false) > 0)
        {
            await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        }

        return new ResourceInfo($"/stored/{content.Name}") { Size = content.Size };
    }
}
