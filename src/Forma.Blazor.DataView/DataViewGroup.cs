namespace Forma.Blazor.Components.DataView;

using System.Globalization;

using Forma.Framework.Globalization;

/// <summary>A group of the data view's items: the key they fold under, the items of it on screen, how many items it holds, and the name its heading shows.</summary>
/// <typeparam name="TItem">The item the group holds.</typeparam>
public sealed record DataViewGroup<TItem>
{
    private readonly Func<object?, CultureInfo, string> _nameSelector;
    private readonly ICultureState _cultureState;

    internal DataViewGroup(object? key, Func<object?, CultureInfo, string> nameSelector, IReadOnlyList<TItem> items, ICultureState cultureState)
    {
        Key = key;
        _nameSelector = nameSelector;
        Items = items;
        Count = items.Count;
        _cultureState = cultureState;
    }

    /// <summary>The key the group's items share.</summary>
    public object? Key { get; }

    /// <summary>The group's items on screen, in the order shown, where empty means a virtualizing view fetches them as it scrolls.</summary>
    public IReadOnlyList<TItem> Items { get; }

    /// <summary>How many items the group holds.</summary>
    public int Count { get; init; }

    /// <summary>The name the group's heading shows, in the culture state's cultures.</summary>
    /// <remarks>
    /// Each read runs the grouping's name callback for the group's key, synchronously and with the state's cultures as the ambient
    /// ones, so a heading that reads it as it renders runs the callback once on every render, in the cultures the state holds then.
    /// </remarks>
    public string DisplayName
        => _cultureState.Run(() => _nameSelector(Key, _cultureState.Culture));
}
