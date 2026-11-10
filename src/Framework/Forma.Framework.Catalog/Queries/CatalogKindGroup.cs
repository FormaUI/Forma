namespace Forma.Framework.Catalog.Queries;

/// <summary>The items of one kind.</summary>
/// <param name="Key">The group's key.</param>
/// <param name="Kind">The kind of the group's items.</param>
public sealed record CatalogKindGroup(string Key, CatalogItemKind Kind) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.GetKind() == Kind;
    }
}
