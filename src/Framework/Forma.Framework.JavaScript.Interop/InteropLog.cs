namespace Forma.Framework.JavaScript.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The log messages for interop failures at import, call and release.</summary>
internal static partial class InteropLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while importing {Module} for {RuntimeId}.")]
    public static partial void CircuitDroppedWhileImporting(ILogger logger, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Importing {Module} was canceled for {RuntimeId}.")]
    public static partial void ImportCanceled(ILogger logger, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Module} could not be imported for {RuntimeId}.")]
    public static partial void ImportFailed(ILogger logger, Exception exception, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while calling {Identifier} in {Module} for {RuntimeId}.")]
    public static partial void CircuitDroppedWhileCalling(ILogger logger, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The call to {Identifier} in {Module} was canceled for {RuntimeId}.")]
    public static partial void CallCanceled(ILogger logger, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Identifier} in {Module} threw for {RuntimeId}.")]
    public static partial void ScriptThrewWhileCalling(ILogger logger, Exception exception, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "What {Identifier} in {Module} answered was lost on its way to {RuntimeId}.")]
    public static partial void AnswerLost(ILogger logger, Exception exception, string identifier, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while releasing {Module} for {RuntimeId}.")]
    public static partial void CircuitDroppedWhileReleasing(ILogger logger, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Releasing {Module} was canceled for {RuntimeId}.")]
    public static partial void ReleaseCanceled(ILogger logger, string module, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Module} threw while being released for {RuntimeId}.")]
    public static partial void ScriptThrewWhileReleasing(ILogger logger, Exception exception, string module, Guid runtimeId);
}
