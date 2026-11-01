namespace Forma.Framework.Input.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages an input scope writes for the failures it absorbs.</summary>
internal static partial class InputInteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} on the input scope of {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The call to {Identifier} on the input scope of {RuntimeId} was canceled.")]
    public static partial void CallCanceled(ILogger logger, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} on the input scope of {RuntimeId} threw.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, Guid runtimeId);
}
