namespace Forma.Framework.Culture.Browser.Interop;

/// <summary>How a switch persists the pair the page's culture coordinator accepts, which the page publishes for its coordinator.</summary>
public sealed record CultureSwitchPolicy
{
    /// <summary>The policy that persists nothing beyond the page.</summary>
    public static CultureSwitchPolicy Default { get; } = new();

    /// <summary>Where a switch keeps the pair it accepts.</summary>
    public CultureSwitchPersistence Persistence { get; init; } = CultureSwitchPersistence.State;

    /// <summary>The name of the cookie a switch writes, the one request localization's cookie provider reads.</summary>
    public string CookieName { get; init; } = ".AspNetCore.Culture";

    /// <summary>The path the cookie is written at.</summary>
    public string CookiePath { get; init; } = "/";

    /// <summary>The domain the cookie is written for, where it is one other than the page's own host.</summary>
    public string? CookieDomain { get; init; }

    /// <summary>How long the cookie lasts, where it outlives the browser's session.</summary>
    public TimeSpan? CookieLifetime { get; init; } = TimeSpan.FromDays(365);

    /// <summary>Whether the cookie is sent over secure connections alone.</summary>
    public bool IsCookieSecure { get; init; }

    /// <summary>The sites the cookie is sent from.</summary>
    public CultureCookieSameSite CookieSameSite { get; init; } = CultureCookieSameSite.Lax;
}
