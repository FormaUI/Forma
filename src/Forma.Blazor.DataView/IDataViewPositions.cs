namespace Forma.Blazor.Components.DataView;

/// <summary>A virtualizing view whose items stand at positions in an order it reaches beyond the items it draws: the ends of that order, the items between two positions, and a scroll to one.</summary>
/// <typeparam name="TItem">The item the view shows.</typeparam>
internal interface IDataViewPositions<TItem>
    where TItem : notnull
{
    /// <summary>The position of the first or the last item the view reaches, or null where it reaches none.</summary>
    /// <param name="last">Whether the last is asked for rather than the first.</param>
    int? GetEdgePosition(bool last);

    /// <summary>The items the view reaches from <paramref name="from"/> to <paramref name="to"/>, both included, in their order.</summary>
    /// <param name="from">The position of the first item.</param>
    /// <param name="to">The position of the last item.</param>
    /// <param name="cancellationToken">Canceled when the items are no longer wanted.</param>
    ValueTask<IReadOnlyList<TItem>> ReadAsync(int from, int to, CancellationToken cancellationToken);

    /// <summary>Scrolls the item at <paramref name="position"/> into range.</summary>
    /// <param name="position">The item's position.</param>
    /// <param name="cancellationToken">Canceled when the scroll is no longer wanted.</param>
    ValueTask ScrollToPositionAsync(int position, CancellationToken cancellationToken);
}
