namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Blazor.Components.Files;
using Forma.Framework.Files;

/// <summary>Options configuring which files may be attached to a document.</summary>
public sealed record FileUploadPluginOptions
{
    /// <summary>The files the dialog's picker accepts.</summary>
    public FilePickerOptions Picker { get; set; } = FilePickerOptions.Default;

    /// <summary>Default options for the file upload plugin.</summary>
    public static FileUploadPluginOptions Default => new();
}
