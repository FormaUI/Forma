namespace Forma.UI.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>What an explorer has selected, as everything rendering it needs to know and nothing it may change.</summary>
public interface IExplorerSelectionView
{
    /// <summary>Whether the explorer shows selection controls instead of activating previews.</summary>
    bool IsSelectMode { get; }

    /// <summary>How many media resources and folders are selected together.</summary>
    int Count { get; }

    /// <summary>Whether any media resource is selected.</summary>
    bool HasResources { get; }

    /// <summary>Whether any folder is selected.</summary>
    bool HasFolders { get; }

    /// <summary>Whether the resource is selected.</summary>
    bool IsSelected(ResourceInfo resource);

    /// <summary>Whether the folder is selected.</summary>
    bool IsFolderSelected(FolderResourceInfo folder);
}
