namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring media insertion in rich text documents.</summary>
public sealed record MediaPluginOptions
{
    /// <summary>Whether uploading new images directly from the dialog is enabled.</summary>
    public bool AllowUpload { get; init; } = true;

    /// <summary>Whether browsing existing stored media is enabled.</summary>
    public bool AllowLibrary { get; init; } = true;

    /// <summary>Default options for the media plugin.</summary>
    public static MediaPluginOptions Default { get; } = new();
}
