namespace Forma.Blazor.Components.Inputs;

using Microsoft.Extensions.Logging;

/// <summary>The messages the slider writes for what it refuses.</summary>
internal static partial class SliderLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "A value the browser sent {RuntimeId} isn't a number between the slider's bounds, and was refused.")]
    public static partial void ValueRefused(ILogger logger, Guid runtimeId);
}
