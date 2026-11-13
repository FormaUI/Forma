namespace Forma.Blazor.Browser.Components.Checks.Services;

using Forma.Framework.Resources;

/// <summary>A file store that holds the content of each file it saves in memory, under the address it gives the file, and refuses a file over its ceiling.</summary>
public sealed class MemoryFileStore : IFileStore
{
    private readonly Dictionary<string, byte[]> _contents = new(StringComparer.Ordinal);

    /// <summary>The most bytes the store takes in one file, where null takes any size.</summary>
    public long? MaxFileBytes { get; init; }

    /// <inheritdoc />
    public ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return ValueTask.FromResult(_contents.Remove(resource.Source));
    }

    /// <inheritdoc />
    public async ValueTask<FileSaveResult> SaveAsync(FileContent content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (MaxFileBytes is { } ceiling && content.Size > ceiling)
        {
            return FileSaveResult.Refused(new FileRefusal(FileRefusalReason.TooLarge, ceiling));
        }

        using var buffer = new MemoryStream();
        await content.Stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        var source = $"memory/{content.Name}";
        _contents[source] = buffer.ToArray();

        return FileSaveResult.Saved(new FileResourceInfo(source) { ContentType = content.ContentType, Size = buffer.Length });
    }
}
