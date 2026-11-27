namespace Forma.Blazor.Components.Themes;

using Microsoft.Extensions.Logging;

/// <summary>Every message the theme components write.</summary>
internal static partial class ThemesLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "The page refused the theme picker's switch to {Theme}, so the choice in force stays.")]
    public static partial void SwitchRefused(ILogger logger, string theme, Exception exception);
}
