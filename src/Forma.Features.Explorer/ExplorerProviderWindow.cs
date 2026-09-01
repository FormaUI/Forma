namespace Forma.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The window of resources fetched from a provider one page at a time, with the fetch itself in flight.</summary>
public sealed class ExplorerProviderWindow : IDisposable
{
    private List<ResourceInfo> _resources = [];
    private CancellationTokenSource? _fetchCancellation;

    /// <summary>The resources the last fetch returned, replaced or extended as asked.</summary>
    public IReadOnlyList<ResourceInfo> Resources
        => _resources;

    /// <summary>How many resources the provider reports in total, or null when it cannot say without counting them.</summary>
    public int? Total { get; private set; }

    /// <summary>Whether the last page came back full — the only signal left for more pages once <see cref="Total"/> is unknown.</summary>
    public bool WasFull { get; private set; }

    /// <summary>Whether a fetch is currently running.</summary>
    public bool IsFetching { get; private set; }

    /// <summary>
    /// Runs the query against the provider, replacing the window or extending it, filtering out a stray resource
    /// the provider let through. A fetch still running when a newer one starts is abandoned rather than raced.
    /// </summary>
    public async Task FetchAsync(
        IExplorerResourcesProvider provider,
        Action onStateChanged,
        ExplorerResourceQuery query,
        ExplorerResourceFilter? filter,
        bool appendToWindow)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(onStateChanged);

        // A newer window makes an older one irrelevant rather than merely late, so the earlier request is abandoned.
        await CancelAsync().ConfigureAwait(false);
        _fetchCancellation = new CancellationTokenSource();
        var token = _fetchCancellation.Token;

        IsFetching = true;
        onStateChanged();

        try
        {
            var page = await provider.GetResourcesAsync(query, token).ConfigureAwait(false);

            if (token.IsCancellationRequested)
            {
                return;
            }

            // A provider is expected to apply the same filter itself; this only keeps a stray resource off the screen.
            var window = filter is { } accepted ? page.Resources.Where(accepted.Accepts) : page.Resources;

            _resources = appendToWindow ? [.. _resources, .. window] : [.. window];
            Total = page.TotalCount;
            WasFull = page.Resources.Count >= query.Take;
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            // A superseded fetch leaves the flag to the one that replaced it; anything else, including a failure, clears it.
            if (!token.IsCancellationRequested)
            {
                IsFetching = false;
            }
        }

        onStateChanged();
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
