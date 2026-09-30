namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Blazor.Components.Symbols;

/// <summary>Options configuring the emoji plugin.</summary>
public sealed record EmojiPluginOptions
{
    /// <summary>The options of the emoji picker the button opens.</summary>
    public EmojiPickerOptions Picker { get; set; } = EmojiPickerOptions.Default;

    /// <summary>Default options for the emoji plugin.</summary>
    public static EmojiPluginOptions Default => new();
}
