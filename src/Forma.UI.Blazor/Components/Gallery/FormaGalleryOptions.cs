namespace Forma.UI.Components.Gallery;

/// <summary>How a <see cref="FormaGallery"/> presents itself: which surfaces it shows, and how it moves through a collection.</summary>
public sealed record FormaGalleryOptions
{
    /// <summary>The standard presentation.</summary>
    public static FormaGalleryOptions Default { get; } = new();

    /// <summary>Whether the gallery is presented as dialog content, without a frame of its own.</summary>
    public bool AsDialog { get; init; }

    /// <summary>Whether the reader can resize the gallery.</summary>
    public bool Resizable { get; init; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    public bool ShowSidebar { get; init; } = true;

    /// <summary>Whether each breadcrumb offers a menu of that folder's children.</summary>
    public bool ShowFolderMenu { get; init; } = true;

    /// <summary>Whether reaching the end of the list appends the next page, and whether a button does so instead.</summary>
    public GalleryInfiniteScrollMode InfiniteScroll { get; init; } = GalleryInfiniteScrollMode.Off;

    /// <summary>Which optional columns the details view shows beside the name.</summary>
    public GalleryDetailsColumns DetailsColumns { get; init; } = GalleryDetailsColumns.Kind | GalleryDetailsColumns.Size | GalleryDetailsColumns.Modified;
}
