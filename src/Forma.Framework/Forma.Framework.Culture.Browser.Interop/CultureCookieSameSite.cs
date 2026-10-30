namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>The sites the culture cookie is sent from.</summary>
public enum CultureCookieSameSite
{
    /// <summary>The cookie's own site, and a navigation into it from another.</summary>
    Lax,

    /// <summary>The cookie's own site alone.</summary>
    Strict,

    /// <summary>Every site, which a browser accepts only for a secure cookie.</summary>
    None,
}
