namespace Forma.UI.Blazor.Showcase.Components.Pages.Indicators;

using Forma.Framework.Indicators;
using Forma.Framework.Localization;
using Forma.Framework.Style.Css;
using Forma.Framework.Style.Glyphs;
using Forma.UI.Components.Indicators;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaStatusBadgePage : ComponentBase
{
    [Inject] private IStringLocale<FormaStatusBadgePage> L { get; set; } = default!;

    [Inject] private IStatusBadgeCatalog Catalog { get; set; } = default!;

    private (string Name, string Label)[] OrderStatuses => field ??=
    [
        ("order-new", L["New order"]),
        ("order-progress", L["Being handled"]),
        ("order-transit", L["On the way"]),
        ("order-done", L["Completed"]),
        ("order-cancelled", L["Cancelled"])
    ];

    // A second application vocabulary, registered from the page to show the catalog API.
    protected override void OnInitialized()
    {
        Catalog.Add(new("order-new", CssColor.FromRgba(73, 184, 124, 0.18), CssColor.FromHex(0x1c7430)) { Marker = Glyph.CartShopping });
        Catalog.Add(new("order-progress", CssColor.FromRgba(116, 122, 242, 0.18), CssColor.FromHex(0x4b50c4)) { Marker = Glyph.BoxOpen });
        Catalog.Add(new("order-transit", CssColor.FromRgba(255, 121, 121, 0.22), CssColor.FromHex(0xb23c3c)) { Marker = Glyph.Truck });
        Catalog.Add(new("order-done", CssColor.FromRgba(22, 168, 255, 0.16), CssColor.FromHex(0x0b6ea8)) { Marker = Glyph.CircleCheck });
        Catalog.Add(new("order-cancelled", CssColor.FromRgba(125, 137, 156, 0.20), CssColor.FromHex(0x5a6675)) { Marker = Glyph.Ban });
    }
}
