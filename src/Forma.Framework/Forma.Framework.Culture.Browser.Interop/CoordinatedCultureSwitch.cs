namespace Forma.Framework.Culture.Browser.Interop;

using System.Globalization;

/// <summary>A switch that requests each change through the page's culture coordinator, which accepts it for every runtime on the page.</summary>
/// <param name="registration">The user scope's registration with the coordinator.</param>
public sealed class CoordinatedCultureSwitch(CultureCoordinatorRegistration registration) : ICultureSwitch
{
    /// <inheritdoc />
    /// <remarks>
    /// A switch requested while the registration is under way waits for it and fails with it; with no registration under way, or one
    /// unavailable, it fails at once with <see cref="InvalidOperationException"/>, as it does in a static render.
    /// </remarks>
    public Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return registration.RequestAsync(culture, uiCulture ?? culture);
    }
}
