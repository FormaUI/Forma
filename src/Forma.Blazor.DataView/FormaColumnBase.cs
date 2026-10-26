namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;
using Forma.Framework.Globalization;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>A column of a grid: what its header says, how its cells align and sort, and what they draw for an item.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public abstract class FormaColumnBase<TItem> : ComponentBase
    where TItem : notnull
{
    // What a column is given that only its header or its sort reads, which no cell is drawn by.
    private static readonly string[] _headerParameters =
    [
        nameof(Title), nameof(SortName), nameof(Sortable), nameof(SortBy), nameof(InitialSortDirection), nameof(IsDefaultSortColumn),
        nameof(HeaderTemplate), nameof(ColumnOptions), nameof(PlaceholderTemplate),
    ];

    private ParameterRevision? _parameterRevision;

    [CascadingParameter] private FormaDataGrid<TItem> Grid { get; set; } = default!;

    /// <summary>The cultures the column writes its title and its cells in.</summary>
    [Inject] protected ICultureState CultureState { get; set; } = default!;

    /// <summary>The title shown in the header cell and read as the column's name.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>The name the column's sort goes by in the address and to a sort menu, the same in every culture.</summary>
    [Parameter] public string? SortName { get; set; }

    /// <summary>The class the column's header and cells carry.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Where the column's content sits in its cells.</summary>
    [Parameter] public ColumnAlign Align { get; set; }

    /// <summary>How much the column matters when the width runs out.</summary>
    /// <remarks>A column with a lower number leaves first, and the columns at the highest priority among them always stay.</remarks>
    [Parameter] public int Priority { get; set; }

    /// <summary>Whether the column stays at the start of the pane while the others scroll sideways beneath it.</summary>
    [Parameter] public bool Frozen { get; set; }

    /// <summary>Whether the column is drawn in the grid and listed in its columns menu.</summary>
    [Parameter] public bool Visible { get; set; } = true;

    /// <summary>The width the column takes.</summary>
    [Parameter] public CssLength? Width { get; set; }

    /// <summary>Whether the grid sorts by this column when its header is pressed, where null leaves it to the column's own default.</summary>
    [Parameter] public bool? Sortable { get; set; }

    /// <summary>The sort pressing the header applies.</summary>
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

    /// <summary>How many times what the column is given has changed in a way that can change what its cells draw.</summary>
    internal int Revision
        => _parameterRevision?.Value ?? 0;

    /// <summary>Whether the column can be sorted by, from <see cref="Sortable"/> or the column's own default.</summary>
    public bool IsSortable
        => Sortable ?? IsSortableByDefault();

    /// <summary>The title the column shows and is named by, from <see cref="Title"/> or the column's own default.</summary>
    public string? EffectiveTitle
        => Title ?? CultureState.Run(GetDefaultTitle);

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
                builder.AddContent(1, EffectiveTitle);
            }
        };

    /// <summary>Renders the cell for <paramref name="item"/>.</summary>
    /// <param name="builder">The builder the cell is rendered into.</param>
    /// <param name="item">The item the row shows.</param>
    protected internal abstract void CellContent(RenderTreeBuilder builder, TItem item);

    /// <summary>Whether the column sorts by default.</summary>
    protected virtual bool IsSortableByDefault()
        => SortBy is not null;

    /// <summary>The column's default title, in the culture in force.</summary>
    protected virtual string? GetDefaultTitle()
        => null;

    /// <summary>What stands for a parameter's value in judging whether the column's cells are drawn differently.</summary>
    /// <param name="parameter">The parameter the column is given.</param>
    /// <returns>The value itself, unless it differs each time it is given and means the same.</returns>
    protected virtual object? ProjectParameter(ParameterValue parameter)
        => parameter.Value;

    /// <inheritdoc />
    public override Task SetParametersAsync(ParameterView parameters)
    {
        _parameterRevision ??= new ParameterRevision(_headerParameters, ProjectParameter);
        _parameterRevision.Observe(parameters);

        return base.SetParametersAsync(parameters);
    }

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
        if (IsSortable && string.IsNullOrWhiteSpace(SortName))
        {
            throw new InvalidOperationException($"{GetType().Name} '{EffectiveTitle}' sorts, so it needs a {nameof(SortName)}: the name its sort goes by in the address, the same in every culture.");
        }

        Grid.AddColumn(this);
    }
}
