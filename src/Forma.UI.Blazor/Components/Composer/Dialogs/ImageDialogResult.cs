namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the image dialog.</summary>
/// <param name="Source">The image source URL.</param>
/// <param name="AltText">The accessible alternative text describing the image.</param>
/// <param name="Caption">The optional caption rendered in a figcaption element.</param>
public sealed record ImageDialogResult(
    string Source,
    string? AltText,
    string? Caption);
