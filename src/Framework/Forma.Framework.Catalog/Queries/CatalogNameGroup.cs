namespace Forma.Framework.Catalog.Queries;

/// <summary>The items whose name begins with a character from <paramref name="First"/> to <paramref name="Last"/>, both included, compared in invariant upper case.</summary>
/// <param name="Key">The group's key.</param>
/// <param name="First">The first character of the range.</param>
/// <param name="Last">The last character of the range.</param>
public sealed record CatalogNameGroup(string Key, char First, char Last) : CatalogGroup(Key)
{
    /// <inheritdoc />
    public override bool Admits(ICatalogItem item, CatalogGroupField field)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrEmpty(item.Name))
        {
            return false;
        }

        var first = char.ToUpperInvariant(item.Name[0]);

        return first >= First && first <= Last;
    }
}
