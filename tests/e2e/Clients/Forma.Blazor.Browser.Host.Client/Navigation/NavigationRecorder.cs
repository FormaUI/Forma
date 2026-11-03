namespace Forma.Blazor.Browser.Host.Client.Navigation;

/// <summary>What one runtime observed of its navigations, in order, for the navigation boundary's check to read.</summary>
/// <remarks>A navigation's identity is the number the runtime gives it as its router starts navigating; the document's is a value drawn once per runtime, so a navigation that replaces the document shows as a new one.</remarks>
public sealed class NavigationRecorder
{
    private readonly Lock _gate = new();
    private readonly List<string> _entries = [];
    private int _sequence;

    /// <summary>The runtime's own identity, drawn as it starts.</summary>
    public string RuntimeId { get; } = Guid.NewGuid().ToString("N")[..8];

    /// <summary>Raised after each entry is recorded.</summary>
    public event Action? Recorded;

    /// <summary>The entries so far.</summary>
    public IReadOnlyList<string> Entries
    {
        get
        {
            lock (_gate)
            {
                return [.. _entries];
            }
        }
    }

    /// <summary>Records that the router started navigating to <paramref name="path"/>, giving the navigation its identity.</summary>
    /// <param name="path">The path the router navigates to.</param>
    /// <returns>The navigation's identity.</returns>
    public int Start(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        int sequence;

        lock (_gate)
        {
            sequence = ++_sequence;
        }

        Record($"start {sequence} /{path.TrimStart('/')}");

        return sequence;
    }

    /// <summary>Records an observation.</summary>
    /// <param name="entry">What was observed.</param>
    public void Record(string entry)
    {
        lock (_gate)
        {
            _entries.Add(entry);
        }

        Recorded?.Invoke();
    }
}
