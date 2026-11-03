namespace Forma.Blazor.Components.DataView;

/// <summary>Compares two items by the key a data view identifies them by.</summary>
/// <typeparam name="TItem">The item compared.</typeparam>
/// <param name="keySelector">Reads an item's key.</param>
internal sealed class ItemKeyComparer<TItem>(Func<TItem, object> keySelector) : IEqualityComparer<TItem>
    where TItem : notnull
{
    /// <inheritdoc />
    public bool Equals(TItem? x, TItem? y)
        => x is null ? y is null : y is not null && keySelector(x).Equals(keySelector(y));

    /// <inheritdoc />
    public int GetHashCode(TItem obj)
        => keySelector(obj).GetHashCode();
}
