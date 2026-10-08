namespace Forma.Framework.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The browser interop's log messages: an entry that could not be reached, a call that failed, and a handler that threw.</summary>
internal static partial class DomLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while {Operation} reached {Root} for {RuntimeId}.")]
    public static partial void CircuitDropped(ILogger logger, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Root} was released before {Operation} reached it for {RuntimeId}.")]
    public static partial void BridgeReleased(ILogger logger, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Root} could not be imported for {Operation} by {RuntimeId}.")]
    public static partial void ImportFailed(ILogger logger, Exception exception, string root, string operation, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Operation} failed in {Root} for {RuntimeId}.")]
    public static partial void CallFailed(ILogger logger, Exception exception, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "The {Capability} handler of {RuntimeId} threw on registration {Token}, generation {Generation}.")]
    public static partial void HandlerFailed(ILogger logger, Exception exception, string capability, string token, int generation, Guid runtimeId);
}
