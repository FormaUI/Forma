namespace Forma.Blazor.Components.DataView;

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

    /// <summary>The class the column's header and cells carry.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Where the column's content sits in its cells.</summary>
    [Parameter] public ColumnAlign Align { get; set; }

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

    /// <summary>What a cell of this column draws while its row's window has not loaded, in a virtualizing grid.</summary>
    [Parameter] public RenderFragment<PlaceholderContext>? PlaceholderTemplate { get; set; }

    /// <summary>Whether the column can be sorted by, from <see cref="Sortable"/> or the column's own default.</summary>
    public bool IsSortable
        => Sortable ?? IsSortableByDefault();

    /// <summary>The name the column's sort is known and written to the address by.</summary>
    public string SortName
        => Title ?? string.Empty;

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
        => Grid.AddColumn(this);
}
