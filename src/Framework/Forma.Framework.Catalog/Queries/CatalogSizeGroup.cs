namespace Forma.Framework.Catalog.Queries;

/// <summary>The items whose size in bytes is greater than <paramref name="Above"/> and at most <paramref name="AtMost"/>, where a null bound is open.</summary>
/// <param name="Key">The group's key.</param>
/// <param name="Above">The size every item of the group exceeds.</param>
/// <param name="AtMost">The largest size an item of the group has.</param>
public sealed record CatalogSizeGroup(string Key, long? Above, long? AtMost) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
    {
        ArgumentNullException.ThrowIfNull(item);

        return item.Resource?.Size is { } size
            && (Above is not { } above || size > above)
            && (AtMost is not { } most || size <= most);
    }
}
