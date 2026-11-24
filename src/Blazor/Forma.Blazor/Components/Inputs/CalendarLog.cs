namespace Forma.Blazor.Components.Inputs;

using Microsoft.Extensions.Logging;

/// <summary>The messages the calendar writes for what it refuses.</summary>
internal static partial class CalendarLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A year or month the browser chose in {RuntimeId}'s dropdowns isn't one they offer, and was refused.")]
    public static partial void SelectionRefused(ILogger logger, Guid runtimeId);
}
