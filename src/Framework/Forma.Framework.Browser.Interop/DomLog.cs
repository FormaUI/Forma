namespace Forma.Framework.Browser.Interop;

using Microsoft.Extensions.Logging;

/// <summary>The browser interop's log messages: an entry that could not be reached, a call that failed or was canceled, and a handler that threw.</summary>
internal static partial class DomLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The circuit dropped while {Operation} reached {Root} for {RuntimeId}.")]
    public static partial void CircuitDropped(ILogger logger, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Operation} was canceled in {Root} for {RuntimeId}.")]
    public static partial void CallCanceled(ILogger logger, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{Root} was released before {Operation} reached it for {RuntimeId}.")]
    public static partial void BridgeReleased(ILogger logger, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The state of registration {Token}.{Generation} was lost on its way to {RuntimeId} and not delivered.")]
    public static partial void StateLost(ILogger logger, Exception exception, string token, int generation, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The edit of registration {Token} at revision {Revision} was lost on its way to {RuntimeId}; its field sends its whole text in its place.")]
    public static partial void EditLost(ILogger logger, Exception exception, string token, int revision, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The edit of registration {Token} at revision {Revision} falls outside the text {RuntimeId} holds; its field sends its whole text in its place.")]
    public static partial void EditMisplaced(ILogger logger, string token, int revision, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The edit of registration {Token} at revision {Revision} lengthens the text {RuntimeId} holds past its field's maximum length; its field sends its whole text in its place.")]
    public static partial void EditTooLong(ILogger logger, string token, int revision, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The whole text of registration {Token} at revision {Revision} is longer than its field's maximum length allows {RuntimeId}, and was refused.")]
    public static partial void ReplacementTooLong(ILogger logger, string token, int revision, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The edit of registration {Token} at revision {Revision} stands further ahead of the text {RuntimeId} holds than its field's edits reach; its field sends its whole text in its place.")]
    public static partial void EditTooFarAhead(ILogger logger, string token, int revision, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A {Operation} report the browser sent {RuntimeId} named no registration or carried no value, and was refused.")]
    public static partial void ReportRefused(ILogger logger, string operation, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "The state the browser sent registration {Token}.{Generation} of {RuntimeId} isn't one it measures, and was refused.")]
    public static partial void StateRefused(ILogger logger, Exception? exception, string token, int generation, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Root} could not be imported for {Operation} by {RuntimeId}.")]
    public static partial void ImportFailed(ILogger logger, Exception exception, string root, string operation, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{Operation} failed in {Root} for {RuntimeId}.")]
    public static partial void CallFailed(ILogger logger, Exception exception, string operation, string root, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Error, Message = "The {Capability} handler of {RuntimeId} threw on registration {Token}, generation {Generation}.")]
    public static partial void HandlerFailed(ILogger logger, Exception exception, string capability, string token, int generation, Guid runtimeId);
}
