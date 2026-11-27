namespace Forma.Blazor.Showcase.Components.Pages.Feedback;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Css;
using Forma.Framework.Feedback;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

public sealed partial class FormaStatusBadgePage
{
    [Inject] private IStatusBadgeCatalog Catalog { get; set; } = default!;

    private (string Name, string Label)[] OrderStatuses =>
    [
        ("order-new", L["New order"]),
        ("order-progress", L["Being handled"]),
        ("order-transit", L["On the way"]),
        ("order-done", L["Completed"]),
        ("order-canceled", L["Canceled"])
    ];

    private (string Name, string Label)[] ThemeStatuses =>
    [
        ("theme-booked", L["Booked"]),
        ("theme-flagged", L["Flagged"]),
        ("theme-archived", L["Archived"])
    ];

    // Registers the order statuses as a second application vocabulary, each on an opaque tint so it reads in every scheme.
    /// <inheritdoc />
    protected override void OnInitialized()
    {
        Catalog.Add(new("order-new", CssColor.FromHex(0xdef2e7), CssColor.FromHex(0x1c7430)) { Marker = Glyph.Cart });
        Catalog.Add(new("order-progress", CssColor.FromHex(0xe6e7fd), CssColor.FromHex(0x4b50c4)) { Marker = Glyph.Package });
        Catalog.Add(new("order-transit", CssColor.FromHex(0xffe2e2), CssColor.FromHex(0xb23c3c)) { Marker = Glyph.Truck });
        Catalog.Add(new("order-done", CssColor.FromHex(0xdaf1ff), CssColor.FromHex(0x0b6ea8)) { Marker = Glyph.CircleCheck });
        Catalog.Add(new("order-canceled", CssColor.FromHex(0xe5e7eb), CssColor.FromHex(0x5a6675)) { Marker = Glyph.Prohibited });

        // Statuses read from the theme's role tokens, which a theme recolors by redefining them.
        Catalog.Add(new("theme-booked", CssColorValue.FromProperty("--primary-subtle-color"), CssColorValue.FromProperty("--primary-ink-color")) { Marker = Glyph.Clock });
        Catalog.Add(new("theme-flagged", CssColorValue.FromProperty("--danger-subtle-color"), CssColorValue.FromProperty("--danger-ink-color")) { Marker = Glyph.Warning });
        Catalog.Add(new("theme-archived", CssColorValue.FromProperty("--secondary-subtle-color"), CssColorValue.FromProperty("--secondary-ink-color")) { Marker = Glyph.Package });
    }
}
