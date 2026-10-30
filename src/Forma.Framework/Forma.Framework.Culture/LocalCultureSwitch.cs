namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>A switch that changes one user scope's culture state alone, accepting each switch at once in a sequence of its own.</summary>
public sealed class LocalCultureSwitch : ICultureSwitch
{
    private readonly ICultureStateIntegration _integration;
    private readonly string _documentKey = $"local-{Guid.CreateVersion7()}";
    private int _number;

    /// <summary>Creates the switch over the integration of the user scope's culture state.</summary>
    /// <param name="integration">The integration that applies each accepted pair to the state.</param>
    public LocalCultureSwitch(ICultureStateIntegration integration)
    {
        ArgumentNullException.ThrowIfNull(integration);

        _integration = integration;
    }

    /// <inheritdoc />
    /// <remarks>The method is synchronous up to the notification's task it returns, so the state and the caller's flow hold the pair once it returns.</remarks>
    public Task SwitchAsync(CultureInfo culture, CultureInfo? uiCulture = null)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var version = new CultureVersion(_documentKey, Interlocked.Increment(ref _number));

        return _integration.ApplyAsync(new AcceptedCulture(culture, uiCulture ?? culture, version));
    }
}
