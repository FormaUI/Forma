namespace Forma.UI.Features.Media.Providers;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>Embeds an Instagram post, reel or video through Instagram's own player script.</summary>
public sealed partial class InstagramPlayerProvider : IPlayerProvider
{
    private const string EmbedScript = "https://www.instagram.com/embed.js";

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

        // A permalink addresses a reel in the singular, and Instagram's script matches it literally.
        var kind = match.Groups["kind"].Value;
        var singular = kind.Equals("reels", StringComparison.OrdinalIgnoreCase) ? "reel" : kind;

        // The permalink reaches the markup, so it is encoded rather than trusted.
        var permalink = WebUtility.HtmlEncode($"https://www.instagram.com/{singular}/{match.Groups["shortcode"].Value}/");
        var html = $"""<blockquote class="instagram-media" data-instgrm-permalink="{permalink}" data-instgrm-version="14"></blockquote>""";

        return new HtmlPlayerFrame(match.Value, html, EmbedScript);
    }

    [GeneratedRegex(@"^https?://(?:www\.)?instagram\.com/(?<kind>p|reel|reels|tv)/(?<shortcode>[A-Za-z0-9_-]+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
