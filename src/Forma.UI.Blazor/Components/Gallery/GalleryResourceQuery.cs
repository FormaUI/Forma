namespace Forma.UI.Components.Gallery;

/// <summary>The window of media a gallery is asking for, described by the state the gallery itself owns.</summary>
/// <param name="Skip">How many matching resources to pass over before the window begins.</param>
/// <param name="Take">How many resources the window holds.</param>
/// <param name="Search">What the reader typed into the gallery's search box, or null when it is empty.</param>
/// <param name="SortBy">Which field the reader is ordering by.</param>
/// <param name="Direction">Which way that order runs.</param>
public readonly record struct GalleryResourceQuery(
    int Skip,
    int Take,
    string? Search,
    GallerySortField SortBy,
    GallerySortDirection Direction);
