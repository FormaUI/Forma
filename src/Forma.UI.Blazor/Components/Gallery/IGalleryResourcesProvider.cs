namespace Forma.UI.Components.Gallery;

/// <summary>Answers a gallery with one window of media at a time, for collections too large to hold.</summary>
public interface IGalleryResourcesProvider
{
    /// <summary>The window the query asks for. Anything the gallery does not own, such as the folder being browsed, belongs to the implementation.</summary>
    ValueTask<GalleryResourcePage> GetResourcesAsync(GalleryResourceQuery query, CancellationToken cancellationToken);
}
