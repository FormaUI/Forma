namespace Forma.Framework.Themes.Blazor.Server;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>The switch a statically rendered theme picker posts: the store written first, then the browser's record, then the reader sent back.</summary>
internal static class ThemeSwitchEndpoint
{
    /// <summary>Keeps the posted choice and sends the reader back to the page, or refuses a form naming no registered theme or no scheme preference.</summary>
    public static async Task<IResult> SwitchAsync(HttpContext context, IFormCollection form)
    {
        var registry = context.RequestServices.GetRequiredService<ThemeRegistry>();
        var theme = form["theme"].ToString();

        if (string.IsNullOrWhiteSpace(theme) || !registry.TryGetTheme(theme, out _) || !ThemeAttributes.TryParseScheme(form["scheme"], out var scheme))
        {
            ThemesServerLog.SwitchRefused(RequestThemeResolution.GetLogger(context));

            return Results.BadRequest();
        }

        var choice = new ThemeChoice(theme, scheme);
        var record = ThemeRecord.Read(context.Request);
        var stored = await RequestThemeResolution.ReadStoreAsync(context).ConfigureAwait(false);
        var latest = Math.Max(record?.Revision ?? 0, stored?.Revision ?? 0);
        var revision = await WriteStoreAsync(context, new ThemePreference(choice, latest + 1)).ConfigureAwait(false) ? latest + 1 : latest;

        new ThemeRecord(choice, revision).Write(context);

        return Results.Redirect(GetReturnAddress(form["returnUrl"]));
    }

    // Whether the host's store kept the preference; a host that registered no store keeps the revision the record advances to.
    private static async Task<bool> WriteStoreAsync(HttpContext context, ThemePreference preference)
    {
        if (context.RequestServices.GetService<IThemePreferenceStore>() is not { } store)
        {
            return true;
        }

        try
        {
            await store.WriteAsync(preference, context.RequestAborted).ConfigureAwait(false);

            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ThemesServerLog.StoreWriteFailed(RequestThemeResolution.GetLogger(context), preference.Choice.Theme, exception);

            return false;
        }
    }

    // An address of the application's own, so the endpoint never sends a reader elsewhere; the root where the form gives none.
    private static string GetReturnAddress(string? address)
        => address is { Length: > 0 } && address[0] == '/' && (address.Length == 1 || (address[1] != '/' && address[1] != '\\'))
            ? address
            : "/";
}
