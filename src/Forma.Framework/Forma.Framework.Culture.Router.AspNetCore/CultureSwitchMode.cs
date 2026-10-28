namespace Forma.Framework.Culture.Router.AspNetCore;

/// <summary>How a switch to another culture navigates.</summary>
public enum CultureSwitchMode
{
    /// <summary>Every culture's address loads as a new document under a base in its culture.</summary>
    FullLoad,

    /// <summary>Interactive routing reaches every culture's address in the same circuit, through pages carrying their culture route.</summary>
    InPlace
}
