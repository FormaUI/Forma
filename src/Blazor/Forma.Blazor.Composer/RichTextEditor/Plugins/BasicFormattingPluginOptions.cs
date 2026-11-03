namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring the formatting <see cref="BasicFormattingPlugin"/> offers.</summary>
public sealed record BasicFormattingPluginOptions
{
    /// <summary>Whether subscript and superscript are offered.</summary>
    public bool IncludeSubscriptAndSuperscript { get; set; }

    /// <summary>Default options for the basic formatting plugin.</summary>
    public static BasicFormattingPluginOptions Default => new();
}
