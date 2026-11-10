namespace Forma.Framework.Catalog;

/// <summary>The kind of thing a catalog item is.</summary>
public enum CatalogItemKind
{
    /// <summary>A folder.</summary>
    Folder,

    /// <summary>An image.</summary>
    Image,

    /// <summary>A video.</summary>
    Video,

    /// <summary>A sound recording.</summary>
    Audio,

    /// <summary>An archive of other files.</summary>
    Archive,

    /// <summary>A document.</summary>
    Document,

    /// <summary>A file of any other kind.</summary>
    File,

    /// <summary>An item with no resource behind it.</summary>
    Unknown
}
