namespace Forma.Blazor.Components.Layout;

using Microsoft.Extensions.Logging;

/// <summary>The messages the splitter writes for what it refuses.</summary>
internal static partial class SplitterLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A drag the browser reported to {RuntimeId} traveled past any number of pixels, and was refused.")]
    public static partial void TravelRefused(ILogger logger, Guid runtimeId);
}
