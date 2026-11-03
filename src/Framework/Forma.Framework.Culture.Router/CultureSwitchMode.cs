namespace Forma.Framework.Culture.Router;

/// <summary>How a switch to another culture navigates.</summary>
public enum CultureSwitchMode
{
    /// <summary>Every culture's address loads as a new document under a base in its culture.</summary>
    FullLoad,

    /// <summary>Every culture's address is reached in the same document: through pages carrying their culture route under an interactive router, or by an enhanced navigation where the pages are islands.</summary>
    InPlace
}
