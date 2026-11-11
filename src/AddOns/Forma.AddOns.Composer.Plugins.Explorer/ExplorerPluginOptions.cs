namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Framework.Composer.Editing;

/// <summary>Options configuring what the explorer plugin browses, takes in, and inserts.</summary>
public sealed record ExplorerPluginOptions
{
    /// <summary>The kinds of file listed and chosen, as <c>globs:*.jpg;*.pdf</c> or <c>contentType:image;video</c>, where null lists every file.</summary>
    public string? AcceptedResourceKinds { get; set; }

    /// <summary>The kinds of resource that may be uploaded, in the same syntax, where null accepts the listed kinds.</summary>
    public string? AcceptedUploadKinds { get; set; }

    /// <summary>Whether the dialog allows uploading new files.</summary>
    public bool AllowUpload { get; set; } = true;

    /// <summary>The mode a chosen file the browser can render is inserted in.</summary>
    public MediaInsertMode InsertMode { get; set; } = MediaInsertMode.Embed;

    /// <summary>Default options for the explorer plugin.</summary>
    public static ExplorerPluginOptions Default => new();
}
