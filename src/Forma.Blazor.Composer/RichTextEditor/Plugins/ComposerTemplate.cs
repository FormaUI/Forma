namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>A predefined document template available for insertion.</summary>
/// <param name="Id">The unique identifier of the template.</param>
/// <param name="Title">The title of the template.</param>
/// <param name="Description">The description of the template.</param>
/// <param name="Html">The HTML the template inserts.</param>
public sealed record ComposerTemplate(
    string Id,
    string Title,
    string Description,
    string Html);
