namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The file chosen from the stored files dialog, and how it is to be put into the document.</summary>
/// <param name="Source">Where the file lives.</param>
/// <param name="Name">The file's own name, used as the link's text and the picture's alternative text.</param>
/// <param name="Kind">What the browser can make of it: <c>image</c>, <c>video</c>, <c>audio</c>, or <c>file</c>.</param>
/// <param name="Embed">Whether it is rendered where it is inserted.</param>
/// <param name="Link">Whether a link to it is inserted.</param>
public sealed record ResourceDialogResult(
    string Source,
    string Name,
    string Kind,
    bool Embed,
    bool Link);
