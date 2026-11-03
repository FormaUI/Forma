namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components.Forms;

/// <summary>The row a grid is editing, together with the detached copy its editors write to.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
internal sealed class RowEditSession<TItem>
    where TItem : notnull
{
    /// <summary>Resolves a row's identity.</summary>
    public Func<TItem, object> KeySelector { get; set; } = item => item;

    /// <summary>The untouched row the edit started from.</summary>
    public TItem? Item { get; private set; }

    /// <summary>The copy the editors bind to.</summary>
    public TItem? Draft { get; private set; }

    /// <summary>Validation state for <see cref="Draft"/>.</summary>
    public EditContext? EditContext { get; private set; }

    /// <summary>Whether <paramref name="candidate"/> is the row being edited.</summary>
    /// <param name="candidate">The row to judge.</param>
    public bool IsEditing(TItem candidate)
        => Item is not null && KeySelector(Item).Equals(KeySelector(candidate));

    /// <summary>Starts editing <paramref name="item"/> through <paramref name="draft"/>.</summary>
    /// <param name="item">The row being edited.</param>
    /// <param name="draft">The copy the editors write to.</param>
    public void BeginEditing(TItem item, TItem draft)
    {
        Item = item;
        Draft = draft;
        EditContext = new EditContext(draft);
    }

    /// <summary>Ends the edit, dropping the draft and its validation state.</summary>
    public void EndEditing()
    {
        Item = default;
        Draft = default;
        EditContext = null;
    }
}
