namespace Forma.Features.Explorer;

/// <summary>Which operations a <see cref="FormaExplorer"/> permits on the items it lists.</summary>
public sealed record ExplorerItemOptions
{
    private const ExplorerItemCapabilities EveryCapability =
        ExplorerItemCapabilities.Drag
        | ExplorerItemCapabilities.Drop
        | ExplorerItemCapabilities.Read
        | ExplorerItemCapabilities.Write
        | ExplorerItemCapabilities.Open
        | ExplorerItemCapabilities.View
        | ExplorerItemCapabilities.Create
        | ExplorerItemCapabilities.Delete
        | ExplorerItemCapabilities.Move
        | ExplorerItemCapabilities.Rename
        | ExplorerItemCapabilities.Execute;

    private const ExplorerItemCapabilities BrowsingCapabilities =
        ExplorerItemCapabilities.Open | ExplorerItemCapabilities.View;

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
    public ExplorerItemCapabilities AllowedCapabilities { get; init; } = EveryCapability;

    /// <summary>The item capabilities this explorer instance permits on folders.</summary>
    public ExplorerItemCapabilities AllowedFolderCapabilities { get; init; } = EveryCapability;

    /// <summary>Whether new media can be uploaded.</summary>
    public bool AllowUpload { get; init; } = true;
}
