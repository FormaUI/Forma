namespace Forma.Blazor.Components.DataView;

/// <summary>The query executor that counts and reads any queryable by enumerating it in memory.</summary>
public sealed class InMemoryQueryExecutor : IDataViewQueryExecutor
{
    /// <summary>The shared instance.</summary>
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
