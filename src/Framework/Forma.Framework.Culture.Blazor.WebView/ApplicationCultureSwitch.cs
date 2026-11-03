namespace Forma.Framework.Culture.Blazor.WebView;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;

/// <summary>The application's switch under Blazor Hybrid, which requests each change of the application's culture coordinator, so a component in any web view and the host's native code switch through one entry point.</summary>
/// <param name="coordinator">The application's culture coordinator.</param>
/// <param name="addressing">The culture addresses the web views' culture follows, where culture routing is registered.</param>
internal sealed class ApplicationCultureSwitch(ApplicationCultureCoordinator coordinator, ICultureAddressing? addressing) : ICultureSwitch
{
    /// <inheritdoc />
    /// <remarks>
    /// The switch is accepted at once, and completes once every subscriber of the state has been notified; each web view's document
    /// and address follow it on the web view's own thread. Where culture addresses decide the culture, a pair no route carries fails
    /// at once with <see cref="InvalidOperationException"/>, before anything changes.
    /// </remarks>
    public Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        uiCulture ??= culture;

        if (addressing is not null && !addressing.CanAddress(culture, uiCulture))
        {
            throw new InvalidOperationException($"No configured culture route represents the culture '{culture.Name}' with the UI culture '{uiCulture.Name}', so the switch can't be made by address.");
        }

        return coordinator.SwitchAsync(culture, uiCulture);
    }
}
