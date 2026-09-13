namespace Forma.Blazor.Features.Emojis;

/// <summary>Tracks the lifecycle of sprite sheets being loaded, where arriving and failing both complete a sheet.</summary>
public sealed class SpriteSheetLoadTracker
{
    private readonly HashSet<string> _loadedSheets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _loadingSheets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _failedSheets = new(StringComparer.Ordinal);

    /// <summary>Whether the sheet has arrived or failed, either way no longer worth waiting for.</summary>
    public bool IsCompleted(string sheet)
        => _loadedSheets.Contains(sheet) || _failedSheets.Contains(sheet);

    /// <summary>Claims a sheet for loading, refusing one already loading or completed.</summary>
    public bool TryStartLoading(string sheet)
        => !IsCompleted(sheet) && _loadingSheets.Add(sheet);

    /// <summary>Completes a claimed sheet as arrived.</summary>
    public void MarkLoaded(string sheet)
    {
        _loadingSheets.Remove(sheet);
        _loadedSheets.Add(sheet);
    }

    /// <summary>Completes a claimed sheet as failed.</summary>
    public void MarkFailed(string sheet)
    {
        _loadingSheets.Remove(sheet);
        _failedSheets.Add(sheet);
    }

    /// <summary>Forgets every failure, so each failed sheet may be asked for again.</summary>
    public void ClearFailures()
        => _failedSheets.Clear();
}
