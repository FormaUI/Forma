namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the embed dialog.</summary>
/// <param name="EmbedType">The category or kind of embed (e.g. form, poll, widget).</param>
/// <param name="Identifier">The identifier or source path of the embedded resource.</param>
/// <param name="Settings">Optional JSON or configuration string for the embed.</param>
public sealed record EmbedDialogResult(
    string EmbedType,
    string Identifier,
    string? Settings);
