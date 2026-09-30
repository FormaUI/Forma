namespace Forma.Framework.Catalog;

using Forma.Framework.Resources;

/// <summary>An item in a catalog.</summary>
public interface ICatalogItem
{
    /// <summary>The item's key, a stable identifier unique within the catalog.</summary>
    string Key { get; }

    /// <summary>The item's display name.</summary>
    string Name { get; }

    /// <summary>The capabilities the item supports.</summary>
    CatalogItemCapabilities Capabilities { get; }

    /// <summary>How the item participates in presentation and layout.</summary>
    CatalogItemVisibility Visibility { get; }

    /// <summary>Whether the item is disabled, listed with its operations unavailable.</summary>
    bool IsDisabled { get; }

    /// <summary>When the item was created.</summary>
    DateTimeOffset? CreatedAt { get; }

    /// <summary>When the item was last modified.</summary>
    DateTimeOffset? ModifiedAt { get; }

    /// <summary>The source of the item's thumbnail.</summary>
    string? ThumbnailSource { get; }

    /// <summary>The resource the item wraps.</summary>
    ResourceInfo? Resource { get; }
}
