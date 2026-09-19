namespace Forma.Blazor.Components.Explorer;

using Forma.Framework.Catalog;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Localization;
using Forma.Framework.Resources;

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
        => _item switch
        {
            ICatalogFolderItem => _locale["Folder"].Value,
            { Resource: ImageResourceInfo } => _locale["Image"].Value,
            { Resource: VideoResourceInfo } => _locale["Video"].Value,
            { Resource: AudioResourceInfo } => _locale["Audio"].Value,
            { Resource: ArchiveResourceInfo } => _locale["Archive"].Value,
            { Resource: DocumentResourceInfo } => _locale["Document"].Value,
            { Resource: FileResourceInfo } => _locale["File"].Value,
            _ => _locale["Unknown"].Value
        };

    /// <summary>The accessible label for the item's actions menu.</summary>
    internal string ActionsLabel
        => _locale["Actions for {Name}", _item.Name].Value;
}
