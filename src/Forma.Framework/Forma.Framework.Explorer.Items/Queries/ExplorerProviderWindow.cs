namespace Forma.Framework.Explorer.Items.Queries;


/// <summary>The window of items fetched from a provider one page at a time, with the fetch itself in flight.</summary>
public sealed class ExplorerProviderWindow : IDisposable
{
    private List<IExplorerItem> _items = [];
    private int _fetchedCount;
    private int _unlistedCount;
    private CancellationTokenSource? _fetchCancellation;

    /// <summary>The items the last fetch returned, replaced or extended as asked.</summary>
    public IReadOnlyList<IExplorerItem> Items
        => _items;

    /// <summary>How many items the provider has returned into the window, the ones left out of it included.</summary>
    public int FetchedCount
        => _fetchedCount;

    /// <summary>How many items the provider reports in total, or null when it cannot say without counting them.</summary>
    public int? Total { get; private set; }

    /// <summary>The reported total less the items the provider returned that the window left out, or null when the total is unknown.</summary>
    public int? ListedTotal
        => Total is { } total ? Math.Max(total - _unlistedCount, _items.Count) : null;

    /// <summary>Whether the last page came back full — the only signal left for more pages once <see cref="Total"/> is unknown.</summary>
    public bool WasFull { get; private set; }

    /// <summary>Whether a fetch is currently running.</summary>
    public bool IsFetching { get; private set; }

    /// <summary>
    /// Runs the query against the provider, replacing the window or extending it, leaving out hidden items and
    /// a stray item the provider let through. A fetch still running when a newer one starts, or when the given token
    /// is canceled, is abandoned rather than raced.
    /// </summary>
    public async Task FetchAsync(
        IExplorerItemsProvider provider,
        Action onStateChanged,
        ExplorerItemQuery query,
        ExplorerResourceFilter? filter,
        bool appendToWindow,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(onStateChanged);

        // A newer window makes an older one irrelevant rather than merely late, so the earlier request is abandoned.
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

            var listed = page.Items.Where(item => item.Visibility != ExplorerItemVisibility.Hidden);

            // A provider is expected to apply the same filter itself; this only keeps a stray item off the screen.
            if (filter is { } accepted)
            {
                listed = listed.Where(item => item.Resource is not { } resource || accepted.Accepts(resource));
            }

            List<IExplorerItem> window = [.. listed];
            var unlisted = page.Items.Count - window.Count;

            if (appendToWindow)
            {
                _items = [.. _items, .. window];
                _fetchedCount += page.Items.Count;
                _unlistedCount += unlisted;
            }
            else
            {
                _items = window;
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
