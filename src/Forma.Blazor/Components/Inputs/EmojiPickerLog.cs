namespace Forma.Blazor.Components.Inputs;

using Microsoft.Extensions.Logging;

/// <summary>The messages the emoji picker writes for the failures it absorbs.</summary>
internal static partial class EmojiPickerLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Loading sprite sheet {Sheet} threw for {RuntimeId}.")]
    public static partial void SheetLoadThrew(ILogger logger, Exception exception, string sheet, Guid runtimeId);
}
