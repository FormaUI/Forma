namespace Forma.Blazor.Components.Symbols;

using Forma.Blazor.Features.Emojis;
using Forma.Framework.Css;
using Forma.Framework.Emojis;
using Forma.Framework.Emojis.Unicode;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an emoji as inline artwork, drawing it from a host's chosen provider.</summary>
/// <param name="emojiProvider">The registered emoji provider, where the reader's font draws an emoji none is registered for.</param>
public sealed partial class FormaEmoji(IEmojiProvider? emojiProvider = null) : FormaElementBase
{
    private EmojiArtwork _artwork = default!;

    private string? _requestedSheet;

    /// <summary>The emoji to draw.</summary>
    [Parameter, EditorRequired] public Emoji Emoji { get; set; }

    /// <summary>The emoji's accessible name, where null hides the emoji from assistive technology.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The provider supplying this emoji's artwork, where null takes the surrounding subtree's provider or the registered one.</summary>
    [Parameter] public IEmojiProvider? Provider { get; set; }

    [Inject] private IEmojiSpriteLoader SpriteLoader { get; set; } = default!;

    [CascadingParameter] private IEmojiProvider? CascadingProvider { get; set; }

    private string? Role
        => Label is null ? null : "img";

    private string? AriaHidden
        => Label is null ? "true" : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Nearest wins: this emoji's own provider, then the surrounding subtree's, then the registered one.
        var provider = Provider
            ?? CascadingProvider
            ?? emojiProvider
            ?? UnicodeEmojiProvider.Instance;

        _artwork = provider.GetArtwork(Emoji);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_artwork is not EmojiSprite sprite || string.Equals(_requestedSheet, sprite.Sheet, StringComparison.Ordinal))
        {
            return;
        }

        // The sheet's symbols are adopted into the document, where the rendered reference resolves them without a re-render.
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
