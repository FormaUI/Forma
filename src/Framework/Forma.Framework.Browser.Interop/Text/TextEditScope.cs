namespace Forma.Framework.Browser.Interop.Text;

/// <summary>The text fields an action reads together, as a form's are, which it settles before it reads them.</summary>
public sealed class TextEditScope
{
    private readonly HashSet<DomTextEdits> _edits = [];

    /// <summary>Takes <paramref name="edits"/> into the scope.</summary>
    public void Add(DomTextEdits edits)
    {
        ArgumentNullException.ThrowIfNull(edits);

        _edits.Add(edits);
    }

    /// <summary>Takes <paramref name="edits"/> out of the scope.</summary>
    public void Remove(DomTextEdits edits)
        => _edits.Remove(edits);

    /// <summary>Waits until each field's text holds every edit the field sent before the call.</summary>
    /// <returns>Whether every field's text holds them, which a field whose text can't follow its edits keeps from being so.</returns>
    public async Task<bool> SettleAsync()
    {
        var results = await Task.WhenAll(_edits.Where(edits => !edits.IsDisposed).ToList().Select(edits => edits.SettleAsync()));

        return results.All(isSettled => isSettled);
    }
}
