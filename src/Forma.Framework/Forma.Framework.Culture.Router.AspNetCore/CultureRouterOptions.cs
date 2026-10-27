namespace Forma.Framework.Culture.Router.AspNetCore;

/// <summary>The cultures an application serves and how they are addressed.</summary>
public sealed class CultureRouterOptions
{
    /// <summary>The <see cref="CultureRoute.Token"/> of the culture serving a request that names none.</summary>
    public required string DefaultCulture { get; init; }

    /// <summary>Where a culture token appears in a URL.</summary>
    public CulturePrefix Prefix { get; init; } = CulturePrefix.Always;

    /// <summary>Every culture the application serves.</summary>
    public required IReadOnlyList<CultureRoute> Routes { get; init; }
}
