namespace Forma.Blazor.Features.Emojis;

using Microsoft.Extensions.Logging;

/// <summary>The messages the sprite loader writes for the failures it absorbs.</summary>
internal static partial class EmojiSpriteLoaderLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Sprite sheet {Sheet} did not load for {RuntimeId}.")]
    public static partial void SpriteSheetDidNotLoad(ILogger logger, string sheet, Guid runtimeId);
}
