namespace Forma.UI.Blazor.Components;

using Forma.UI.Design.Emojis;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an emoji as inline artwork, drawing it from a host's chosen provider.</summary>
public sealed partial class FormaEmoji : ComponentBase
{
    private EmojiArtwork _artwork = default!;

    /// <summary>The emoji to draw.</summary>
    [Parameter, EditorRequired] public Emoji Emoji { get; set; }

    /// <summary>The accessible name announced for this emoji, or null when it carries none of its own.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IEmojiProvider Provider { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-emoji" : $"forma-emoji {Class}";

    private string? Role
        => Label is null ? null : "img";

    private string? AriaHidden
        => Label is null ? "true" : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _artwork = Provider.GetArtwork(Emoji);

    private static string SpriteHref(EmojiSprite sprite)
        => $"#{sprite.Symbol}";
}
