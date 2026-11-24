namespace Forma.Blazor.Components.Explorer;

using Microsoft.Extensions.Logging;

/// <summary>The messages the explorer writes for what it refuses.</summary>
internal static partial class ExplorerLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A layout the browser reported to {RuntimeId} isn't one the header's stylesheet names, and was refused.")]
    public static partial void LayoutRefused(ILogger logger, Guid runtimeId);
}
