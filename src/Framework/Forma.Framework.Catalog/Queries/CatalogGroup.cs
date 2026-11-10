namespace Forma.Framework.Catalog.Queries;

/// <summary>A group of a <see cref="CatalogGrouping"/>: its key, and the rule an item's value of the grouped field meets to fall in it.</summary>
/// <param name="Key">The group's key, the same in every culture and on every day.</param>
public abstract record CatalogGroup(string Key)
{
    /// <summary>Whether the item falls in the group when grouped by <paramref name="field"/>.</summary>
    /// <param name="item">The item.</param>
    /// <param name="field">The field the items are grouped by.</param>
    public abstract bool Admits(ICatalogItem item, CatalogGroupField field);
}
