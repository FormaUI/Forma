namespace Forma.Blazor.Components.DataView;

using Microsoft.Extensions.Logging;

/// <summary>The messages the data view logs.</summary>
internal static partial class DataViewLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The range a selection gesture spanned was lost on its way to {RuntimeId} and not selected.")]
    public static partial void RangeLost(ILogger logger, Exception exception, Guid runtimeId);
}
