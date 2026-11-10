namespace Forma.Framework.Catalog.Queries;

/// <summary>The items that hold no value of the grouped field: no size, or no date.</summary>
/// <param name="Key">The group's key.</param>
public sealed record CatalogMissingGroup(string Key) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
    {
        ArgumentNullException.ThrowIfNull(item);

        return field switch
        {
            CatalogGroupField.Size => item.Resource?.Size is null,
            CatalogGroupField.DateAdded or CatalogGroupField.DateModified => CatalogGrouping.GetDate(item, field) is null,
            _ => false
        };
    }
}
