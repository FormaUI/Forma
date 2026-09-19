namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

/// <summary>Which operations a <see cref="FormaExplorer"/> permits on the items it lists.</summary>
public sealed record ExplorerItemOptions
{
    private const CatalogItemCapabilities EveryCapability =
        CatalogItemCapabilities.Drag
        | CatalogItemCapabilities.Drop
        | CatalogItemCapabilities.Read
        | CatalogItemCapabilities.Write
        | CatalogItemCapabilities.Open
        | CatalogItemCapabilities.View
        | CatalogItemCapabilities.Create
        | CatalogItemCapabilities.Delete
        | CatalogItemCapabilities.Move
        | CatalogItemCapabilities.Rename
        | CatalogItemCapabilities.Execute;

    private const CatalogItemCapabilities BrowsingCapabilities =
        CatalogItemCapabilities.Open | CatalogItemCapabilities.View;

    /// <summary>Every operation permitted.</summary>
    public static ExplorerItemOptions Default { get; } = new();

    /// <summary>Browsing and selection alone, with nothing that changes what is stored.</summary>
    public static ExplorerItemOptions ReadOnly { get; } = new()
    {
        AllowedCapabilities = BrowsingCapabilities,
        AllowedFolderCapabilities = BrowsingCapabilities,
        AllowUpload = false
    };

    /// <summary>The item capabilities this explorer instance permits.</summary>
    public CatalogItemCapabilities AllowedCapabilities { get; init; } = EveryCapability;

    /// <summary>The item capabilities this explorer instance permits on folders.</summary>
    public CatalogItemCapabilities AllowedFolderCapabilities { get; init; } = EveryCapability;

    /// <summary>Whether new media can be uploaded.</summary>
    public bool AllowUpload { get; init; } = true;
}
