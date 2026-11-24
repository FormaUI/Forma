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

    [LoggerMessage(Level = LogLevel.Debug, Message = "An action the browser sent an input scope of {RuntimeId} names no route of the scope's or carries an input that can't be read, and was refused.")]
    public static partial void ActionRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A dismissal the browser sent an input scope of {RuntimeId} names a behavior or a reason the router doesn't have, and was refused.")]
    public static partial void DismissalRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "An action the browser sent the host's routes on {RuntimeId} names no host route or carries an input that can't be read, and was refused.")]
    public static partial void HostActionRefused(ILogger logger, Guid runtimeId);
}
