namespace Forma.Blazor.Components.Debugging.InputInspector;

using Microsoft.Extensions.Logging;

/// <summary>The messages the input inspector writes for what it refuses.</summary>
internal static partial class InputInspectorLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A decision the browser reported to {RuntimeId} carries no input or stack the inspector can draw, and was refused.")]
    public static partial void DecisionRefused(ILogger logger, Guid runtimeId);
}
