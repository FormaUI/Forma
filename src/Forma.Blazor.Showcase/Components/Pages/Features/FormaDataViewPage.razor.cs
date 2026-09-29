namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.DataView;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaDataViewPage : ComponentBase
{
    [Inject] private IStringLocale<FormaDataViewPage> L { get; set; } = default!;

    private FormaDataView<ShowcaseProduct>? _dataView;

    private IReadOnlyList<ShowcaseProduct> _selectedProducts = [];

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
