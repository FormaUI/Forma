namespace Forma.Blazor.Components.Virtualization;

using Microsoft.Extensions.Logging;

/// <summary>The messages the virtualizer writes for what it refuses.</summary>
internal static partial class VirtualizeLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A viewport the browser reported to {RuntimeId} stands at no place a list can, and was refused.")]
    public static partial void ViewportRefused(ILogger logger, Guid runtimeId);
}
