namespace Forma.Blazor.Browser.Tests;

/// <summary>The _framework requests a page makes, each tracked as itself, two for one address apart.</summary>
/// <remarks>A request is pending until it finishes with a successful response or fails, and a failure is kept apart. Once none is pending and the ICU data downloaded, a request that starts is recorded as late rather than reopening the wait, until tracking stops.</remarks>
public sealed class FrameworkDownloads
{
    private readonly IPage _page;
    private readonly Lock _gate = new();
    private readonly HashSet<IRequest> _pending = new(ReferenceEqualityComparer.Instance);
    private readonly List<string> _downloaded = [];
    private readonly List<string> _failed = [];
    private readonly List<string> _late = [];
    private readonly SemaphoreSlim _changed = new(0);
    private bool _isReady;
    private bool _isStopped;

    /// <summary>Starts tracking the page's _framework requests.</summary>
    /// <param name="page">The page whose requests are tracked.</param>
    public FrameworkDownloads(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        _page = page;
        page.Request += (_, request) => Start(request);
        page.RequestFinished += async (_, request) =>
        {
            if (IsFramework(request))
            {
                var response = await request.ResponseAsync().ConfigureAwait(false);
                Settle(request, response is { Ok: true } ? null : $"status {response?.Status}");
            }
        };
        page.RequestFailed += (_, request) =>
        {
            if (IsFramework(request))
            {
                Settle(request, request.Failure ?? "failed");
            }
        };
    }

    /// <summary>The requests that failed, each with why.</summary>
    public IReadOnlyList<string> Failed
    {
        get
        {
            lock (_gate)
            {
                return [.. _failed];
            }
        }
    }

    /// <summary>The requests that started once the page was ready.</summary>
    public IReadOnlyList<string> Late
    {
        get
        {
            lock (_gate)
            {
                return [.. _late];
            }
        }
    }

    /// <summary>Waits until no request is pending and the ICU data downloaded, or a request failed.</summary>
    /// <returns>A task that completes once the page is ready.</returns>
    public async Task WaitAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        while (true)
        {
            lock (_gate)
            {
                if (_pending.Count == 0 && (_failed.Count > 0 || _downloaded.Exists(name => name.StartsWith("icudt", StringComparison.Ordinal))))
                {
                    _isReady = true;

                    return;
                }
            }

            await _changed.WaitAsync(timeout.Token).ConfigureAwait(false);
        }
    }

    /// <summary>Waits until the page has downloaded the framework, as <see cref="WaitAsync"/> does, and given the page a frame and a task to record that it did, which Auto reads on the next visit, then stops tracking.</summary>
    /// <returns>A task that completes once tracking stopped.</returns>
    /// <remarks>A request that failed fails the check there, naming it, since Auto stays on the Server without what it failed to download.</remarks>
    public async Task StopOnceDownloadedAsync()
    {
        await WaitAsync().ConfigureAwait(false);

        var failed = Failed;
        Assert.True(failed.Count == 0, $"The page's framework didn't download: {string.Join(", ", failed)}.");

        await _page.EvaluateAsync("() => new Promise(resolve => requestAnimationFrame(() => setTimeout(resolve)))").ConfigureAwait(false);
        Stop();
    }

    /// <summary>Stops tracking, so what the page requests next, a reload's requests among it, is no late start.</summary>
    public void Stop()
    {
        lock (_gate)
        {
            _isStopped = true;
        }
    }

    private void Start(IRequest request)
    {
        if (!IsFramework(request))
        {
            return;
        }

        lock (_gate)
        {
            if (_isStopped)
            {
                return;
            }

            if (_isReady)
            {
                _late.Add(GetName(request));
            }
            else
            {
                _pending.Add(request);
            }
        }

        _changed.Release();
    }

    private void Settle(IRequest request, string? failure)
    {
        lock (_gate)
        {
            if (!_pending.Remove(request))
            {
                return;
            }

            if (failure is null)
            {
                _downloaded.Add(GetName(request));
            }
            else
            {
                _failed.Add($"{GetName(request)} ({failure})");
            }
        }

        _changed.Release();
    }

    private static bool IsFramework(IRequest request)
        => request.Url.Contains("/_framework/", StringComparison.Ordinal);

    private static string GetName(IRequest request)
        => request.Url[(request.Url.LastIndexOf('/', StringComparison.Ordinal) + 1)..];
}
