namespace Forma.Framework.Catalog;

/// <summary>How a catalog item participates in presentation and layout.</summary>
public enum CatalogItemVisibility
{
    /// <summary>The item is shown and occupies its normal layout space.</summary>
    Visible,

    /// <summary>The item is left out of presentation and layout.</summary>
    Hidden,

    /// <summary>The item is concealed and keeps its layout space.</summary>
    Invisible
}
