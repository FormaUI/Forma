namespace Forma.Framework.Catalog.Requests;

/// <summary>A request to move items, files and folders together, into a target folder, which the host can report it failed to carry out.</summary>
/// <param name="items">The items being dropped.</param>
/// <param name="targetFolder">The target folder, where null is the top level.</param>
public sealed class CatalogDropRequest(IReadOnlyList<ICatalogItem> items, ICatalogFolderItem? targetFolder)
{
    /// <summary>The items being dropped.</summary>
    public IReadOnlyList<ICatalogItem> Items { get; } = items;

    /// <summary>The target folder, where null is the top level.</summary>
    public ICatalogFolderItem? TargetFolder { get; } = targetFolder;

    /// <summary>Whether the host reported that it failed to carry the request out.</summary>
    public bool IsFailed { get; private set; }

    /// <summary>Reports that the request was not carried out, which leaves the selection as it was.</summary>
    public void Fail()
        => IsFailed = true;
}
