namespace Forma.Framework.Culture.Browser.Interop;

using Microsoft.JSInterop;

/// <summary>The receiver a culture coordinator registration hands the page's coordinator, which delivers each pair the page accepts to it.</summary>
public sealed class CultureCoordinatorReceiver
{
    private readonly CultureCoordinatorRegistration _registration;

    internal CultureCoordinatorReceiver(CultureCoordinatorRegistration registration)
    {
        _registration = registration;
    }

    /// <summary>Applies the pair of the cultures <paramref name="culture"/> and <paramref name="uiCulture"/> name, which the coordinator accepted as number <paramref name="number"/> of the document <paramref name="documentKey"/> names.</summary>
    /// <returns>A task that completes once the culture state holds the pair.</returns>
    [JSInvokable]
    public Task ApplyAsync(string culture, string uiCulture, string documentKey, int number)
        => _registration.ApplyAsync(culture, uiCulture, new CultureVersion(documentKey, number));

    /// <summary>The address, relative to the application's root and with its culture segment, the page at <paramref name="relativeAddress"/> leads to in the route <paramref name="token"/> names, or <see langword="null"/> for that page's own address there.</summary>
    [JSInvokable]
    public Task<string?> ResolveDestinationAsync(string relativeAddress, string token)
        => _registration.ResolveDestinationAsync(relativeAddress, token);
}
