namespace Forma.UI.Blazor.Components.Composer.RichTextEditor.Plugins;

/// <summary>Options configuring the emoji palette.</summary>
public sealed record EmojiPluginOptions
{
    /// <summary>The emojis available in the palette dropdown.</summary>
    public IReadOnlyList<string> Emojis { get; init; } =
    [
        "😀", "😂", "😊", "😍", "🤔", "😎", "👍", "👎", "👏", "🙌",
        "❤️", "🎉", "🔥", "⭐", "💡", "⚠️", "📌", "🚀", "💬", "🚲",
        "🚴", "🏆", "🥇", "⏱️", "📅", "✨", "💯", "✅"
    ];

    /// <summary>Default options for the emoji plugin.</summary>
    public static EmojiPluginOptions Default { get; } = new();
}
