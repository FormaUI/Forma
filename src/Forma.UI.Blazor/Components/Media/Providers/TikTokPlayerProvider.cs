namespace Forma.UI.Components.Media.Providers;

using System.Net;
using System.Text.RegularExpressions;

using Forma.Framework.Media;
using Forma.UI.Components.Media;

/// <summary>Embeds a TikTok video through TikTok's own player script.</summary>
internal sealed partial class TikTokPlayerProvider : IPlayerProvider
{
    private const string EmbedScript = "https://www.tiktok.com/embed.js";

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
        var id = match.Groups["id"].Value;
        var html = $"""<blockquote class="tiktok-embed" cite="{address}" data-video-id="{id}"><section></section></blockquote>""";

        return new HtmlPlayerFrame(match.Value, html, EmbedScript);
    }

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?tiktok\.com/@[A-Za-z0-9_.]+/video/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
