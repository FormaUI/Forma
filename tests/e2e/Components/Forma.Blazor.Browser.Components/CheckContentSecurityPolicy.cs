namespace Forma.Blazor.Browser.Components;

using System.Security.Cryptography;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>The strict Content Security Policy the check pages are served under, so a script or a style Forma emits that it would block fails a check.</summary>
/// <remarks>
/// No inline script runs but the import map Blazor renders, which carries the request's nonce, nothing is evaluated from text, and
/// WebAssembly is compiled only on a page whose runtime is WebAssembly. A style element or sheet loads from the page's own origin alone,
/// and a style attribute is allowed through <c>style-src-attr</c>, since every root and part a component sizes or places writes one, each
/// from a typed value.
/// </remarks>
internal static class CheckContentSecurityPolicy
{
    // The key the request's nonce is kept under for the page's head.
    private const string NonceKey = "Forma.CheckContentSecurityPolicy.Nonce";

    /// <summary>Serves each response under the policy, with a nonce of its own.</summary>
    public static IApplicationBuilder UseCheckContentSecurityPolicy(this IApplicationBuilder app)
        => app.Use((context, next) =>
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            context.Items[NonceKey] = nonce;
            context.Response.Headers.ContentSecurityPolicy = Format(nonce, RunsWebAssembly(context.Request.Path));

            return next(context);
        });

    /// <summary>The nonce the request's policy allows its inline scripts by.</summary>
    public static string? GetNonce(HttpContext? context)
        => context?.Items[NonceKey] as string;

    // A page's runtime is its address's first segment, and Auto runs on WebAssembly once its resources have downloaded.
    private static bool RunsWebAssembly(PathString path)
        => path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() is "webassembly" or "auto";

    private static string Format(string nonce, bool runsWebAssembly)
        => string.Join(
            "; ",
            "default-src 'self'",
            "base-uri 'self'",
            "object-src 'none'",
            "frame-ancestors 'self'",
            "form-action 'self'",
            $"script-src 'self' 'nonce-{nonce}'{(runsWebAssembly ? " 'wasm-unsafe-eval'" : string.Empty)}",
            "style-src 'self'",
            "style-src-attr 'unsafe-inline'",
            "img-src 'self' data: blob:",
            "media-src 'self' data: blob:",
            "font-src 'self'",
            "connect-src 'self'");
}
