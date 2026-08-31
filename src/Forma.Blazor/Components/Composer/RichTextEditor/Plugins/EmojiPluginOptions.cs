namespace Forma.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring the emoji picker.</summary>
public sealed record EmojiPluginOptions
{
    /// <summary>How many recently chosen emoji the picker keeps.</summary>
    public int MaxRecent { get; init; } = 24;

    /// <summary>Default options for the emoji plugin.</summary>
    public static EmojiPluginOptions Default { get; } = new();
}
