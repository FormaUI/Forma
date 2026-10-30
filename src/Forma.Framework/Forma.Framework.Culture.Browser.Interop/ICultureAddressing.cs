namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

/// <summary>The culture addresses a page's culture follows: the pairs an address carries, the document's base and the mapping the page publishes for its coordinator.</summary>
public interface ICultureAddressing
{
    /// <summary>Whether a culture address carries the pair of <paramref name="culture"/> and <paramref name="uiCulture"/>.</summary>
    bool CanAddress(CultureInfo culture, CultureInfo uiCulture);

    /// <summary>The document's base, under the application's root <paramref name="root"/>, for a page rendered in the pair of <paramref name="culture"/> and <paramref name="uiCulture"/>.</summary>
    /// <param name="root">The application's root, an escaped absolute path ending in a slash, such as <c>/app/</c>.</param>
    /// <param name="culture">The culture the page formats in.</param>
    /// <param name="uiCulture">The culture the page's strings and direction are in.</param>
    string GetDocumentBase(string root, CultureInfo culture, CultureInfo uiCulture);

    /// <summary>The mapping between addresses and pairs under the application's root <paramref name="root"/>, as the page publishes it.</summary>
    /// <param name="root">The application's root, an escaped absolute path ending in a slash, such as <c>/app/</c>.</param>
    CultureRouteMap GetMap(string root);
}
