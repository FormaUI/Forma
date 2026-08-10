namespace Forma.Framework.Resources;

/// <summary>Stores a file's content wherever the application keeps its files.</summary>
public interface IFileStore
{
    /// <summary>Removes a resource this store previously saved, so a failed set can be undone.</summary>
    ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken);

    /// <summary>Stores one file's content, returning the resource it became.</summary>
    ValueTask<ResourceInfo> SaveAsync(FileContent content, CancellationToken cancellationToken);
}
