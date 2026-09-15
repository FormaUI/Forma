namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Framework.Composer.Editing;

/// <summary>Options configuring what the explorer plugin browses, takes in, and inserts.</summary>
public sealed record ExplorerPluginOptions
{
    /// <summary>Which resources are listed, as <c>globs:*.jpg;*.pdf</c> or <c>contentType:image;video</c>; everything when null.</summary>
    public string? AcceptedResourceKinds { get; init; }

    /// <summary>Which resources may be added, in the same syntax, falling back to what is listed when null.</summary>
    public string? AcceptedUploadKinds { get; init; }

    /// <summary>Whether new files may be added to the library from the dialog.</summary>
    public bool AllowUpload { get; init; } = true;

    /// <summary>How a chosen file the browser can render is put into the document.</summary>
    public MediaInsertMode InsertMode { get; init; } = MediaInsertMode.Embed;

    /// <summary>Default options for the explorer plugin.</summary>
    public static ExplorerPluginOptions Default { get; } = new();
}
