namespace Forma.Blazor.BUnit.Tests.Support;

using Forma.Framework.Events;

/// <summary>A debouncer that runs every action at once and records what it was asked.</summary>
public sealed class RecordingDebounceable : IDebounceable
{
    /// <summary>The delay of every request, in order.</summary>
    public List<TimeSpan> Delays { get; } = [];

    /// <summary>How many times the pending request was canceled.</summary>
    public int Cancellations { get; private set; }

    /// <inheritdoc />
    public async ValueTask DebounceAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);

        Delays.Add(delay);

        await action(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Cancel()
        => Cancellations++;
}
