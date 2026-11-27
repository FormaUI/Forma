namespace Forma.Framework.Themes.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>Every message the theme coordinator's registrations write.</summary>
internal static partial class ThemesBrowserInteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The theme coordinator delivered a choice naming no registered theme, no scheme preference or no place in its document's sequence to {RuntimeId}, which was refused.")]
    public static partial void ChoiceRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The theme coordinator asked {RuntimeId} to keep a choice naming no registered theme or no scheme preference, or a runtime without a store, which was refused.")]
    public static partial void WriteRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The theme preference store failed to keep the reader's choice for {RuntimeId}; the choice stays accepted on the page and in the browser's record.")]
    public static partial void StoreWriteFailed(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "A subscriber of {RuntimeId}'s theme state failed as it heard of the accepted choice.")]
    public static partial void NotificationFailed(ILogger logger, Exception exception, Guid runtimeId);
}
