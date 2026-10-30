namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

/// <summary>A switch that requests each change through the page's culture coordinator, which accepts it for every runtime on the page.</summary>
/// <param name="registration">The user scope's registration with the coordinator.</param>
/// <param name="addressing">The culture addresses the page's culture follows, where culture routing is registered.</param>
public sealed class CoordinatedCultureSwitch(CultureCoordinatorRegistration registration, ICultureAddressing? addressing = null) : ICultureSwitch
{
    /// <inheritdoc />
    /// <remarks>
    /// A switch requested while the registration is under way waits for it and fails with it; with no registration under way, or one
    /// unavailable, it fails at once with <see cref="InvalidOperationException"/>, as it does in a static render. Where culture
    /// addresses decide the culture, a pair no route carries fails at once with <see cref="InvalidOperationException"/>, before
    /// anything changes, and a switch whose navigation didn't land completes as canceled.
    /// </remarks>
    public Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        uiCulture ??= culture;

        if (addressing is not null && !addressing.CanAddress(culture, uiCulture))
        {
            throw new InvalidOperationException($"No configured culture route represents the culture '{culture.Name}' with the UI culture '{uiCulture.Name}', so the switch can't be made by address.");
        }

        return registration.RequestAsync(culture, uiCulture);
    }
}
