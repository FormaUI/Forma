namespace Forma.Framework.Resources;

/// <summary>Stores a file's content wherever the application keeps its files.</summary>
public interface IFileStore
{
    /// <summary>Removes a resource this store saved.</summary>
    ValueTask<bool> DeleteAsync(ResourceInfo resource, CancellationToken cancellationToken);

    /// <summary>Stores one file's content, returning the resource it became or the store's refusal of it.</summary>
    /// <remarks>A file the store turns away for its type, its size or its dimensions is refused, with the limit it went past; an exception is a failure.</remarks>
    ValueTask<FileSaveResult> SaveAsync(FileContent content, CancellationToken cancellationToken);
}
