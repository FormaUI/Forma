namespace Forma.Blazor.Components.DataView;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web.Virtualization;

/// <summary>The table a grid draws: its header row, its rows, and the state row that stands in for them while loading or empty.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataGridTable<TItem> : FormaElementBase
    where TItem : notnull
{
    /// <summary>The grid the table draws for, which re-renders the table whenever the grid renders.</summary>
    [Parameter, EditorRequired] public FormaDataGrid<TItem> Grid { get; set; } = default!;

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    [CascadingParameter] private IDataViewInteraction<TItem> Interaction { get; set; } = default!;

    // The field the master box names to the input base; the selection is not a form field and nothing binds it.
    private bool MasterSelection { get; }

    /// <summary>The virtualization the table renders its rows through, while the data view virtualizes.</summary>
    internal Virtualize<TItem>? Virtualizer { get; private set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataGrid<TItem>);

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Grid);

        if (View is null || Interaction is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataGridTable<TItem>)} must be rendered inside a {nameof(FormaDataGrid<TItem>)}.");
        }
    }

    private Task HandleMasterClickAsync()
        => Interaction.ToggleMasterAsync();

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
    }
}
