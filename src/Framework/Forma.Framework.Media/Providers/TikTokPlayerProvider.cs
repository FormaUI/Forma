namespace Forma.Framework.Media.Providers;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>Embeds a TikTok video through TikTok's own player script.</summary>
public sealed partial class TikTokPlayerProvider : IPlayerProvider
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

        var address = WebUtility.HtmlEncode(match.Value);
        var id = match.Groups["id"].Value;
        var html = $"""<blockquote class="tiktok-embed" cite="{address}" data-video-id="{id}"><section></section></blockquote>""";

        return new HtmlPlayerFrame(match.Value, html, EmbedScript);
    }

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?tiktok\.com/@[A-Za-z0-9_.]+/video/(?<id>\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
