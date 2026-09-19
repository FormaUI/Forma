namespace Forma.Framework.Catalog;

using Forma.Framework.Catalog.Requests;

/// <summary>What a catalog refuses, each ceiling unlimited when left at zero. Every member pairs with a <see cref="CatalogRejectionReason"/>.</summary>
public sealed record CatalogConstraints
{
    /// <summary>Nothing refused.</summary>
    public static CatalogConstraints None { get; } = new();

    /// <summary>Which resources may be added, as <c>globs:*.jpg;*.pdf</c> or <c>contentType:image;video</c>; anything when null.</summary>
    public string? AcceptedResourceKinds { get; init; }

    /// <summary>How deep folders may nest, counting a top-level folder as one.</summary>
    public int MaxFolderDepth { get; init; }

    /// <summary>How many folders may sit at the top level.</summary>
    public int MaxFolderRootCount { get; init; }

    /// <summary>How many items one folder may hold.</summary>
    public int MaxFolderItemCount { get; init; }

    /// <summary>How many folders may sit directly inside another.</summary>
    public int MaxSubfolderCount { get; init; }

    /// <summary>How many items the catalog may hold in all.</summary>
    public int MaxItemCount { get; init; }

    /// <summary>How many bytes the catalog may hold in all.</summary>
    public long MaxTotalBytes { get; init; }
}
