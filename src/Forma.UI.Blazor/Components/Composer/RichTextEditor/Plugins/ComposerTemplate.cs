namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>A predefined document template available for insertion.</summary>
/// <param name="Id">The unique identifier of the template.</param>
/// <param name="Title">The localized or readable title of the template.</param>
/// <param name="Description">A brief description of what the template contains.</param>
/// <param name="Html">The HTML fragment or document to insert.</param>
public sealed record ComposerTemplate(
    string Id,
    string Title,
    string Description,
    string Html);
