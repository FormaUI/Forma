namespace Forma.Framework.Core.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The messages an interop wrapper writes for the failures it absorbs.</summary>
internal static partial class InteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} in {Module} for {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} in {Module} threw for {RuntimeId}.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while releasing {Module} for {RuntimeId}.")]
    public static partial void CircuitDroppedWhileReleasing(ILogger logger, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Module} could not be imported for {RuntimeId}.")]
    public static partial void ImportFailed(ILogger logger, Exception exception, string module, Guid runtimeId);
}
