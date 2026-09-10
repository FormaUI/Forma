namespace Forma.Blazor.Components.Symbols;

using Forma.Framework.Css;
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an emoji as inline artwork, drawing it from a host's chosen provider.</summary>
/// <param name="emojiProvider">The registered provider: the host's own, or the reader's font when it registered none.</param>
public sealed partial class FormaEmoji(IEmojiProvider emojiProvider) : FormaElementBase
{
    private EmojiArtwork _artwork = default!;

    private string? _requestedSheet;

    /// <summary>The emoji to draw.</summary>
    [Parameter, EditorRequired] public Emoji Emoji { get; set; }

    /// <summary>The accessible name announced for this emoji, or null when it carries none of its own.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The provider supplying this emoji's artwork, taken from the surrounding subtree or the registered provider when unset.</summary>
    [Parameter] public IEmojiProvider? Provider { get; set; }

    [Inject] private EmojiSpriteLoader SpriteLoader { get; set; } = default!;

    [CascadingParameter] private IEmojiProvider? CascadingProvider { get; set; }

    private string? Role
        => Label is null ? null : "img";

    private string? AriaHidden
        => Label is null ? "true" : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Nearest wins: this emoji's own provider, then the one a surrounding subtree draws from, then the
        // registered provider.
        var provider = Provider
            ?? CascadingProvider
            ?? emojiProvider;

        _artwork = provider.GetArtwork(Emoji);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_artwork is not EmojiSprite sprite || string.Equals(_requestedSheet, sprite.Sheet, StringComparison.Ordinal))
        {
            return;
        }

        // A sprite draws from a symbol adopted into the document, which the reference alone never fetches.
        // The symbol resolves the reference already rendered above it, so nothing has to re-render here.
        _requestedSheet = sprite.Sheet;

        if (!await SpriteLoader.LoadAsync(sprite.Sheet))
        {
            _requestedSheet = null;
        }
    }

    private static string GetSpriteHref(EmojiSprite sprite)
        => $"#{sprite.Symbol}";

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-emoji");
}
