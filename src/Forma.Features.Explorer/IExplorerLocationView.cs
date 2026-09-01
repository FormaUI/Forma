namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>An explorer's location in its folders, as the components drawing it read it.</summary>
public interface IExplorerLocationView
{
    /// <summary>Whether a previous folder is available to return to.</summary>
    bool CanGoBack { get; }

    /// <summary>Whether a folder left by going back is available to return to.</summary>
    bool CanGoForward { get; }

    /// <summary>Whether the current folder has a parent to go up to.</summary>
    bool CanGoUp { get; }

    /// <summary>The path from the root to the current folder.</summary>
    IReadOnlyList<FolderResourceInfo> Breadcrumbs { get; }
}
