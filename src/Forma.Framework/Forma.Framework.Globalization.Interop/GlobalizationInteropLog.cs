namespace Forma.Framework.Globalization.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages a document culture synchronizer writes for the failures it absorbs.</summary>
internal static partial class GlobalizationInteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} on the document culture synchronizer of {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} on the document culture synchronizer of {RuntimeId} threw.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A reconciliation of the document culture synchronizer of {RuntimeId} was cancelled.")]
    public static partial void ReconciliationCancelled(ILogger logger, Exception exception, Guid runtimeId);
}
