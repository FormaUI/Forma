namespace Forma.Blazor.Components.Design;

using Forma.Blazor.Design;
using Forma.Framework.Emojis;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>Renders an emoji as inline artwork, drawing it from a host's chosen provider.</summary>
public sealed partial class FormaEmoji : ComponentBase, IAsyncDisposable
{
    private EmojiArtwork _artwork = default!;

    private EmojiSpriteLoader? _ownLoader;

    private EmojiSpriteLoader? _resolvedLoader;

    private string? _requestedSheet;

    /// <summary>The emoji to draw.</summary>
    [Parameter, EditorRequired] public Emoji Emoji { get; set; }

    /// <summary>The accessible name announced for this emoji, or null when it carries none of its own.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>Extra classes applied to the root element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The provider supplying this emoji's artwork, taken from the surrounding subtree or the host's registration when unset.</summary>
    [Parameter] public IEmojiProvider? Provider { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [CascadingParameter] private EmojiSpriteLoader? SpriteLoader { get; set; }

    [CascadingParameter] private IEmojiProvider? CascadingProvider { get; set; }

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-emoji" : $"forma-emoji {Class}";

    private string? Role
        => Label is null ? null : "img";

    private string? AriaHidden
        => Label is null ? "true" : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Nearest wins: this emoji's own provider, then the one a surrounding subtree draws from, then the host's
        // registration. Absent all three is a host that wants no artwork package, not a fault, and the reader's
        // own font stands in.
        var provider = Provider
            ?? CascadingProvider
            ?? (IEmojiProvider?)Services.GetService(typeof(IEmojiProvider))
            ?? UnicodeEmojiProvider.Default;

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

        if (!await GetSpriteLoader().LoadAsync(sprite.Sheet))
        {
            _requestedSheet = null;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_ownLoader is not null)
        {
            await _ownLoader.DisposeAsync();
        }
    }

    // A sheet is fetched once however many emoji draw from it: a surface rendering many hands its own loader
    // down, and the registered one covers the rest of the circuit. Only an emoji offered neither loads alone.
    private EmojiSpriteLoader GetSpriteLoader()
        => SpriteLoader
            ?? (_resolvedLoader ??= (EmojiSpriteLoader?)Services.GetService(typeof(EmojiSpriteLoader)) ?? (_ownLoader = new EmojiSpriteLoader(JS)));

    private static string SpriteHref(EmojiSprite sprite)
        => $"#{sprite.Symbol}";
}
