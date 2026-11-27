namespace Forma.Framework.Themes.Blazor.Server;

using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;

/// <summary>The browser's record of the theme choice, the choice cookie: the choice, and the store's revision it last agreed with.</summary>
/// <remarks>
/// The cookie is <c>__Host-forma-theme</c> over HTTPS, so only the page's own origin sets it, and <c>forma-theme</c> over plain
/// HTTP, as a development host serves, where a browser refuses a secure cookie. Its value is a query string,
/// <c>theme=harbor&amp;scheme=dark&amp;revision=3</c> escaped as a cookie value is, which the page's coordinator writes and reads too.
/// </remarks>
internal sealed record ThemeRecord(ThemeChoice Choice, long Revision)
{
    /// <summary>The cookie's name over HTTPS.</summary>
    public const string SecureCookieName = "__Host-forma-theme";

    /// <summary>The cookie's name over plain HTTP.</summary>
    public const string CookieName = "forma-theme";

    // How long the browser keeps the record.
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(365);

    /// <summary>The name of the cookie <paramref name="request"/> carries the record in.</summary>
    public static string GetCookieName(HttpRequest request)
        => request.IsHttps ? SecureCookieName : CookieName;

    /// <summary>Reads the record <paramref name="request"/> carries, where it carries one the record's format holds.</summary>
    public static ThemeRecord? Read(HttpRequest request)
        => request.Cookies.TryGetValue(GetCookieName(request), out var value) ? Parse(value) : null;

    /// <summary>Reads a record from the cookie's value, refusing one that isn't a choice and a revision.</summary>
    public static ThemeRecord? Parse(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        var fields = QueryHelpers.ParseQuery(value);

        if (!fields.TryGetValue("theme", out var theme) || string.IsNullOrWhiteSpace(theme)
            || !fields.TryGetValue("scheme", out var scheme) || !ThemeAttributes.TryParseScheme(scheme, out var preference)
            || !fields.TryGetValue("revision", out var revisionText) || !long.TryParse(revisionText, NumberStyles.None, CultureInfo.InvariantCulture, out var revision))
        {
            return null;
        }

        return new ThemeRecord(new ThemeChoice(theme.ToString(), preference), revision);
    }

    /// <summary>Writes the record into the response's choice cookie.</summary>
    public void Write(HttpContext context)
        => context.Response.Cookies.Append(GetCookieName(context.Request), Format(), new CookieOptions
        {
            Path = "/",
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            MaxAge = Lifetime,
        });

    /// <summary>The record as the cookie's value.</summary>
    public string Format()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"theme={Uri.EscapeDataString(Choice.Theme)}&scheme={ThemeAttributes.GetSchemeValue(Choice.Scheme)}&revision={Revision}");
}
