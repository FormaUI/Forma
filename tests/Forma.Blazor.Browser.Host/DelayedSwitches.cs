namespace Forma.Blazor.Browser.Host;

/// <summary>How many switches made after a delay have landed, which a check reads over HTTP while the browser is offline.</summary>
public sealed class DelayedSwitches
{
    private int _count;

    /// <summary>The switches landed so far.</summary>
    public int Count
        => Volatile.Read(ref _count);

    /// <summary>Records a switch that landed.</summary>
    public void Record()
        => Interlocked.Increment(ref _count);
}
