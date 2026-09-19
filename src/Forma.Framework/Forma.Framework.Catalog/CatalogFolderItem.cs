namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>A catalog item representing a folder.</summary>
public sealed class CatalogFolderItem : CatalogItem, ICatalogFolderItem
{
    /// <inheritdoc />
    public string? ParentKey { get; init; }

    /// <inheritdoc />
    public int? Count { get; init; }

    /// <inheritdoc />
    public new FolderResourceInfo? Resource
    {
        get => (FolderResourceInfo?)base.Resource;
        init => base.Resource = value;
    }
}
