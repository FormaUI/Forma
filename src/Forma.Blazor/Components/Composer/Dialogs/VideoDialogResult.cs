namespace Forma.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the video embed dialog.</summary>
/// <param name="Source">The address of the video to embed.</param>
/// <param name="Title">The accessible title or caption for the video.</param>
/// <param name="Autoplay">Whether the video begins playing automatically when loaded.</param>
public sealed record VideoDialogResult(
    string Source,
    string? Title,
    bool Autoplay) : ComposerDialogResult;
