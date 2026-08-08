namespace Forma.Blazor.Components.Media;

using Forma.Blazor.Components.Media.Providers;
using Forma.Blazor.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

/// <summary>A player for a file the browser can fetch, or a video a hosting service embeds.</summary>
public sealed partial class FormaVideoPlayer : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.Blazor/Components/Media/FormaVideoPlayer.razor.js";

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

    /// <summary>Caption files offered for a file the browser plays; a hosting service serves its own.</summary>
    [Parameter] public IReadOnlyList<VideoCaptionTrack>? Captions { get; set; }

    /// <summary>Additional attributes applied to the wrapping element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocalizer<FormaVideoPlayer> L { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private JsModule _module = default!;

    private ElementReference _video;

    private bool _autoplayMuted;

    private string? _activatedSource;

    private PlayerFrame? Frame { get; set; }

    private string CssClass
    {
        get
        {
            // Merged rather than left to the splat, which the explicit class attribute would override.
            var hostClass = AdditionalAttributes is not null && AdditionalAttributes.TryGetValue("class", out var value)
                ? value?.ToString()
                : null;

            // A service that sizes its own player is left to do so rather than cropped to the frame's ratio.
            var classes = Frame is HtmlPlayerFrame ? "forma-video-player forma-video-player-flow" : "forma-video-player";

            return string.IsNullOrWhiteSpace(hostClass) ? classes : $"{classes} {hostClass}";
        }
    }

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    protected override async Task OnParametersSetAsync()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Source);
        ArgumentException.ThrowIfNullOrWhiteSpace(Title);

        Frame = await ResolveFrameAsync();

        // A frame of another kind takes the video element with it, so the next one has to be muted again.
        if (Frame is not NativePlayerFrame)
        {
            _autoplayMuted = false;
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

        foreach (var provider in Providers)
        {
            if (await provider.GetPlayerFrameAsync(Source, options, CancellationToken.None) is { } frame)
            {
                return frame;
            }
        }

        return null;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Autoplay && !_autoplayMuted && Frame is NativePlayerFrame)
        {
            _autoplayMuted = await _module.TryInvokeVoidAsync("mute", _video);
        }

        // Keyed by address rather than script, because a new video needs the loaded script to scan again.
        if (Frame is not HtmlPlayerFrame markup || _activatedSource == markup.Source)
        {
            return;
        }

        if (await _module.TryInvokeVoidAsync("activate", markup.ScriptUrl))
        {
            _activatedSource = markup.Source;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _module.DisposeAsync();
}
