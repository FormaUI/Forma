namespace Forma.UI.Components.Media.Providers;

using System.Text.RegularExpressions;

using Forma.Framework.Media;
using Forma.UI.Components.Media;

/// <summary>Embeds a Vimeo video from its watch or player address.</summary>
internal sealed partial class VimeoPlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        => ValueTask.FromResult<PlayerFrame?>(BuildFrame(source, options));

    private static EmbeddedPlayerFrame? BuildFrame(string source, PlayerOptions options)
    {
        var match = AddressPattern().Match(source);

        if (!match.Success)
        {
            return null;
        }

        var query = new List<string>();

        if (options.Autoplay)
        {
            query.Add("autoplay=1");
            query.Add("muted=1");
        }

        if (options.LimitTracking)
        {
            query.Add("dnt=1");
        }

        var suffix = query.Count > 0 ? $"?{string.Join('&', query)}" : string.Empty;
        return new EmbeddedPlayerFrame($"https://player.vimeo.com/video/{match.Groups["id"].Value}{suffix}");
    }

    // Anchored to Vimeo's own hosts, so an address on any other domain is not mistaken for one.
    [GeneratedRegex(
        @"^https?://(?:www\.)?(?:vimeo\.com/(?:video/|channels/[^/]+/|groups/[^/]+/videos/)?|player\.vimeo\.com/video/)(?<id>\d+)(?:[?&#/]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
