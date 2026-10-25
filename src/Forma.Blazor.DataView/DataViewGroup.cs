namespace Forma.Blazor.Components.DataView;

/// <summary>A group of the data view's items: the key they fold under, the items of it on screen, how many items it holds, and the name its heading shows.</summary>
/// <typeparam name="TItem">The item the group holds.</typeparam>
public sealed record DataViewGroup<TItem>
{
    private readonly Func<object?, string> _nameSelector;

    internal DataViewGroup(object? key, Func<object?, string> nameSelector, IReadOnlyList<TItem> items)
    {
        Key = key;
        _nameSelector = nameSelector;
        Items = items;
        Count = items.Count;
    }

    /// <summary>The key the group's items share.</summary>
    public object? Key { get; }

    /// <summary>The group's items on screen, in the order shown, where empty means a virtualizing view fetches them as it scrolls.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>How many items the group holds.</summary>
    public int Count { get; init; }

    /// <summary>The name the group's heading shows, in the culture in force.</summary>
    /// <remarks>
    /// Each read runs the grouping's name callback for the group's key, synchronously, so a heading that reads it as it renders
    /// runs the callback once on every render, in the culture in force then.
    /// </remarks>
    public string DisplayName
        => _nameSelector(Key);
}
