namespace Forma.Blazor.Components.DataView;

/// <summary>Counts and reads a queryable asynchronously, so a query backed by a store runs there rather than on the renderer.</summary>
public interface IDataViewQueryExecutor
{
    /// <summary>Whether this executor handles <paramref name="query"/>; one that does not leaves it to the next.</summary>
    /// <typeparam name="TItem">The item the query yields.</typeparam>
    /// <param name="query">The query to judge.</param>
    bool Supports<TItem>(IQueryable<TItem> query);

    /// <summary>How many items <paramref name="query"/> yields.</summary>
    /// <typeparam name="TItem">The item the query yields.</typeparam>
    /// <param name="query">The query to count.</param>
    /// <param name="cancellationToken">Canceled when the count is no longer wanted.</param>
    ValueTask<int> CountAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken);

    /// <summary>Every item <paramref name="query"/> yields.</summary>
    /// <typeparam name="TItem">The item the query yields.</typeparam>
    /// <param name="query">The query to read.</param>
    /// <param name="cancellationToken">Canceled when the items are no longer wanted.</param>
    ValueTask<TItem[]> ToArrayAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken);
}
