namespace Forma.Framework.Catalog.Queries;


/// <summary>The items fetched from a catalog provider.</summary>
public sealed class CatalogProviderItems : IDisposable
{
    private List<ICatalogItem> _items = [];
    private int _fetchedCount;
    private int _unlistedCount;
    private CancellationTokenSource? _fetchCancellation;

    /// <summary>The items the last fetch returned, replaced or extended as asked.</summary>
    public IReadOnlyList<ICatalogItem> Items
        => _items;

    /// <summary>How many items the provider has returned.</summary>
    public int FetchedCount
        => _fetchedCount;

    /// <summary>How many items the provider reports in total.</summary>
    public int? Total { get; private set; }

    /// <summary>The reported total less the items left out.</summary>
    public int? ListedTotal
        => Total is { } total ? Math.Max(total - _unlistedCount, _items.Count) : null;

    /// <summary>Whether the last page came back full.</summary>
    public bool WasFull { get; private set; }

    /// <summary>Whether a fetch is currently running.</summary>
    public bool IsFetching { get; private set; }

    /// <summary>Runs the query against the provider.</summary>
    public async Task FetchAsync(
        ICatalogProvider provider,
        Action onStateChanged,
        CatalogQuery query,
        CatalogResourceFilter? filter,
        bool append,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(onStateChanged);

        // A newer slice makes an older one irrelevant rather than merely late, so the earlier request is abandoned.
        await CancelAsync().ConfigureAwait(false);
        var own = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _fetchCancellation = own;
        var token = own.Token;

        IsFetching = true;
        onStateChanged();

        try
        {
            var page = await provider.GetItemsAsync(query, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                return;
            }

            var listed = page.Items.Where(item => item.Visibility != CatalogItemVisibility.Hidden);

            // A provider is expected to apply the same filter itself; this only keeps a stray item off the screen.
            if (filter is { } accepted)
            {
                listed = listed.Where(item => item.Resource is not { } resource || accepted.Accepts(resource));
            }

            List<ICatalogItem> kept = [.. listed];
            var unlisted = page.Items.Count - kept.Count;

            if (append)
            {
                _items = [.. _items, .. kept];
                _fetchedCount += page.Items.Count;
                _unlistedCount += unlisted;
            }
            else
            {
                _items = kept;
                _fetchedCount = page.Items.Count;
                _unlistedCount = unlisted;
            }

            Total = page.TotalCount;
            WasFull = page.Items.Count >= query.Take;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // A superseded fetch leaves the flag to the one that replaced it; anything else, including a failure, clears it.
            if (ReferenceEquals(_fetchCancellation, own))
            {
                IsFetching = false;
                onStateChanged();
            }
        }
    }

    private async Task CancelAsync()
    {
        if (_fetchCancellation is null)
        {
            return;
        }

        await _fetchCancellation.CancelAsync().ConfigureAwait(false);
        _fetchCancellation.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
        => _fetchCancellation?.Dispose();
}
