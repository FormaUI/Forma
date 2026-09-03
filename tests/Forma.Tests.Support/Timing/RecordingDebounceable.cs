namespace Forma.Tests.Timing;

using Forma.Framework.Timing;

/// <summary>A debouncer that runs every action at once and records what it was asked, for tests about how a component debounces.</summary>
public sealed class RecordingDebounceable : IDebounceable
{
    /// <summary>The delay of every request, in order.</summary>
    public List<TimeSpan> Delays { get; } = [];

    /// <summary>How many times a pending request was abandoned.</summary>
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
