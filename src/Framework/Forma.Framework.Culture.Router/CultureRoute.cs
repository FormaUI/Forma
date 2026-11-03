namespace Forma.Framework.Culture.Router;

using System.Globalization;

/// <summary>A culture pair an application serves, and the URL token that addresses it.</summary>
public sealed record CultureRoute
{
    private readonly CultureInfo? _culture;
    private readonly string? _hrefLang;

    /// <summary>Tokens that address this route but redirect to <see cref="Token"/>.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>The culture that formats dates, numbers and collation for a page this route selects, the UI culture where none is named.</summary>
    public CultureInfo Culture
    {
        get => _culture ?? UICulture;
        init => _culture = value;
    }

    /// <summary>The culture that chooses the strings and direction of a page this route selects.</summary>
    public required CultureInfo UICulture { get; init; }

    /// <summary>The value emitted in hreflang annotations.</summary>
    public string HrefLang
    {
        get => _hrefLang ?? Token;
        init => _hrefLang = value;
    }

    /// <summary>The canonical first path segment selecting this route.</summary>
    public required string Token { get; init; }
}
