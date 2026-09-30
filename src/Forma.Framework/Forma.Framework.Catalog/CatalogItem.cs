namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>A catalog item, which can wrap a resource.</summary>
public class CatalogItem : ICatalogItem
{
    private const CatalogItemCapabilities ResourceCapabilities =
        CatalogItemCapabilities.Drag
        | CatalogItemCapabilities.Open
        | CatalogItemCapabilities.View
        | CatalogItemCapabilities.Move
        | CatalogItemCapabilities.Rename
        | CatalogItemCapabilities.Delete;

    private const CatalogItemCapabilities FolderCapabilities =
        CatalogItemCapabilities.Drag
        | CatalogItemCapabilities.Drop
        | CatalogItemCapabilities.Open
        | CatalogItemCapabilities.Create
        | CatalogItemCapabilities.Move
        | CatalogItemCapabilities.Rename
        | CatalogItemCapabilities.Delete;

    /// <inheritdoc />
    public required string Key { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public CatalogItemCapabilities Capabilities { get; init; }

    /// <inheritdoc />
    public CatalogItemVisibility Visibility { get; init; } = CatalogItemVisibility.Visible;

    /// <inheritdoc />
    public bool IsDisabled { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? CreatedAt { get; init; }

    /// <inheritdoc />
    public DateTimeOffset? ModifiedAt { get; init; }

    /// <inheritdoc />
    public string? ThumbnailSource { get; init; }

    /// <inheritdoc />
    public ResourceInfo? Resource { get; init; }

    /// <summary>Creates a catalog item for each of the resources, in order.</summary>
    public static IReadOnlyList<ICatalogItem> ForAll(IEnumerable<ResourceInfo> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        return [.. resources.Select(ICatalogItem (resource) => For(resource))];
    }

    /// <summary>Creates the catalog item for the resource, with the capabilities of its kind.</summary>
    public static CatalogItem For(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource is FolderResourceInfo folder)
        {
            return ForFolder(folder);
        }

        return new CatalogItem
        {
            Key = resource.Source,
            Name = resource.Name,
            Capabilities = ResourceCapabilities,
            CreatedAt = resource.CreatedAt,
            ModifiedAt = resource.ModifiedAt,
            Resource = resource
        };
    }

    /// <summary>Creates the catalog folder item for the folder, with the capabilities of a folder.</summary>
    public static CatalogFolderItem ForFolder(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return new CatalogFolderItem
        {
            Key = folder.Source,
            Name = folder.Name,
            Capabilities = FolderCapabilities,
            CreatedAt = folder.CreatedAt,
            ModifiedAt = folder.ModifiedAt,
            ParentKey = folder.Parent,
            Count = folder.Count,
            Resource = folder
        };
    }
}
