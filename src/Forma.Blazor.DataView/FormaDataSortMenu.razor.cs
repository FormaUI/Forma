namespace Forma.Blazor.Components.DataView;

using Forma.Blazor.Components.Buttons;
using Forma.Blazor.Components.Menus;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A drop-down that sorts a data view by the sorts it offers and turns the direction, marking the sort applied; a page composes it beside the data view it sorts, whichever view shows.</summary>
/// <typeparam name="TItem">The item the data view manages.</typeparam>
public sealed partial class FormaDataSortMenu<TItem> : FormaElementBase
    where TItem : notnull
{
    private static readonly ButtonOptions TriggerOptions = new() { Variant = ButtonVariant.Ghost };

    private static readonly DataViewSortDirection[] Directions = [DataViewSortDirection.Ascending, DataViewSortDirection.Descending];

    private IDataViewSorter<TItem>? _observedDataView;

    /// <summary>The sorts the menu offers, in the order it lists them.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<DataViewSortOption<TItem>> Sorts { get; set; } = default!;

    /// <summary>The data view the menu sorts, for a menu composed outside it; inside, the data view reaches the menu itself.</summary>
    [Parameter] public IDataViewSorter<TItem>? DataView { get; set; }

    [CascadingParameter] private IDataViewSorter<TItem>? EnclosingDataView { get; set; }

    private FormaDropDownMenu? Menu { get; set; }

    /// <inheritdoc />
    public override ElementReference RootElement
        => Menu is null ? default : Menu.RootElement;

    // The data view the menu drives: the one given, else the one it stands inside.
    private IDataViewSorter<TItem> Sorter
        => DataView ?? EnclosingDataView ?? throw new InvalidOperationException($"{nameof(FormaDataSortMenu<>)} needs a {nameof(DataView)}, or to be declared inside a data view.");

    // The option the data view is sorted by, where the sort applied is one the menu offers.
    private DataViewSortOption<TItem>? CurrentOption
        => Sorter.SortName is { } name ? Sorts.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.Ordinal)) : null;

    private string TriggerLabel
        => CurrentOption is { } current
            ? L["Sorted by {Name}, {Direction}", current.Label, GetDirectionLabel(Sorter.SortDirection)].Value
            : L["Sort"].Value;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Sorts);

        if (Sorts.GroupBy(option => option.Name, StringComparer.Ordinal).FirstOrDefault(names => names.Count() > 1) is { } repeated)
        {
            throw new ArgumentException($"{nameof(FormaDataSortMenu<>)} offers the sort '{repeated.Key}' twice; each sort's name is what the address carries, so names must differ.", nameof(Sorts));
        }

        // The menu follows the data view it drives, and lets go of one it drove before.
        if (!ReferenceEquals(_observedDataView, Sorter))
        {
            _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);
            _observedDataView = Sorter;
            _observedDataView.Subscribe(HandleDataViewChangedAsync);
        }
    }

    // A sort the address asked for by the name of one the menu offers opens the data view sorted by it.
    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Sorter.SortName is null
            && Sorter.RequestedSortName is { } requested
            && Sorts.FirstOrDefault(option => string.Equals(option.Name, requested, StringComparison.Ordinal)) is { } option)
        {
            await Sorter.SortByAsync(option.Name, option.Sort, Sorter.RequestedSortDirection);
        }
    }

    private bool IsCurrent(DataViewSortOption<TItem> option)
        => ReferenceEquals(CurrentOption, option);

    // The sort applied keeps its direction when chosen again; another opens in its own.
    private Task ChooseAsync(DataViewSortOption<TItem> option)
        => Sorter.SortByAsync(option.Name, option.Sort, IsCurrent(option) ? Sorter.SortDirection : option.InitialDirection);

    private Task TurnAsync(DataViewSortDirection direction)
        => CurrentOption is { } current ? Sorter.SortByAsync(current.Name, current.Sort, direction) : Task.CompletedTask;

    private string GetDirectionLabel(DataViewSortDirection direction)
        => direction == DataViewSortDirection.Ascending ? L["Ascending"].Value : L["Descending"].Value;

    private Task HandleDataViewChangedAsync()
        => InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add("forma-data-sort-menu");
    }

    /// <inheritdoc />
    protected override ValueTask DisposeAsyncCore()
    {
        _observedDataView?.Unsubscribe(HandleDataViewChangedAsync);

        return base.DisposeAsyncCore();
    }
}
