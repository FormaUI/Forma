namespace Forma.Blazor.Browser.Host;

/// <summary>How many switches made after a delay have settled, and how many of them failed, which a check reads over HTTP while the browser is offline.</summary>
public sealed class DelayedSwitches
{
    private int _count;
    private int _failures;

    /// <summary>The switches settled so far.</summary>
    public int Count
        => Volatile.Read(ref _count);

    /// <summary>The settled switches that failed.</summary>
    public int Failures
        => Volatile.Read(ref _failures);

    /// <summary>Records a switch that settled, and whether it failed.</summary>
    public void Record(bool isFailed)
    {
        if (isFailed)
        {
            Interlocked.Increment(ref _failures);
        }

        Interlocked.Increment(ref _count);
    }
}
