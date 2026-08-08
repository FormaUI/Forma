namespace Forma.Blazor.Components.Media.Providers;

using System.Text.RegularExpressions;

/// <summary>Embeds a Twitch stream, video or clip.</summary>
internal sealed partial class TwitchPlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        // Twitch refuses to load without the embedding host, so an unknown page is an address it cannot play.
        => ValueTask.FromResult<PlayerFrame?>(options.PageAddress is null ? null : BuildFrame(source, options, options.PageAddress.Host));

    private static EmbeddedPlayerFrame? BuildFrame(string source, PlayerOptions options, string parent)
    {
        if (HostedClipPattern().Match(source) is { Success: true } hostedClip)
        {
            return Embedded("https://clips.twitch.tv/embed", $"clip={hostedClip.Groups["slug"].Value}", options, parent);
        }

        if (ChannelClipPattern().Match(source) is { Success: true } channelClip)
        {
            return Embedded("https://clips.twitch.tv/embed", $"clip={channelClip.Groups["slug"].Value}", options, parent);
        }

        if (VideoPattern().Match(source) is { Success: true } video)
        {
            // The player wants the "v" prefix that the web address omits.
            return Embedded("https://player.twitch.tv/", $"video=v{video.Groups["id"].Value}", options, parent);
        }

        return ChannelPattern().Match(source) is { Success: true } channel
            ? Embedded("https://player.twitch.tv/", $"channel={channel.Groups["name"].Value}", options, parent)
            : null;
    }

    private static EmbeddedPlayerFrame Embedded(string playerUrl, string target, PlayerOptions options, string parent)
    {
        // Twitch autoplays unless told not to, which is the opposite of every other provider here.
        var query = new List<string>
        {
            target,
            $"parent={Uri.EscapeDataString(parent)}",
            options.Autoplay ? "autoplay=true" : "autoplay=false",
        };

        if (options.Autoplay)
        {
            query.Add("muted=true");
        }

        return new EmbeddedPlayerFrame($"{playerUrl}?{string.Join('&', query)}");
    }

    [GeneratedRegex(@"^https?://clips\.twitch\.tv/(?<slug>[A-Za-z0-9_-]+)(?:[?#]|/?$)", RegexOptions.IgnoreCase)]
    private static partial Regex HostedClipPattern();

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?twitch\.tv/[A-Za-z0-9_]+/clip/(?<slug>[A-Za-z0-9_-]+)(?:[?#]|/?$)", RegexOptions.IgnoreCase)]
    private static partial Regex ChannelClipPattern();

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?twitch\.tv/videos/(?<id>\d+)(?:[?#]|/?$)", RegexOptions.IgnoreCase)]
    private static partial Regex VideoPattern();

    [GeneratedRegex(@"^https?://(?:www\.|m\.)?twitch\.tv/(?<name>[A-Za-z0-9_]{3,25})(?:[?#]|/?$)", RegexOptions.IgnoreCase)]
    private static partial Regex ChannelPattern();
}
