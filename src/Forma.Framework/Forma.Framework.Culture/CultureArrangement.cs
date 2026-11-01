namespace Forma.Framework.Culture;

/// <summary>How a user scope's culture stands toward the others: a WebAssembly client's toward its page's, or a web view's toward its application's.</summary>
public enum CultureArrangement
{
    /// <summary>Every runtime on the page follows the page's culture coordinator, the client's starting in the pair the server rendered, or, in a standalone app, the page starting in the pair the client's startup chose; in a Blazor Hybrid application, every web view follows the application's.</summary>
    Coordinated,

    /// <summary>The client's culture is its own, switched alone, touching neither the document, the coordinator, the address nor the cookie; in a Blazor Hybrid application, each web view's culture is its own, switched alone, its document and its address following it.</summary>
    Independent,
}
