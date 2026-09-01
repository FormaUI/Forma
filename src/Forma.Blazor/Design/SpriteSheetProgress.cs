namespace Forma.Blazor.Design;

/// <summary>A ledger of sprite sheets on their way in, where arriving and failing both settle a sheet.</summary>
internal sealed class SpriteSheetProgress
{
    private readonly HashSet<string> _loaded = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loading = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failed = new(StringComparer.Ordinal);

    /// <summary>Whether the sheet has arrived or failed, either way no longer worth waiting for.</summary>
    public bool IsSettled(string sheet)
        => _loaded.Contains(sheet) || _failed.Contains(sheet);

    /// <summary>Claims a sheet for loading, refusing one already loading or settled.</summary>
    public bool Begin(string sheet)
        => !IsSettled(sheet) && _loading.Add(sheet);

    /// <summary>Settles a claimed sheet as arrived or failed.</summary>
    public void Settle(string sheet, bool arrived)
    {
        _loading.Remove(sheet);
        (arrived ? _loaded : _failed).Add(sheet);
    }

    /// <summary>Forgets every failure, so each failed sheet may be asked for again.</summary>
    public void ForgetFailures()
        => _failed.Clear();
}
