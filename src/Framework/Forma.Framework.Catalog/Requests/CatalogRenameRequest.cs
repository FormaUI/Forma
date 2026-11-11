namespace Forma.Framework.Catalog.Requests;

/// <summary>A request to rename an item, a file or a folder, which the host can report it failed to carry out.</summary>
/// <param name="item">The item to rename.</param>
/// <param name="newName">The new name of the item.</param>
public sealed class CatalogRenameRequest(ICatalogItem item, string newName)
{
    /// <summary>The item to rename.</summary>
    public ICatalogItem Item { get; } = item;

    /// <summary>The new name of the item.</summary>
    public string NewName { get; } = newName;

    /// <summary>Whether the host reported that it failed to carry the request out.</summary>
    public bool IsFailed { get; private set; }

    /// <summary>Reports that the request was not carried out.</summary>
    public void Fail()
        => IsFailed = true;
}
