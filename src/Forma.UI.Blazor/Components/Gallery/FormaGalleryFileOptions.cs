namespace Forma.UI.Blazor.Components.Gallery;

/// <summary>Which operations a <see cref="FormaGallery"/> offers on the media and folders it lists.</summary>
public sealed record FormaGalleryFileOptions
{
    /// <summary>Every operation offered.</summary>
    public static FormaGalleryFileOptions Default { get; } = new();

    /// <summary>Browsing and selection alone, with nothing that changes what is stored.</summary>
    public static FormaGalleryFileOptions ReadOnly { get; } = new()
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
}
