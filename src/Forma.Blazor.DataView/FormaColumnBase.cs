namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>A column of a grid: what its header says, how its cells align and sort, and what they draw for an item.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public abstract class FormaColumnBase<TItem> : ComponentBase
    where TItem : notnull
{
    [CascadingParameter] private FormaDataGrid<TItem> Grid { get; set; } = default!;

    /// <summary>The title shown in the header cell and read as the column's name.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The name the column's sort goes by in the address and to a sort menu, the same in every culture; a property column takes its property's path when none is given, and any other sortable column is given one.</summary>
    [Parameter] public string? SortName { get; set; }

    /// <summary>The class the column's header and cells carry.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Where the column's content sits in its cells.</summary>
    [Parameter] public ColumnAlign Align { get; set; }

    /// <summary>How much the column matters when the width runs out: a lower number leaves first, and the highest priority among the columns never leaves.</summary>
    [Parameter] public int Priority { get; set; }

    /// <summary>Whether the column stays at the start of the pane while the others scroll sideways beneath it; frozen columns are written first.</summary>
    [Parameter] public bool Frozen { get; set; }

    /// <summary>Whether the column is drawn at all; off, it is out of the grid and its columns menu alike.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>The width the column takes, where its content does not size it well.</summary>
    [Parameter] public CssLength? Width { get; set; }

    /// <summary>Whether the grid sorts by this column when its header is pressed; absent, the column decides for itself from what it shows.</summary>
    [Parameter] public bool? Sortable { get; set; }

    /// <summary>The sort pressing the header applies; a derived column may supply one of its own.</summary>
    [Parameter] public virtual DataViewSort<TItem>? SortBy { get; set; }

    /// <summary>The direction the first press of the header sorts in.</summary>
    [Parameter] public DataViewSortDirection InitialSortDirection { get; set; }

    /// <summary>Whether the grid opens sorted by this column.</summary>
    [Parameter] public bool IsDefaultSortColumn { get; set; }

    /// <summary>Content rendered in the header cell in place of the title, given the column.</summary>
    [Parameter] public RenderFragment<FormaColumnBase<TItem>>? HeaderTemplate { get; set; }

    /// <summary>An options surface the header opens from a button beside the title.</summary>
    [Parameter] public RenderFragment? ColumnOptions { get; set; }

    /// <summary>The placeholder a cell of this column draws.</summary>
    [Parameter] public RenderFragment<PlaceholderContext>? PlaceholderTemplate { get; set; }

    /// <summary>The value the grid names the column by in its markup and its measurements, for its own lifetime.</summary>
    internal string ColumnKey { get; } = $"column-{Guid.NewGuid():N}";

    /// <summary>Whether the column can be sorted by, from <see cref="Sortable"/> or the column's own default.</summary>
    public bool IsSortable
        => Sortable ?? IsSortableByDefault();

    /// <summary>The content of the header cell: the template where the caller gave one, else the title.</summary>
    protected internal virtual RenderFragment HeaderContent
        => builder =>
        {
            if (HeaderTemplate is { } template)
            {
                builder.AddContent(0, template(this));
            }
            else
            {
                builder.AddContent(1, Title);
            }
        };

    /// <summary>Renders the cell for <paramref name="item"/>.</summary>
    /// <param name="builder">The builder the cell is rendered into.</param>
    /// <param name="item">The item the row shows.</param>
    protected internal abstract void CellContent(RenderTreeBuilder builder, TItem item);

    /// <summary>Whether the column sorts unless the caller says otherwise.</summary>
    protected virtual bool IsSortableByDefault()
        => SortBy is not null;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Grid is null)
        {
            throw new InvalidOperationException($"{GetType().Name} must be declared inside a {nameof(FormaDataGrid<TItem>)}.");
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // The address carries a sort by its name, so a column that sorts has one, and a label that changes with the culture is never it.
        if (IsSortable && string.IsNullOrWhiteSpace(SortName))
        {
            throw new InvalidOperationException($"{GetType().Name} '{Title}' sorts, so it needs a {nameof(SortName)}: the name its sort goes by in the address, the same in every culture.");
        }

        Grid.AddColumn(this);
    }
}
