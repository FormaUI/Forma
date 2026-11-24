namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Microsoft.Extensions.Logging;

/// <summary>The messages the rich text editor logs.</summary>
internal static partial class RichTextEditorLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "What the {Kind} the reader reopened holds was lost on its way to {RuntimeId}, and its dialog stayed closed.")]
    public static partial void ActivationLost(ILogger logger, Exception exception, string kind, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "What the browser sent {RuntimeId} for the {Kind} the reader reopened isn't what its dialog takes, and its dialog stayed closed.")]
    public static partial void ActivationRefused(ILogger logger, Exception? exception, string kind, Guid runtimeId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "A selection the browser reported to {RuntimeId} carries no list of styles or a style that is none, and was refused.")]
    public static partial void SelectionRefused(ILogger logger, Guid runtimeId);
}
