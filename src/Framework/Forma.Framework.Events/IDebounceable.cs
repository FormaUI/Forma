namespace Forma.Framework.Events;

/// <summary>Runs an action once a period of inactivity has passed, a later request superseding one still waiting or running.</summary>
public interface IDebounceable
{
    /// <summary>Runs the action once the delay passes with no further request, handing it a token that a later request, <see cref="Cancel"/> or the given token cancels.</summary>
    ValueTask DebounceAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>Abandons the request still waiting or running.</summary>
    void Cancel();
}
