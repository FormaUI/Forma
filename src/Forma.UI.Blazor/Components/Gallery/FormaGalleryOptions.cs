namespace Forma.UI.Components.Gallery;

/// <summary>Which operations a <see cref="FormaGallery"/> and its views, toolbar and sidebar offer.</summary>
public sealed record FormaGalleryOptions
{
    /// <summary>Every operation offered.</summary>
    public static FormaGalleryOptions Default { get; } = new();

    /// <summary>Browsing and selection alone, with nothing that changes what is stored.</summary>
    public static FormaGalleryOptions ReadOnly { get; } = new()
    {
        AllowDelete = false,
        AllowRename = false,
        AllowUpload = false,
        AllowFolderCreate = false,
        AllowFolderDelete = false,
        AllowFolderRename = false
    };

    /// <summary>Whether media can be deleted.</summary>
    public bool AllowDelete { get; init; } = true;

    /// <summary>Whether media can be renamed.</summary>
    public bool AllowRename { get; init; } = true;

    /// <summary>Whether new media can be uploaded.</summary>
    public bool AllowUpload { get; init; } = true;

    /// <summary>Whether folders can be created.</summary>
    public bool AllowFolderCreate { get; init; } = true;

    /// <summary>Whether folders can be deleted.</summary>
    public bool AllowFolderDelete { get; init; } = true;

    /// <summary>Whether folders can be renamed.</summary>
    public bool AllowFolderRename { get; init; } = true;

    /// <summary>Which optional columns the details view shows beside the name.</summary>
    public GalleryDetailsColumns DetailsColumns { get; init; } = GalleryDetailsColumns.Kind | GalleryDetailsColumns.Size | GalleryDetailsColumns.Modified;
}
