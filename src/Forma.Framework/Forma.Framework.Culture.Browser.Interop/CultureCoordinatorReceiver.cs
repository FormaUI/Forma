namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

using Microsoft.JSInterop;

/// <summary>The receiver a culture coordinator registration hands the page's coordinator, which delivers each pair the page accepts to it.</summary>
public sealed class CultureCoordinatorReceiver
{
    private readonly CultureCoordinatorRegistration _registration;

    internal CultureCoordinatorReceiver(CultureCoordinatorRegistration registration)
    {
        _registration = registration;
    }

    /// <summary>Applies the pair of <paramref name="culture"/> and <paramref name="uiCulture"/> the coordinator accepted as number <paramref name="number"/> of the document <paramref name="documentKey"/> names.</summary>
    /// <returns>A task that completes once the culture state has applied the pair and notified its subscribers.</returns>
    [JSInvokable]
    public Task ApplyAsync(string culture, string uiCulture, string documentKey, int number)
        => _registration.ApplyAsync(new AcceptedCulture(CultureInfo.GetCultureInfo(culture), CultureInfo.GetCultureInfo(uiCulture), new CultureVersion(documentKey, number)));
}
