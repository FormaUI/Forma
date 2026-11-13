namespace Forma.Framework.Catalog;

using Forma.Framework.Catalog.Requests;

/// <summary>The limits on what a catalog accepts, where a ceiling of zero is unlimited.</summary>
public sealed record CatalogConstraints
{
    /// <summary>The constraints with no limits.</summary>
    public static CatalogConstraints None { get; } = new();

    /// <summary>The resource kinds the catalog lists and accepts, as <c>globs:*.jpg;*.pdf</c> or <c>contentType:image;video;folder</c>; null accepts any.</summary>
    public string? AcceptedResourceKinds { get; init; }

    /// <summary>The resource kinds an upload may add, in the same syntax, where null is <see cref="AcceptedResourceKinds"/>.</summary>
    public string? AcceptedUploadKinds { get; init; }

    /// <summary>The maximum folder depth, where a top-level folder is at depth one.</summary>
    public int MaxFolderDepth { get; init; }

    /// <summary>The maximum number of top-level folders.</summary>
    public int MaxFolderRootCount { get; init; }

    /// <summary>The maximum number of items in one folder.</summary>
    public int MaxFolderItemCount { get; init; }

    /// <summary>The maximum number of subfolders directly inside one folder.</summary>
    public int MaxSubfolderCount { get; init; }

    /// <summary>The maximum number of items in the catalog.</summary>
    public int MaxItemCount { get; init; }

    /// <summary>The maximum total bytes of the catalog.</summary>
    public long MaxTotalBytes { get; init; }

    /// <summary>The maximum bytes of one file.</summary>
    public long MaxFileBytes { get; init; }
}
