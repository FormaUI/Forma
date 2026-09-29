namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Events;

/// <summary>What a sort control reads and drives of a data view: the sort it holds, the sort the address asked for, and sorting by another, announced through <see cref="INotifyStateChanged"/> as it changes.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public interface IDataViewSorter<TItem> : INotifyStateChanged
    where TItem : notnull
{
    /// <summary>The name of the sort applied, or null while the items stand unsorted.</summary>
    string? SortName { get; }

    /// <summary>The direction of the sort applied.</summary>
    DataViewSortDirection SortDirection { get; }

    /// <summary>The name of the sort the address asked for and nothing has applied yet, or null.</summary>
    string? RequestedSortName { get; }

    /// <summary>The direction the address asked for beside <see cref="RequestedSortName"/>.</summary>
    DataViewSortDirection RequestedSortDirection { get; }

    /// <summary>Sorts the items by <paramref name="sort"/> in <paramref name="direction"/>, under <paramref name="name"/>, which the address carries.</summary>
    /// <param name="name">The name the sort goes by in the address.</param>
    /// <param name="sort">The keys the items are sorted by.</param>
    /// <param name="direction">The direction.</param>
    Task SortByAsync(string name, DataViewSort<TItem> sort, DataViewSortDirection direction);
}
