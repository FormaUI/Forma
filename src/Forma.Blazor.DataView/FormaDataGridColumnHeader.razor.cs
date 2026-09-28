namespace Forma.Blazor.Components.DataView;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>The content of a column's header cell: its title, sorting by it where it sorts, and the button that opens its options.</summary>
/// <typeparam name="TItem">The item a row shows.</typeparam>
public sealed partial class FormaDataGridColumnHeader<TItem> : FormaComponentBase
    where TItem : notnull
{
    /// <summary>The column the header cell belongs to.</summary>
    [Parameter, EditorRequired] public FormaColumnBase<TItem> Column { get; set; } = default!;

    [CascadingParameter] private FormaDataGrid<TItem> Grid { get; set; } = default!;

    [CascadingParameter] private IDataView<TItem> View { get; set; } = default!;

    private FormaButton OptionsButton { get; set; } = default!;

    private FormaPopover OptionsPopover { get; set; } = default!;

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaDataGrid<TItem>);

    /// <summary>Whether the column's options surface is open.</summary>
    public bool IsOptionsOpen
        => OptionsPopover?.IsOpen == true;

    private string OptionsId
        => $"{Grid.ElementId}-options-{Grid.GetColumnIndex(Column)}";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Column);

        if (Grid is null || View is null)
        {
            throw new InvalidOperationException($"{nameof(FormaDataGridColumnHeader<TItem>)} must be rendered inside a {nameof(FormaDataGrid<TItem>)}.");
        }

        Grid.RegisterHeader(Column, this);
    }

    /// <summary>Opens the column's options surface.</summary>
    public Task ShowOptionsAsync()
        => OptionsPopover is null || OptionsPopover.IsOpen ? Task.CompletedTask : OptionsPopover.OpenAsync(OptionsButton.RootElement);

    /// <summary>Closes the column's options surface.</summary>
    public Task HideOptionsAsync()
        => OptionsPopover is null || !OptionsPopover.IsOpen ? Task.CompletedTask : OptionsPopover.CloseAsync();

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        Grid.UnregisterHeader(Column, this);

        return base.DisposeAsyncCore();
    }

    private Task HandleSortClickAsync()
        => Grid.SortByColumnCoreAsync(Column);

    private Task ToggleOptionsAsync()
        => OptionsPopover.ToggleAsync(OptionsButton.RootElement);
}
