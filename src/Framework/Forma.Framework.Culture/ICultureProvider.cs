namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>The provider of the culture an application formats and chooses strings in for each culture name, such as a culture whose patterns the application sets itself.</summary>
/// <remarks>
/// Where a provider is registered, every culture Forma holds or applies is the provider's for its name: the culture state's start and each
/// pair it applies, a pair the page's culture coordinator delivers by name, and a request's cultures where culture routing or
/// <c>UseFormaRequestCulture</c> sets them. A provider is registered with a lifetime the culture state reading it can resolve: on a server,
/// scoped where its cultures differ by user scope; under WebAssembly and for a Blazor Hybrid application's one culture, a singleton.
/// </remarks>
public interface ICultureProvider
{
    /// <summary>Gets the culture the application uses for the culture name.</summary>
    /// <param name="name">The culture's name, such as <c>de-DE</c>, or the empty name of the invariant culture.</param>
    /// <returns>The culture, carrying <paramref name="name"/> as its name.</returns>
    CultureInfo GetCulture(string name);
}
