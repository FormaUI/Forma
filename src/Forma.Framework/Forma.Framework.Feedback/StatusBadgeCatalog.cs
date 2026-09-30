namespace Forma.Framework.Feedback;

using System.Collections;
using System.Collections.Concurrent;

/// <summary>The in-memory <see cref="IStatusBadgeCatalog"/> holding the statuses an application defines.</summary>
public sealed class StatusBadgeCatalog : IStatusBadgeCatalog
{
    private readonly ConcurrentDictionary<string, StatusBadgeStyle> _statuses =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates an empty catalog.</summary>
    public StatusBadgeCatalog()
    {
    }

    /// <summary>Creates a catalog holding the given statuses.</summary>
    public StatusBadgeCatalog(params ReadOnlySpan<StatusBadgeStyle> statuses)
    {
        foreach (var status in statuses)
        {
            Add(status);
        }
    }

    /// <inheritdoc />
    public IReadOnlyCollection<StatusBadgeStyle> Statuses
        => [.. _statuses.Values];

    /// <inheritdoc />
    public void Add(StatusBadgeStyle status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _statuses[status.Name] = status;
    }

    /// <inheritdoc />
    public StatusBadgeStyle? Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _statuses.TryGetValue(name, out var style) ? style : null;
    }

    /// <inheritdoc />
    public IEnumerator<StatusBadgeStyle> GetEnumerator()
        => _statuses.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
