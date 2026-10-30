namespace Forma.Blazor.Browser.Components.Client.Services;

using Forma.Framework.Resources;

/// <summary>A file store that holds the content of each file it saves in memory, under the address it gives the file.</summary>
public sealed class MemoryFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _contents = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return ValueTask.FromResult(_contents.Remove(resource.Source));
    }

    /// <inheritdoc />
    public async ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        using var buffer = new MemoryStream();
        await content.Stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var source = $"memory/{content.Name}";
        _contents[source] = buffer.ToArray();

        return new FileResourceInfo(source) { ContentType = content.ContentType, Size = buffer.Length };
    }
}
