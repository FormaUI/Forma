namespace Forma.Blazor.Components.Explorer;

using Forma.Features.Explorer;
using Forma.Framework.Localization;

/// <summary>Creates localized presentation contexts for explorer items.</summary>
internal static class ExplorerItemLocalizationExtensions
{
    /// <summary>Combines the item with the context needed to produce its localized labels.</summary>
    internal static ExplorerItemLocalization GetLocalizedLabels(
        this IExplorerItem item,
        IStringLocale<FormaExplorer> locale)
        => new(item, locale);

    /// <summary>Combines the item with the context needed to produce its localized labels.</summary>
    internal static ExplorerItemSelectionLocalization GetLocalizedLabels(
        this IExplorerItem item,
        IStringLocale<FormaExplorer> locale,
        IExplorerSelectionView selection)
        => new(item, locale, selection);
}
