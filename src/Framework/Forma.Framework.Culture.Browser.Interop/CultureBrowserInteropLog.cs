namespace Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages a culture coordinator registration writes for the failures it absorbs.</summary>
internal static partial class CultureBrowserInteropLog
{
    [LoggerMessage(Level = LogLevel.Error, Message = "A subscriber of the culture state of {RuntimeId} failed as it was notified of a change of its cultures.")]
    public static partial void NotificationFailed(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "The application's culture provider couldn't give the culture state of {RuntimeId} the cultures the page's culture coordinator delivered.")]
    public static partial void CultureNotProvided(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "The destinations of {RuntimeId} couldn't name where the page leads in the culture route a switch goes to, so the switch goes to the page's own address there.")]
    public static partial void DestinationNotResolved(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A pair the browser delivered to the culture state of {RuntimeId} names no culture or no place in its document's sequence, and was refused.")]
    public static partial void PairRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A pair the browser delivered to the culture state of {RuntimeId} names a culture outside the application's cultures, and was refused before the culture provider was asked for it.")]
    public static partial void CultureRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A destination the browser asked the destinations of {RuntimeId} for names no address or no route, and was refused.")]
    public static partial void DestinationRefused(ILogger logger, Guid runtimeId);
}
