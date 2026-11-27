namespace Forma.Framework.Coordination.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages a switch coordinator registration writes for the failures it absorbs, each naming its setting.</summary>
internal static partial class CoordinationBrowserInteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} on the {Setting} coordinator registration of {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, string setting, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The call to {Identifier} on the {Setting} coordinator registration of {RuntimeId} was canceled.")]
    public static partial void CallCanceled(ILogger logger, string identifier, string setting, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} on the {Setting} coordinator registration of {RuntimeId} threw.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, string setting, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "The {Setting} state of {RuntimeId} couldn't register with the page's {Setting} coordinator.")]
    public static partial void RegistrationFailed(ILogger logger, Exception exception, string setting, Guid runtimeId);
}
