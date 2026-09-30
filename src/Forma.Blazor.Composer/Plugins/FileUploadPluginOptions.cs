namespace Forma.Blazor.Components.Composer.Plugins;

using Forma.Blazor.Components.Files;
using Forma.Framework.Files;

/// <summary>The options of a <see cref="FileUploadPlugin"/>.</summary>
public sealed record FileUploadPluginOptions
{
    /// <summary>The files the dialog's picker accepts.</summary>
    public FilePickerOptions Picker { get; set; } = FilePickerOptions.Default;

    /// <summary>The default options.</summary>
    public static FileUploadPluginOptions Default => new();
}
