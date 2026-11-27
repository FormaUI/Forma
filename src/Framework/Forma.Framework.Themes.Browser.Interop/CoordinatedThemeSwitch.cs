namespace Forma.Framework.Themes.Browser.Interop;

/// <summary>A switch that requests each change through the page's theme coordinator, which accepts it for every runtime on the page.</summary>
/// <param name="registration">The user scope's registration with the coordinator.</param>
public sealed class CoordinatedThemeSwitch(ThemeCoordinatorRegistration registration) : IThemeSwitch
{
    /// <inheritdoc />
    /// <remarks>
    /// A switch requested while the registration is under way waits for it and fails with it; with no registration under way, or one
    /// unavailable, it fails at once with <see cref="InvalidOperationException"/>, as it does in a static render, which switches with a
    /// request. A theme that isn't selectable fails with <see cref="InvalidOperationException"/> before anything changes, and a switch the
    /// coordinator receives after a later one requested through the same registration completes as canceled. A store's write that fails
    /// after the switch was accepted fails the switch with the store's exception, the choice staying accepted on the page.
    /// </remarks>
    public Task SwitchAsync(ThemeChoice choice)
    {
        ArgumentNullException.ThrowIfNull(choice);

        return registration.RequestAsync(choice);
    }
}
