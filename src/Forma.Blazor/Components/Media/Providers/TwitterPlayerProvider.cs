namespace Forma.Blazor.Components.Media.Providers;

using System.Net;
using System.Text.RegularExpressions;

/// <summary>Embeds a post on X through the service's own widget script.</summary>
internal sealed partial class TwitterPlayerProvider : IPlayerProvider
{
    private const string EmbedScript = "https://platform.twitter.com/widgets.js";

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
        var html = $"""<blockquote class="twitter-tweet" data-dnt="true"><a href="{address}"></a></blockquote>""";

        return new HtmlPlayerFrame(match.Value, html, EmbedScript);
    }

    [GeneratedRegex(@"^https?://(?:www\.|mobile\.)?(?:twitter|x)\.com/[A-Za-z0-9_]{1,15}/status/\d+", RegexOptions.IgnoreCase)]
    private static partial Regex AddressPattern();
}
