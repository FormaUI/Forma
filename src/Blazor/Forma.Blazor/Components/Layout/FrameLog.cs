namespace Forma.Blazor.Components.Layout;

using Microsoft.Extensions.Logging;

/// <summary>The messages the frame writes for what it refuses.</summary>
internal static partial class FrameLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A placement the browser reported to {RuntimeId} isn't one a box can take, and was refused.")]
    public static partial void PlacementRefused(ILogger logger, Guid runtimeId);
}
