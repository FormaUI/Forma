namespace Forma.Framework.Catalog.Requests;

/// <summary>A request to delete items, files and folders together, which the host can report it failed to carry out.</summary>
/// <param name="items">The items to delete.</param>
public sealed class CatalogDeleteRequest(IReadOnlyList<ICatalogItem> items)
{
    /// <summary>The items to delete.</summary>
    public IReadOnlyList<ICatalogItem> Items { get; } = items;

    /// <summary>Whether the host reported that it failed to carry the request out.</summary>
    public bool IsFailed { get; private set; }

    /// <summary>Reports that the request was not carried out, which leaves the selection as it was.</summary>
    public void Fail()
        => IsFailed = true;
}
