namespace Forma.Framework.Themes;

/// <summary>Applies a choice a switch accepted to the user scope's theme state.</summary>
public interface IThemeStateIntegration
{
    /// <summary>Applies <paramref name="acceptedTheme"/> where its version follows the one the state holds.</summary>
    /// <param name="acceptedTheme">The choice accepted, with its version.</param>
    /// <returns>A task that completes once the state's subscribers have heard of the change.</returns>
    Task ApplyAsync(AcceptedTheme acceptedTheme);
}
