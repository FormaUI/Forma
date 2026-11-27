namespace Forma.Framework.Themes.Blazor.Server;

using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Varies a host's output cache by the theme choice a page is drawn in.</summary>
public static class ThemeOutputCacheExtensions
{
    /// <summary>Caches a page once per theme choice it is drawn in, since the choice is written into the page.</summary>
    /// <remarks>
    /// The variation is the request's choice where <c>UseFormaThemes</c> resolved it before the cache, and otherwise the choice
    /// the browser's record names, resolved against the registered themes, or the default. A page a host caches is one for
    /// readers who aren't signed in, whose choice no store holds.
    /// </remarks>
    /// <param name="builder">The output cache policy.</param>
    /// <returns>The same policy.</returns>
    public static OutputCachePolicyBuilder VaryByFormaTheme(this OutputCachePolicyBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.VaryByValue(static context =>
        {
            var choice = context.GetFormaTheme()?.Choice ?? ResolveRecorded(context.RequestServices.GetRequiredService<ThemeRegistry>(), ThemeRecord.Read(context.Request));

            return new KeyValuePair<string, string>("forma-theme", $"{choice.Theme}/{ThemeAttributes.GetSchemeValue(choice.Scheme)}");
        });
    }

    private static ThemeChoice ResolveRecorded(ThemeRegistry registry, ThemeRecord? record)
        => record is null ? registry.Default : registry.Resolve(record.Choice);
}
