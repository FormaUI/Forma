namespace Forma.Framework.Themes.Browser.Interop;

using Microsoft.JSInterop;

/// <summary>The receiver a theme coordinator registration hands the page's coordinator, which delivers each choice the page accepts to it and asks it to write the host's store.</summary>
public sealed class ThemeCoordinatorReceiver
{
    private readonly ThemeCoordinatorRegistration _registration;

    internal ThemeCoordinatorReceiver(ThemeCoordinatorRegistration registration)
    {
        _registration = registration;
    }

    /// <summary>Applies the choice of <paramref name="theme"/> in <paramref name="scheme"/>, which the coordinator accepted as number <paramref name="number"/> of the document <paramref name="documentKey"/> names.</summary>
    /// <returns>A task that completes once the theme state holds the choice.</returns>
    [JSInvokable]
    public Task ApplyAsync(string theme, string scheme, string documentKey, int number)
        => _registration.ApplyAsync(theme, scheme, documentKey, number);

    /// <summary>Keeps the choice of <paramref name="theme"/> in <paramref name="scheme"/> in the host's store at <paramref name="revision"/>.</summary>
    /// <returns>A task that completes once the store keeps it, faulted where the store failed.</returns>
    [JSInvokable]
    public Task WriteStoreAsync(string theme, string scheme, long revision)
        => _registration.WriteStoreAsync(theme, scheme, revision);
}
