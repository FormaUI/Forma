namespace Forma.Framework.Culture;

/// <summary>How a page treats a WebAssembly client's culture.</summary>
public enum CultureArrangement
{
    /// <summary>Every runtime on the page follows the page's culture coordinator, the client's starting in the pair the server rendered, or, in a standalone app, the page starting in the pair the client's startup chose.</summary>
    Coordinated,

    /// <summary>The client's culture is its own, switched alone, touching neither the document, the coordinator, the address nor the cookie.</summary>
    Independent,
}
