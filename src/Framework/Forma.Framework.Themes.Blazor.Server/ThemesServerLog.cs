namespace Forma.Framework.Themes.Blazor.Server;

using Microsoft.Extensions.Logging;

/// <summary>Every message the server's half of the theme system writes.</summary>
internal static partial class ThemesServerLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "The theme preference store failed to read the reader's preference, so the request starts in the browser's record or the default.")]
    public static partial void StoreReadFailed(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The theme preference store failed to keep the reader's choice of {Theme}, so the browser's record holds it beside the store's earlier revision.")]
    public static partial void StoreWriteFailed(ILogger logger, string theme, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A theme switch posted no theme the application registers, or no scheme preference, and was refused.")]
    public static partial void SwitchRefused(ILogger logger);
}
