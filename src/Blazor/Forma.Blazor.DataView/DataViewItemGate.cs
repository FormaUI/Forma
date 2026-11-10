namespace Forma.Blazor.Components.DataView;

/// <summary>Decides whether the component of one item has to draw again: only when something it was last drawn by has changed.</summary>
internal struct DataViewItemGate
{
    private object? _item;
    private int? _position;
    private int _viewRevision;
    private int _cultureRevision;
    private int _state;
    private object? _shape;

    /// <summary>Whether a draw has been recorded.</summary>
    public bool IsRecorded { get; private set; }

    /// <summary>Whether what the item is to be drawn by differs from what it was last drawn by.</summary>
    /// <param name="item">The item the component shows.</param>
    /// <param name="position">The item's place in the whole set, where the view knows it.</param>
    /// <param name="viewRevision">The revision of what the item's view draws every item by.</param>
    /// <param name="cultureRevision">The revision of the culture state the item is drawn in.</param>
    /// <param name="state">The item's own state, one bit for each thing about it that changes what is drawn.</param>
    /// <param name="shape">Where the item stands in a hierarchy, for a view whose items stand in one.</param>
    public readonly bool Differs(object? item, int? position, int viewRevision, int cultureRevision, int state, object? shape = null)
        => !IsRecorded || !ReferenceEquals(_item, item) || _position != position || _viewRevision != viewRevision || _cultureRevision != cultureRevision || _state != state || !Equals(_shape, shape);

    /// <summary>Records what the item is being drawn by.</summary>
    /// <param name="item">The item the component shows.</param>
    /// <param name="position">The item's place in the whole set, where the view knows it.</param>
    /// <param name="viewRevision">The revision of what the item's view draws every item by.</param>
    /// <param name="cultureRevision">The revision of the culture state the item is drawn in.</param>
    /// <param name="state">The item's own state, one bit for each thing about it that changes what is drawn.</param>
    /// <param name="shape">Where the item stands in a hierarchy, for a view whose items stand in one.</param>
    public void Record(object? item, int? position, int viewRevision, int cultureRevision, int state, object? shape = null)
    {
        _item = item;
        _position = position;
        _viewRevision = viewRevision;
        _cultureRevision = cultureRevision;
        _state = state;
        _shape = shape;
        IsRecorded = true;
    }

    /// <summary>How many bits the shared state takes, which a view's own bits stand above.</summary>
    public const int SharedBitCount = 5;

    /// <summary>The state bits shared by every view's items: whether the item is selected, whether its own selection control can act, whether it is drawn muted, whether it can be selected, and whether it can take the focus.</summary>
    /// <typeparam name="TItem">The item.</typeparam>
    /// <param name="view">The view the item is shown by.</param>
    /// <param name="item">The item.</param>
    /// <returns>The bits.</returns>
    public static int GetState<TItem>(IDataView<TItem> view, TItem item)
        where TItem : notnull
        => (view.IsSelected(item) ? 1 : 0)
            | (view.CanInteractWith(item) ? 2 : 0)
            | (view.IsMuted(item) ? 4 : 0)
            | (view.IsSelectable(item) ? 8 : 0)
            | (view.CanFocus(item) ? 16 : 0);
}
