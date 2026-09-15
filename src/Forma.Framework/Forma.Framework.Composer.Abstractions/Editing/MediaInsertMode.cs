namespace Forma.Framework.Composer.Editing;

/// <summary>How a chosen file the browser can render is put into the document.</summary>
public enum MediaInsertMode
{
    /// <summary>Rendered where it is inserted, as a picture or a player.</summary>
    Embed,

    /// <summary>A link carrying the file's name.</summary>
    Link,

    /// <summary>Rendered where it is inserted, with a link beside it.</summary>
    Both,
}
