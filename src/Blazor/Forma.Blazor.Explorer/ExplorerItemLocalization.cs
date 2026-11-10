namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Localization;

/// <summary>Produces localized presentation labels for an explorer item.</summary>
internal readonly struct ExplorerItemLocalization
{
    private readonly ICatalogItem _item;
    private readonly IStringLocale _locale;

    internal ExplorerItemLocalization(
        ICatalogItem item,
        IStringLocale locale)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(locale);

        _item = item;
        _locale = locale;
    }

    /// <summary>The localized name of the item's kind.</summary>
    internal string KindLabel
        => GetKindLabel(_locale, _item.GetKind());

    /// <summary>The localized name of <paramref name="kind"/>.</summary>
    internal static string GetKindLabel(IStringLocale locale, CatalogItemKind kind)
        => kind switch
        {
            CatalogItemKind.Folder => locale["Folder"].Value,
            CatalogItemKind.Image => locale["Image"].Value,
            CatalogItemKind.Video => locale["Video"].Value,
            CatalogItemKind.Audio => locale["Audio"].Value,
            CatalogItemKind.Archive => locale["Archive"].Value,
            CatalogItemKind.Document => locale["Document"].Value,
            CatalogItemKind.File => locale["File"].Value,
            _ => locale["Unknown"].Value
        };

    /// <summary>The accessible label for the item's actions menu.</summary>
    internal string ActionsLabel
        => _locale["Actions for {Name}", _item.Name].Value;
}
