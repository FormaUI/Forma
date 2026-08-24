namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the media dialog.</summary>
/// <param name="Source">The image or media source URL.</param>
/// <param name="AltText">The accessible alternative text describing the image.</param>
/// <param name="Caption">The optional caption rendered in a figcaption element.</param>
/// <param name="Alignment">The optional alignment specifier (left, center, right).</param>
public sealed record MediaDialogResult(
    string Source,
    string? AltText,
    string? Caption,
    string? Alignment);
