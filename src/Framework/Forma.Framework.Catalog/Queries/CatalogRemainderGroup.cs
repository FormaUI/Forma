namespace Forma.Framework.Catalog.Queries;

/// <summary>The items no other group of the grouping admits.</summary>
/// <param name="Key">The group's key.</param>
public sealed record CatalogRemainderGroup(string Key) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
        => true;
}
