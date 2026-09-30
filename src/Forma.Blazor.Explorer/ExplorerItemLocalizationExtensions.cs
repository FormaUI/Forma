namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Selection;
using Forma.Framework.Localization;

/// <summary>Creates localized presentation contexts for explorer items.</summary>
internal static class ExplorerItemLocalizationExtensions
{
    /// <summary>The localized labels of the item.</summary>
    internal static ExplorerItemLocalization GetLocalizedLabels(
        this ICatalogItem item,
        IStringLocale locale)
        => new(item, locale);

    /// <summary>The localized selection labels of the item.</summary>
    internal static ExplorerItemSelectionLocalization GetLocalizedLabels(
        this ICatalogItem item,
        IStringLocale locale,
        ICatalogSelectionView selection)
        => new(item, locale, selection);
}
