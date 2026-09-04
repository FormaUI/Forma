namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Resources;

/// <summary>An explorer item representing a folder.</summary>
public sealed class ExplorerFolderItem : ExplorerItem, IExplorerFolderItem
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
