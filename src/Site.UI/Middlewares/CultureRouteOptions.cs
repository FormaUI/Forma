namespace Site.UI.Middlewares;

public sealed class CultureRouteOptions
{
    public required string DefaultCulture { get; init; }
    public required IReadOnlyList<string> SupportedCultures { get; init; }
}
