namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.DataView;

public sealed partial class FormaDataViewPage
{
    private FormaDataView<ShowcaseProduct>? _dataView;

    private IReadOnlyList<ShowcaseProduct> _selectedProducts = [];

    // Named by the grid columns' sort names, their property paths, so a menu sort marks the column header and a link holds the same sort in every culture.
    private IReadOnlyList<DataViewSortOption<ShowcaseProduct>> _sorts = [];

    /// <inheritdoc />
    protected override void OnInitialized()
        => _sorts = CreateSorts();

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender)
    {
        // The switch takes the data view by reference, which the first render captures, so the page draws it on the second.
        if (firstRender)
        {
            StateHasChanged();
        }
    }

    /// <inheritdoc />
    protected override Task OnCultureChangedAsync()
    {
        _sorts = CreateSorts();

        return Task.CompletedTask;
    }

    private void HandleSelectionChanged(IReadOnlyList<ShowcaseProduct> selectedProducts)
        => _selectedProducts = selectedProducts;

    private List<DataViewSortOption<ShowcaseProduct>> CreateSorts()
        =>
        [
            new("Name", L["Name"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Name)),
            new("Category", L["Category"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Category).ThenAscending(product => product.Name)),
            new("Price", L["Price"], DataViewSort<ShowcaseProduct>.ByAscending(product => product.Price)) { InitialDirection = DataViewSortDirection.Descending },
        ];
}
