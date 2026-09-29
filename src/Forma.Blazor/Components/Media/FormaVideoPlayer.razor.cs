namespace Forma.Blazor.Components.Media;

using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;
using Forma.Framework.Media;
using Forma.Framework.Media.Providers;

using Microsoft.AspNetCore.Components;

/// <summary>A player for a file the browser can fetch, or a video a hosting service embeds.</summary>
public sealed partial class FormaVideoPlayer : FormaElementBase
{
    // Ordered: the file provider accepts any fetchable address, so it answers only once the services have declined.
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

    /// <summary>Address of the video, whether a file or a page on a hosting service.</summary>
    [Parameter, EditorRequired] public string Source { get; set; } = default!;

    /// <summary>Accessible name for the player.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = default!;

    /// <summary>Starts playback without interaction, always muted because browsers block unmuted autoplay.</summary>
    [Parameter] public bool Autoplay { get; set; }

    /// <summary>Holds a hosting service's player back until the reader presses play, so nothing loads from it before then.</summary>
    [Parameter] public bool DeferLoading { get; set; } = true;

    /// <summary>Still shown before the player loads, served by the host so holding the player back stays worth doing.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>Caption files offered for a file the browser plays; a hosting service serves its own.</summary>
    [Parameter] public IReadOnlyList<VideoCaptionTrack>? Captions { get; set; }

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    /// <summary>Providers the host registered, tried before the services this library recognizes.</summary>
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

    // Autoplay is a request to start, so it overrides holding the player back.
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

        // The providers decide by address and autoplay alone, so the chain is asked again only when either changes.
        if (Source != _resolvedSource || Autoplay != _resolvedAutoplay)
        {
            var previous = Frame;

            Frame = await ResolveFrameAsync();

            // Recorded only once resolution finished, so a provider that threw is asked again on the next change.
            _resolvedSource = Source;
            _resolvedAutoplay = Autoplay;

            // A different video is a fresh decision to load a third party.
            if (previous?.Source != Frame?.Source)
            {
                _isActivated = false;
            }

            // A frame of another kind takes the video element with it, so the next one has to be muted again.
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

        // A host provider comes first so it can claim an address, including one a built-in service would otherwise take.
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

        // Keyed by address rather than script, because a new video needs the loaded script to scan again.
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
            // A service that sizes its own player is left to do so rather than cropped to the frame's ratio.
            .Add("forma-video-player-flow", Frame is HtmlPlayerFrame && !ShowFacade);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _playback.DisposeAsync();
        await _embedding.DisposeAsync();
        await base.DisposeAsyncCore();
    }
}
