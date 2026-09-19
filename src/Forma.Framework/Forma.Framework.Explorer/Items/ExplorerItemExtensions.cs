namespace Forma.Framework.Explorer.Items;

using Forma.Framework.Catalog;

/// <summary>Reads what an explorer item can do under the options of the explorer listing it.</summary>
public static class ExplorerItemExtensions
{
    /// <summary>The capabilities the item supports and the options permit on it; none while the item is disabled.</summary>
    public static CatalogItemCapabilities GetCapabilities(this ICatalogItem item, ExplorerItemOptions options)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(options);

        if (item.IsDisabled)
        {
            return CatalogItemCapabilities.None;
        }

        return item.Capabilities & (item is ICatalogFolderItem ? options.AllowedFolderCapabilities : options.AllowedCapabilities);
    }

    /// <summary>Whether the item supports the capability and the options permit it.</summary>
    public static bool Permits(this ICatalogItem item, ExplorerItemOptions options, CatalogItemCapabilities capability)
        => item.GetCapabilities(options).HasFlag(capability);
}
