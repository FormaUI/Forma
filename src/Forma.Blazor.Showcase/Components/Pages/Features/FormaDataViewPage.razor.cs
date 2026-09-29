namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaDataViewPage : ComponentBase
{
    [Inject] private IStringLocale<FormaDataViewPage> L { get; set; } = default!;

    private FormaDataView<ShowcaseProduct>? _dataView;

    private IReadOnlyList<ShowcaseProduct> _selectedProducts = [];

    // Named as the grid's columns are titled, so a sort chosen from the menu marks the column's header too.
    private IReadOnlyList<DataViewSortOption<ShowcaseProduct>> _sorts = [];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _sorts =
        [
            new(L["Name"], L["Name"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Name)),
            new(L["Category"], L["Category"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Category).ThenAscending(product => product.Name)),
            new(L["Price"], L["Price"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Price)) { InitialDirection = DataViewSortDirection.Descending },
        ];

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        // The switch takes the data view by reference, which the first render captures, so the page draws it on the second.
        if (firstRender)
        {
            StateHasChanged();
        }
    }

    private void HandleSelectionChanged(IReadOnlyList<ShowcaseProduct> selected)
        => _selectedProducts = selected;
}
