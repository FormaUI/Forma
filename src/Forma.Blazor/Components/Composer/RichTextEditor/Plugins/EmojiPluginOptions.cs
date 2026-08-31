namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

using Forma.Blazor.Components.Inputs;

/// <summary>Options configuring the emoji picker.</summary>
public sealed record EmojiPluginOptions
{
    /// <summary>How the picker the button opens browses.</summary>
    public EmojiPickerOptions Picker { get; init; } = EmojiPickerOptions.Default;

    /// <summary>Default options for the emoji plugin.</summary>
    public static EmojiPluginOptions Default { get; } = new();
}
