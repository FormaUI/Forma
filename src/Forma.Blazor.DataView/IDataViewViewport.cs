namespace Forma.Blazor.Components.DataView;

/// <summary>The viewport a view keeps over the items, which the data view asks to move.</summary>
public interface IDataViewViewport
{
    /// <summary>Scrolls the viewport so the item at <paramref name="index"/> is aligned to its start.</summary>
    /// <param name="index">The index of the item in the whole set.</param>
    /// <param name="cancellationToken">Canceled when the scroll is no longer wanted.</param>
    ValueTask ScrollToItemAsync(int index, CancellationToken cancellationToken);

    /// <summary>Fetches the slices on screen again, after the data view's source, sort or set changed.</summary>
    ValueTask RefreshAsync();
}
