namespace Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages a culture coordinator registration writes for the failures it absorbs.</summary>
internal static partial class CultureBrowserInteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} on the culture coordinator registration of {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} on the culture coordinator registration of {RuntimeId} threw.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The culture state of {RuntimeId} couldn't register with the page's culture coordinator.")]
    public static partial void RegistrationFailed(ILogger logger, Exception exception, Guid runtimeId);
}
