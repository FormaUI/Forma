namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Selection;
using Forma.Framework.Localization;

/// <summary>Creates localized presentation contexts for explorer items.</summary>
internal static class ExplorerItemLocalizationExtensions
{
    /// <summary>Combines the item with the context needed to produce its localized labels.</summary>
    internal static ExplorerItemLocalization GetLocalizedLabels(
        this ICatalogItem item,
        IStringLocale locale)
        => new(item, locale);

    /// <summary>Combines the item with the context needed to produce its localized labels.</summary>
    internal static ExplorerItemSelectionLocalization GetLocalizedLabels(
        this ICatalogItem item,
        IStringLocale locale,
        ICatalogSelectionView selection)
        => new(item, locale, selection);
}
