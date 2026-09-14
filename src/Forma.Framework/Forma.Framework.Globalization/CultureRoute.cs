namespace Forma.Framework.Globalization;

using System.Globalization;

/// <summary>A culture an application serves, and the URL token that addresses it.</summary>
public sealed record CultureRoute
{
    private readonly string? _hrefLang;

    /// <summary>Tokens that address this culture but redirect to <see cref="Token"/>.</summary>
    public IReadOnlyList<string> Aliases { get; init; } = [];

    /// <summary>The culture applied to a request that selects this route.</summary>
    public required CultureInfo Culture { get; init; }

    /// <summary>The value emitted in hreflang annotations; defaults to <see cref="Token"/>.</summary>
    public string HrefLang
    {
        get => _hrefLang ?? Token;
        init => _hrefLang = value;
    }

    /// <summary>The canonical first path segment selecting this culture.</summary>
    public required string Token { get; init; }
}
