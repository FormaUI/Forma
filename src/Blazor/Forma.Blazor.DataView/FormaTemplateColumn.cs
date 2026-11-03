namespace Forma.Blazor.Components.DataView;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

/// <summary>A column that renders whatever the caller gives it for each item.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed class FormaTemplateColumn<TItem> : FormaColumnBase<TItem>
    where TItem : notnull
{
    private static readonly RenderFragment<TItem> _emptyCell = _ => _ => { };

    /// <summary>What a cell renders, given the row's item.</summary>
    [Parameter] public RenderFragment<TItem> ChildContent { get; set; } = _emptyCell;

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TItem item)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddContent(0, ChildContent(item));
    }
}
