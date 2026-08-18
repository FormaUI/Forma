namespace Forma.UI.Components.Gallery;

/// <summary>What a <see cref="FormaGallery"/> refuses, each ceiling unlimited when left at zero. Every member pairs with a <see cref="GalleryRejectionReason"/>.</summary>
public sealed record FormaGalleryConstraints
{
    /// <summary>Nothing refused.</summary>
    public static FormaGalleryConstraints None { get; } = new();

    /// <summary>Which resources may be added, as <c>globs:*.jpg;*.pdf</c> or <c>contentType:image;video</c>; anything when null.</summary>
    public string? AcceptedResourceKinds { get; init; }

    /// <summary>How deep folders may nest, counting a top-level folder as one.</summary>
    public int MaxFolderDepth { get; init; }

    /// <summary>How many folders may sit at the top level.</summary>
    public int MaxFolderRootCount { get; init; }

    /// <summary>How many resources one folder may hold.</summary>
    public int MaxFolderResourceCount { get; init; }

    /// <summary>How many folders may sit directly inside another.</summary>
    public int MaxSubfolderCount { get; init; }

    /// <summary>How many resources the gallery may hold in all.</summary>
    public int MaxResourceCount { get; init; }

    /// <summary>How many bytes the gallery may hold in all.</summary>
    public long MaxTotalBytes { get; init; }
}
