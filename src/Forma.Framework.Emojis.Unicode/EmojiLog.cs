namespace Forma.Framework.Emojis.Unicode;

using Microsoft.Extensions.Logging;

/// <summary>The messages the emoji services write for the failures they absorb.</summary>
internal static partial class EmojiLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Sprite sheet {Sheet} did not load for {RuntimeId}.")]
    public static partial void SpriteSheetDidNotLoad(ILogger logger, string sheet, Guid runtimeId);
}
