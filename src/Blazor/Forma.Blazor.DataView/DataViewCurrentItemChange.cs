namespace Forma.Blazor.Components.DataView;

/// <summary>A change of a data view's current node on its way, which its host can refuse before anything changes.</summary>
/// <typeparam name="TItem">The type of the items.</typeparam>
/// <param name="item">The node that would become current, where null is the roots.</param>
/// <param name="previousItem">The node current until now, where null is the roots.</param>
public sealed class DataViewCurrentItemChange<TItem>(TItem? item, TItem? previousItem)
{
    /// <summary>The node that would become current, where null is the roots.</summary>
    public TItem? Item { get; } = item;

    /// <summary>The node current until now, where null is the roots.</summary>
    public TItem? PreviousItem { get; } = previousItem;

    /// <summary>Whether the host refused the change.</summary>
    public bool IsRefused { get; private set; }

    /// <summary>Refuses the change, which leaves the current node, the page and the items as they are.</summary>
    public void Refuse()
        => IsRefused = true;
}
