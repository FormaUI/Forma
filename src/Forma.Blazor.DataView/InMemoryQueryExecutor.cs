namespace Forma.Blazor.Components.DataView;

/// <summary>Counts and reads any queryable by enumerating it where it stands, which is what a queryable over a collection in memory is.</summary>
public sealed class InMemoryQueryExecutor : IDataViewQueryExecutor
{
    /// <summary>The one instance, since the executor holds nothing.</summary>
    public static InMemoryQueryExecutor Instance { get; } = new();

    /// <inheritdoc />
    public bool Supports<TItem>(IQueryable<TItem> query)
        => true;

    /// <inheritdoc />
    public ValueTask<int> CountAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ValueTask.FromResult(query.Count());
    }

    /// <inheritdoc />
    public ValueTask<TItem[]> ToArrayAsync<TItem>(IQueryable<TItem> query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return ValueTask.FromResult(query.ToArray());
    }
}
