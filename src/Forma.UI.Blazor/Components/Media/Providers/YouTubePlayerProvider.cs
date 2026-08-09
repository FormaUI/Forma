namespace Forma.UI.Components.Media.Providers;

using System.Text.RegularExpressions;

using Forma.Framework.Media;
using Forma.UI.Components.Media;

/// <summary>Embeds a YouTube video from its watch, share, embed, shorts or live address.</summary>
internal sealed partial class YouTubePlayerProvider : IPlayerProvider
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

        var query = new List<string> { "rel=0" };

        if (options.Autoplay)
        {
            query.Add("autoplay=1");
            query.Add("mute=1");
        }

        // YouTube expresses limited tracking as a separate domain rather than a parameter.
        var host = options.LimitTracking ? "www.youtube-nocookie.com" : "www.youtube.com";
        return new EmbeddedPlayerFrame($"https://{host}/embed/{match.Groups["id"].Value}?{string.Join('&', query)}");
    }

    // Anchored to YouTube's own hosts, so an address on any other domain is not mistaken for one.
    [GeneratedRegex(
        @"^https?://(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:[^#]*&)?v=|embed/|shorts/|v/|live/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})(?:[?&#/]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
