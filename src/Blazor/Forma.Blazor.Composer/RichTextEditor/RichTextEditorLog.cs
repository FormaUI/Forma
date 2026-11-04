namespace Forma.Blazor.Components.Composer.RichTextEditor;

using Microsoft.Extensions.Logging;

/// <summary>The messages the rich text editor logs.</summary>
internal static partial class RichTextEditorLog
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "What the {Kind} the reader reopened holds was lost on its way to {RuntimeId}, and its dialog stayed closed.")]
    public static partial void ActivationLost(ILogger logger, Exception exception, string kind, Guid runtimeId);
}
