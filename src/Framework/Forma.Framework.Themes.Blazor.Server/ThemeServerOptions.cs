namespace Forma.Framework.Themes.Blazor.Server;

using Microsoft.AspNetCore.Http;

/// <summary>How the server's half resolves a request's theme choice.</summary>
public sealed class ThemeServerOptions
{
    /// <summary>The choice the host gives a request, which the request starts in ahead of the store and the browser's record and of which its page keeps nothing; <see langword="null"/> from it gives the request none.</summary>
    public Func<HttpContext, ThemeChoice?>? GivenChoice { get; set; }
}
