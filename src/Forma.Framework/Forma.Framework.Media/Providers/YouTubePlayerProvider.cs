namespace Forma.Framework.Media.Providers;

using System.Text.RegularExpressions;

/// <summary>Embeds a YouTube video from its watch, share, embed, shorts or live address.</summary>
public sealed partial class YouTubePlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);

        return ValueTask.FromResult<PlayerFrame?>(BuildFrame(source, options));
    }

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

        var host = options.LimitTracking ? "www.youtube-nocookie.com" : "www.youtube.com";
        return new EmbeddedPlayerFrame($"https://{host}/embed/{match.Groups["id"].Value}?{string.Join('&', query)}");
    }

    [GeneratedRegex(
        @"^https?://(?:www\.|m\.)?(?:youtube\.com/(?:watch\?(?:[^#]*&)?v=|embed/|shorts/|v/|live/)|youtu\.be/)(?<id>[A-Za-z0-9_-]{11})(?:[?&#/]|$)",
        RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
