namespace Forma.UI.Features.Media.Providers;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>Embeds a Facebook video through Facebook's own player script.</summary>
public sealed partial class FacebookPlayerProvider : IPlayerProvider
{
    private const string EmbedScript = "https://connect.facebook.net/en_US/sdk.js#xfbml=1&version=v19.0";

    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        => ValueTask.FromResult<PlayerFrame?>(BuildFrame(source));

    private static HtmlPlayerFrame? BuildFrame(string source)
    {
        var match = AddressPattern().Match(source);

        if (!match.Success)
        {
            return null;
        }

        // The address reaches the markup, so it is encoded rather than trusted.
        var address = WebUtility.HtmlEncode(match.Value);
        var html = $"""<div class="fb-video" data-href="{address}" data-allowfullscreen="true" data-width="auto"></div>""";

        return new HtmlPlayerFrame(match.Value, html, EmbedScript);
    }

    [GeneratedRegex(
        @"^https?://(?:(?:www\.|web\.|m\.)?facebook\.com/(?:[A-Za-z0-9_.-]+/videos/\d+|watch/?\?v=\d+|reel/\d+)|fb\.watch/[A-Za-z0-9_-]+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
