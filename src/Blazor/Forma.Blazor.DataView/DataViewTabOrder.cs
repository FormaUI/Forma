namespace Forma.Blazor.Components.DataView;

/// <summary>Which items' own controls Tab reaches in a data view.</summary>
public enum DataViewTabOrder
{
    /// <summary>Every item's controls, in the order they stand in.</summary>
    EveryItem,

    /// <summary>The active item's controls alone, the item holding the focus or else the collection's one tab stop, so Tab leaves the collection after one item's controls.</summary>
    ActiveItem,
}
