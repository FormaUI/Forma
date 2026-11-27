namespace Forma.Framework.Themes.Blazor.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

/// <summary>Resolves the theme choice a request starts in, and writes the browser's record where the host's store holds a newer choice.</summary>
/// <remarks>
/// The request starts in the first of these that names a registered theme, as the theme design orders them: the choice the
/// host gives it, which writes nothing; the store's, where its revision is greater than the one the browser's record agreed
/// with or the browser has no record, which writes the record with the store's revision; the browser's record; and the
/// host's default. A choice naming a theme that isn't registered starts in the default theme in its scheme preference.
/// </remarks>
internal static class RequestThemeResolution
{
    /// <summary>Resolves <paramref name="context"/>'s theme and sets it as the request's <see cref="RequestTheme"/> feature.</summary>
    public static async Task<RequestTheme> ResolveAsync(HttpContext context)
    {
        var registry = context.RequestServices.GetRequiredService<ThemeRegistry>();
        var options = context.RequestServices.GetService<ThemeServerOptions>();
        var theme = options?.GivenChoice?.Invoke(context) is { } given
            ? new RequestTheme(registry.Resolve(given), 0, keepsChoice: false)
            : await ResolveKeptAsync(context, registry).ConfigureAwait(false);

        context.Features.Set(theme);

        return theme;
    }

    /// <summary>The theme a request that keeps the reader's choice starts in, from what the store and the browser's record hold.</summary>
    private static async Task<RequestTheme> ResolveKeptAsync(HttpContext context, ThemeRegistry registry)
    {
        var record = ThemeRecord.Read(context.Request);
        var stored = await ReadStoreAsync(context).ConfigureAwait(false);

        if (stored is not null && (record is null || stored.Revision > record.Revision))
        {
            new ThemeRecord(stored.Choice, stored.Revision).Write(context);

            return new RequestTheme(registry.Resolve(stored.Choice), stored.Revision, keepsChoice: true);
        }

        return record is not null
            ? new RequestTheme(registry.Resolve(record.Choice), record.Revision, keepsChoice: true)
            : new RequestTheme(registry.Default, 0, keepsChoice: true);
    }

    /// <summary>The preference the host's store keeps for the reader, where the host registered a store and it read one.</summary>
    public static async Task<ThemePreference?> ReadStoreAsync(HttpContext context)
    {
        if (context.RequestServices.GetService<IThemePreferenceStore>() is not { } store)
        {
            return null;
        }

        try
        {
            return await store.ReadAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThemesServerLog.StoreReadFailed(GetLogger(context), exception);

            return null;
        }
    }

    /// <summary>The logger the server's half writes under.</summary>
    public static ILogger GetLogger(HttpContext context)
        => context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Forma.Framework.Themes.Blazor.Server");
}
