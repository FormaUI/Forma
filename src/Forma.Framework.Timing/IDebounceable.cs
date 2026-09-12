namespace Forma.Framework.Timing;

/// <summary>Runs an action once a period of inactivity has passed, letting a later request supersede one still waiting or still running.</summary>
public interface IDebounceable
{
    /// <summary>Runs the action once the delay passes with no further request, handing it a token that a later request, <see cref="Cancel"/>, or the given token signals. A request canceled before it runs completes without running.</summary>
    ValueTask DebounceAsync(Func<CancellationToken, Task> action, TimeSpan delay, CancellationToken cancellationToken = default);

    /// <summary>Abandons the request still waiting or running, if any.</summary>
    void Cancel();
}
