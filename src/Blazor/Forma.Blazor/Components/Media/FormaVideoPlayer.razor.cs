namespace Forma.Blazor.Components.Media;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;
using Forma.Framework.Media;
using Forma.Framework.Media.Providers;

using Microsoft.AspNetCore.Components;

/// <summary>A player for a file the browser can fetch, or a video a hosting service embeds.</summary>
public sealed partial class FormaVideoPlayer : FormaElementBase
{
    // The file provider accepts any fetchable address, so it stays last.
    private static readonly IPlayerProvider[] Providers =
    [
        new YouTubePlayerProvider(),
        new VimeoPlayerProvider(),
        new TwitchPlayerProvider(),
        new TikTokPlayerProvider(),
        new InstagramPlayerProvider(),
        new FacebookPlayerProvider(),
        new TwitterPlayerProvider(),
        new FilePlayerProvider(),
    ];

    /// <summary>The source address of the video, a file or a page on a hosting service.</summary>
    [Parameter, EditorRequired] public string Source { get; set; } = default!;

    /// <summary>The title of the video, the player's accessible name.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>Whether playback starts on load, muted.</summary>
    [Parameter] public bool Autoplay { get; set; }

    /// <summary>Whether a hosting service's player loads only once the reader presses play.</summary>
    [Parameter] public bool DeferLoading { get; set; } = true;

    /// <summary>The address of the still shown before the player loads.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The caption tracks offered for a file the browser plays.</summary>
    [Parameter] public IReadOnlyList<VideoCaptionTrack>? Captions { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>The player providers the host registered.</summary>
    [Inject] private IEnumerable<IPlayerProvider> RegisteredProviders { get; set; } = default!;

    private Playback _playback = default!;

    private Embedding _embedding = default!;

    private ElementReference _videoElement;

    private bool _autoplayMuted;

    private string? _resolvedSource;

    private bool _resolvedAutoplay;

    private bool _isActivated;

    private string? _activatedSource;

    private PlayerFrame? Frame { get; set; }

    private string LabelId
        => $"{ElementId}-label";

    private string NoteId
        => $"{ElementId}-note";

    private bool ShowFacade
        => DeferLoading && !Autoplay && !_isActivated && Frame is EmbeddedPlayerFrame or HtmlPlayerFrame;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _playback = new Playback(Context.JS);
        _embedding = new Embedding(Context.JS);
    }

    /// <inheritdoc />
    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        // The providers decide by address and autoplay alone.
        if (Source != _resolvedSource || Autoplay != _resolvedAutoplay)
        {
            var previousFrame = Frame;

            Frame = await ResolveFrameAsync();

            // Recorded after resolution, so a provider that threw is asked again.
            _resolvedSource = Source;
            _resolvedAutoplay = Autoplay;

            // Each video is activated on its own.
            if (previousFrame?.Source != Frame?.Source)
            {
                _isActivated = false;
            }

            // A frame of another kind discards the video element, so the next one is muted again.
            if (Frame is not NativePlayerFrame)
            {
                _autoplayMuted = false;
            }
        }

        if (Captions is { Count: > 0 } && Frame is EmbeddedPlayerFrame or HtmlPlayerFrame)
        {
            throw new InvalidOperationException(
                "Captions render only for a file the browser plays. A hosting service serves its own, so supplying them here would drop them silently.");
        }
    }

    private async Task<PlayerFrame?> ResolveFrameAsync()
    {
        var options = new PlayerOptions
        {
            Autoplay = Autoplay,
            PageAddress = Uri.TryCreate(Navigation.BaseUri, UriKind.Absolute, out var pageAddress) ? pageAddress : null,
        };

        // A host provider comes first, so it can claim any address.
        foreach (var provider in RegisteredProviders.Concat(Providers))
        {
            if (await provider.GetPlayerFrameAsync(Source, options, CancellationToken.None) is { } frame)
            {
                return frame;
            }
        }

        return null;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Autoplay && !_autoplayMuted && Frame is NativePlayerFrame)
        {
            _autoplayMuted = await _playback.MuteAsync(_videoElement);
        }

        // Keyed by address, since each new video needs the loaded script to scan again.
        if (ShowFacade || Frame is not HtmlPlayerFrame markup || _activatedSource == markup.Source)
        {
            return;
        }

        if (await _embedding.ActivateAsync(markup.ScriptUrl))
        {
            _activatedSource = markup.Source;
        }
    }

    private void Activate()
        => _isActivated = true;

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-video-player")
            // A service's HTML player keeps the size the service gives it.
            .Add("forma-video-player-flow", Frame is HtmlPlayerFrame && !ShowFacade);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _playback.DisposeAsync();
        await _embedding.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
