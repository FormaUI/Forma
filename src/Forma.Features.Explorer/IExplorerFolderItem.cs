namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>An explorer item representing a folder.</summary>
public interface IExplorerFolderItem : IExplorerItem
{
    /// <summary>The folder this one sits inside, or null when it is top-level.</summary>
    IExplorerFolderItem? Parent { get; }

    /// <summary>How many items the folder holds, or null when unknown.</summary>
    int? Count { get; }

    /// <summary>The underlying folder resource, or null when the item does not represent one.</summary>
    new FolderResourceInfo? Resource { get; }
}
