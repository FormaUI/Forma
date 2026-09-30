namespace Forma.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the image dialog.</summary>
/// <param name="Source">The address of the image.</param>
/// <param name="AltText">The alternative text describing the image.</param>
/// <param name="Caption">The caption of the image.</param>
public sealed record ImageDialogResult(
    string Source,
    string? AltText,
    string? Caption) : ComposerDialogResult;
