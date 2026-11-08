namespace Forma.Framework.Culture.Router;

/// <summary>The culture pairs an application serves and how they are addressed.</summary>
public sealed class CultureRouterOptions
{
    /// <summary>The <see cref="CultureRoute.Token"/> of the route serving a request that names none.</summary>
    public required string DefaultCulture { get; init; }

    /// <summary>Where a culture token appears in a URL.</summary>
    public CulturePrefix Prefix { get; init; } = CulturePrefix.Always;

    /// <summary>Every route the application serves.</summary>
    public required IReadOnlyList<CultureRoute> Routes { get; init; }

    /// <summary>How a switch to another route navigates.</summary>
    public CultureSwitchMode Switch { get; init; } = CultureSwitchMode.FullLoad;

    /// <summary>How long a switch in place waits for its navigation to land, past which it completes as timed out.</summary>
    public TimeSpan SwitchTimeout { get; init; } = TimeSpan.FromSeconds(30);
}
