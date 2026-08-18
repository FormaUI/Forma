namespace Forma.UI.Features.Indicators;

using System.Collections;
using System.Collections.Concurrent;

/// <summary>
/// In-memory <see cref="IStatusBadgeCatalog"/> holding the statuses an application defines.
/// Supports collection expressions:
/// <c>StatusBadgeCatalog c = [new("draft", CssColor.FromHex(0xeeeeee), CssColor.FromHex(0x333333))];</c>
/// </summary>
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

    /// <summary>Every registered status badge.</summary>
    public IReadOnlyCollection<StatusBadgeStyle> Statuses
        => [.. _statuses.Values];

    /// <summary>Registers a status badge, replacing any entry with the same name.</summary>
    public void Add(StatusBadgeStyle status)
    {
        ArgumentNullException.ThrowIfNull(status);
        _statuses[status.Name] = status;
    }

    /// <summary>Returns the badge registered under a name, or null when the name is unknown.</summary>
    public StatusBadgeStyle? Find(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _statuses.TryGetValue(name, out var style) ? style : null;
    }

    /// <summary>Returns an enumerator over every registered status badge.</summary>
    public IEnumerator<StatusBadgeStyle> GetEnumerator()
        => _statuses.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
