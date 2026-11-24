namespace Forma.Blazor.Components.Collections;

using Microsoft.Extensions.Logging;

/// <summary>The messages the sortable list writes for what it refuses.</summary>
internal static partial class SortableListLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A move the browser reported to {RuntimeId} names a place outside the list, and was refused.")]
    public static partial void ReorderRefused(ILogger logger, Guid runtimeId);
}
