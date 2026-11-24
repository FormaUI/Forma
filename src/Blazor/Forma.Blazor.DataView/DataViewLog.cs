namespace Forma.Blazor.Components.DataView;

using Microsoft.Extensions.Logging;

/// <summary>The messages the data view logs.</summary>
internal static partial class DataViewLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The range a selection gesture spanned was lost on its way to {RuntimeId} and not selected.")]
    public static partial void RangeLost(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A page appended to {RuntimeId} failed to load and is offered again.")]
    public static partial void AppendFailed(ILogger logger, Exception exception, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A selection gesture the browser sent {RuntimeId} carries no range, or reaches a position below the first, and was refused.")]
    public static partial void RangeRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A move the browser sent {RuntimeId} names no way to select, or an anchor below the first position, and moves the focus alone.")]
    public static partial void SeekRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A column count the browser reported to {RuntimeId} isn't one cards can be laid out in, and was refused.")]
    public static partial void ColumnsRefused(ILogger logger, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A column fit the browser reported to {RuntimeId} names no column or carries a width no table or column can take, and was refused.")]
    public static partial void ColumnFitRefused(ILogger logger, Guid runtimeId);
}
