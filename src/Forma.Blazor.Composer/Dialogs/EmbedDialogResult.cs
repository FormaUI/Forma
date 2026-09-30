namespace Forma.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the embed dialog.</summary>
/// <param name="EmbedType">The type of the embed, such as a form, a poll or a widget.</param>
/// <param name="Identifier">The identifier of the embedded resource.</param>
/// <param name="Settings">The settings of the embed.</param>
public sealed record EmbedDialogResult(
    string EmbedType,
    string Identifier,
    string? Settings) : ComposerDialogResult;
