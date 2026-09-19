namespace Forma.Framework.Catalog;

/// <summary>How a catalog item participates in presentation and layout.</summary>
public enum CatalogItemVisibility
{
    /// <summary>The item is shown and occupies its normal layout space.</summary>
    Visible,

    /// <summary>The item is not shown and occupies no layout space.</summary>
    Hidden,

    /// <summary>The item is not shown but retains its layout space.</summary>
    Invisible
}
