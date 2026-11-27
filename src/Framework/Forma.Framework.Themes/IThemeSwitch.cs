namespace Forma.Framework.Themes;

/// <summary>Requests a change of the theme choice every runtime on the page draws.</summary>
public interface IThemeSwitch
{
    /// <summary>Requests <paramref name="choice"/>.</summary>
    /// <param name="choice">The choice requested, naming a theme that is selectable.</param>
    /// <returns>A task that completes once the switch was accepted, kept where the page keeps its choice, and applied by the requesting runtime.</returns>
    Task SwitchAsync(ThemeChoice choice);
}
