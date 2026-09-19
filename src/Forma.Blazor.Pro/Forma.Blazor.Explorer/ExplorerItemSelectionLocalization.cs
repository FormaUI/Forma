namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Selection;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Selection;
using Forma.Framework.Localization;

/// <summary>Produces localized selection labels for an explorer item.</summary>
internal readonly struct ExplorerItemSelectionLocalization
{
    private readonly ICatalogItem _item;
    private readonly IStringLocale _locale;
    private readonly ICatalogSelectionView _selection;

    internal ExplorerItemSelectionLocalization(
        ICatalogItem item,
        IStringLocale locale,
        ICatalogSelectionView selection)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(locale);
        ArgumentNullException.ThrowIfNull(selection);

        _item = item;
        _locale = locale;
        _selection = selection;
    }

    /// <summary>The accessible label for selecting or deselecting the item.</summary>
    internal string SelectionLabel
        => _selection.IsSelected(_item)
            ? _locale["Deselect {Name}", _item.Name].Value
            : _locale["Select {Name}", _item.Name].Value;

    /// <summary>The accessible activation label appropriate to the current mode.</summary>
    internal string ActivationLabel
        => _selection.IsSelectionModeActive
            ? SelectionLabel
            : _locale["Preview {Name}", _item.Name].Value;
}
