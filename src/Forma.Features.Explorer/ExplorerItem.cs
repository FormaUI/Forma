namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>An explorer item wrapped over a resource.</summary>
public class ExplorerItem : IExplorerItem
{
    private const ExplorerItemCapabilities ResourceCapabilities =
        ExplorerItemCapabilities.Drag
        | ExplorerItemCapabilities.Open
        | ExplorerItemCapabilities.View
        | ExplorerItemCapabilities.Move
        | ExplorerItemCapabilities.Rename
        | ExplorerItemCapabilities.Delete;

    private const ExplorerItemCapabilities FolderCapabilities =
        ExplorerItemCapabilities.Drag
        | ExplorerItemCapabilities.Drop
        | ExplorerItemCapabilities.Open
        | ExplorerItemCapabilities.Create
        | ExplorerItemCapabilities.Move
        | ExplorerItemCapabilities.Rename
        | ExplorerItemCapabilities.Delete;

    /// <inheritdoc />
    public required string Key { get; init; }

    /// <inheritdoc />
    public required string Name { get; init; }

    /// <inheritdoc />
    public ExplorerItemCapabilities Capabilities { get; init; }

    /// <inheritdoc />
    public ExplorerItemVisibility Visibility { get; init; } = ExplorerItemVisibility.Visible;

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

    /// <summary>Wraps every resource in the list, in order.</summary>
    public static IReadOnlyList<IExplorerItem> ForAll(IEnumerable<ResourceInfo> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        return [.. resources.Select(IExplorerItem (resource) => For(resource))];
    }

    /// <summary>Wraps the resource or folder, deriving the capabilities its kind supports.</summary>
    public static ExplorerItem For(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        if (resource is FolderResourceInfo folder)
        {
            return ForFolder(folder);
        }

        return new ExplorerItem
        {
            Key = resource.Source,
            Name = resource.Name,
            Capabilities = ResourceCapabilities,
            CreatedAt = resource.CreatedAt,
            ModifiedAt = resource.ModifiedAt,
            Resource = resource
        };
    }

    /// <summary>Wraps the folder, deriving the capabilities its kind supports.</summary>
    public static ExplorerFolderItem ForFolder(FolderResourceInfo folder)
    {
        ArgumentNullException.ThrowIfNull(folder);

        return new ExplorerFolderItem
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
