namespace Forma.Framework.Catalog.Queries;


/// <summary>The items fetched from a catalog provider.</summary>
public sealed class CatalogProviderItems : IDisposable
{
    private List<ICatalogItem> _items = [];
    private int _readCount;
    private bool _hasLeftItemsOut;
    private int? _listedCount;
    private CancellationTokenSource? _fetchCancellation;

    /// <summary>The items fetched from the provider.</summary>
    public IReadOnlyList<ICatalogItem> Items
        => _items;

    /// <summary>How many store positions the fetched slices read.</summary>
    public int ReadCount
        => _readCount;

    /// <summary>How many items the provider reports the store matched, each a position a slice can read.</summary>
    public int? Total { get; private set; }

    /// <summary>How many items of the whole query are listed, kept while the provider guarantees it and no slice held an item left out.</summary>
    public int? ListedCount
        => _hasLeftItemsOut ? null : _listedCount;

    /// <summary>Whether the store holds positions past the slices read.</summary>
    public bool HasMore { get; private set; }

    /// <summary>Whether a fetch is running.</summary>
    public bool IsFetching { get; private set; }

    /// <summary>Fetches the items the query asks for from the provider.</summary>
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

            if (filter is { } accepted)
            {
                listed = listed.Where(item => item.Resource is not { } resource || accepted.Accepts(resource));
            }

            List<ICatalogItem> kept = [.. listed];
            var read = page.ReadCount ?? page.Items.Count;
            var leftOut = kept.Count < page.Items.Count;

            if (append)
            {
                _items = [.. _items, .. kept];
                _readCount += read;
                _hasLeftItemsOut |= leftOut;
            }
            else
            {
                _items = kept;
                _readCount = query.Skip + read;
                _hasLeftItemsOut = leftOut;
            }

            Total = page.TotalCount;
            _listedCount = page.ListedCount;

            // A slice that read nothing ends the run, so a provider that makes no progress is never asked again.
            HasMore = read > 0 && (page.HasMore ?? (page.TotalCount is { } total ? _readCount < total : read >= query.Take));
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // Only the latest fetch clears the flag.
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
