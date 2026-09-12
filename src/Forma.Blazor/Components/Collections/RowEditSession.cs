namespace Forma.Blazor.Components.Collections;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>The row a grid is currently editing, together with the detached copy its editors write to.</summary>
internal sealed class RowEditSession<TGridItem>
    where TGridItem : notnull
{
    /// <summary>Resolves a row's identity, so the editing row is recognized across reloads.</summary>
    public Func<TGridItem, object> KeySelector { get; set; } = item => item;

    /// <summary>The untouched row the edit started from.</summary>
    public TGridItem? Item { get; private set; }

    /// <summary>The copy the editors bind to, discarded wholesale when the edit is canceled.</summary>
    public TGridItem? Draft { get; private set; }

    /// <summary>Validation state for <see cref="Draft"/>.</summary>
    public EditContext? EditContext { get; private set; }

    public bool IsActive
        => Draft is not null;

    public bool IsEditing(TGridItem candidate)
        => Item is not null && KeySelector(Item).Equals(KeySelector(candidate));

    public void BeginEditing(TGridItem item, TGridItem draft)
    {
        Item = item;
        Draft = draft;
        EditContext = new EditContext(draft);
    }

    public void EndEditing()
    {
        Item = default;
        Draft = default;
        EditContext = null;
    }
}
