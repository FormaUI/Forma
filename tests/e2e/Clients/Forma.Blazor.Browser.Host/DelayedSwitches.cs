namespace Forma.Blazor.Browser.Host;

/// <summary>The switches the culture panel holds until a check releases them, and how many have started and settled, which a check reads over HTTP while the browser is offline.</summary>
public sealed class DelayedSwitches
{
    private readonly Lock _gate = new();
    private TaskCompletionSource _release = CreateSignal();
    private TaskCompletionSource _change = CreateSignal();
    private int _startCount;
    private int _count;
    private int _failures;

    /// <summary>The switches settled so far.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _count;
            }
        }
    }

    /// <summary>The settled switches that failed.</summary>
    public int Failures
    {
        get
        {
            lock (_gate)
            {
                return _failures;
            }
        }
    }

    /// <summary>The release the switches held now wait for.</summary>
    /// <returns>A task that completes once a check releases them.</returns>
    public Task WaitForReleaseAsync()
    {
        lock (_gate)
        {
            return _release.Task;
        }
    }

    /// <summary>Releases the switches held.</summary>
    /// <param name="cancellationToken">The token that ends the wait for one to start.</param>
    /// <returns>A task that completes once one of them has started.</returns>
    public async Task ReleaseAsync(CancellationToken cancellationToken)
    {
        TaskCompletionSource release;
        int startCount;

        lock (_gate)
        {
            release = _release;
            startCount = _startCount;
            _release = CreateSignal();
        }

        release.SetResult();
        await WaitForAsync(() => _startCount, startCount, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Waits until more switches than <paramref name="count"/> have settled.</summary>
    /// <param name="count">The settled switches already counted.</param>
    /// <param name="cancellationToken">The token that ends the wait.</param>
    /// <returns>The switches settled.</returns>
    public Task<int> WaitForSettledAsync(int count, CancellationToken cancellationToken)
        => WaitForAsync(() => _count, count, cancellationToken);

    /// <summary>Records a switch that started.</summary>
    public void RecordStart()
        => Change(() => _startCount++);

    /// <summary>Records a switch that settled, and whether it failed.</summary>
    /// <param name="isFailed">Whether the switch failed.</param>
    public void Record(bool isFailed)
        => Change(() =>
        {
            if (isFailed)
            {
                _failures++;
            }

            _count++;
        });

    private static TaskCompletionSource CreateSignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    // A count read under the gate, awaited past each change until it exceeds the one given.
    private async Task<int> WaitForAsync(Func<int> readCount, int count, CancellationToken cancellationToken)
    {
        while (true)
        {
            Task change;

            lock (_gate)
            {
                var current = readCount();

                if (current > count)
                {
                    return current;
                }

                change = _change.Task;
            }

            await change.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void Change(Action change)
    {
        TaskCompletionSource changed;

        lock (_gate)
        {
            change();
            changed = _change;
            _change = CreateSignal();
        }

        changed.SetResult();
    }
}
