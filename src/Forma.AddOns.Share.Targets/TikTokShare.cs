namespace Forma.Framework.Share.Targets;

using System.Net.Http.Headers;
using System.Net.Http.Json;

using Forma.Framework.Icons;

/// <summary>Starts publishing a share item's video on the TikTok account whose token the host supplies; TikTok pulls the video from the resource's address.</summary>
public sealed class TikTokShare : ApiShare
{
    private static readonly Uri PublishEndpoint = new("https://open.tiktokapis.com/v2/post/publish/video/init/");

    private readonly Func<Task<string>> _accessToken;

    /// <summary>Creates a target publishing on the account the token accessor answers for.</summary>
    public TikTokShare(Func<Task<string>> accessToken, HttpClient? http = null)
        : base("tiktok", "TikTok", Brand.TikTok, http)
    {
        ArgumentNullException.ThrowIfNull(accessToken);

        _accessToken = accessToken;
    }

    /// <summary>TikTok pulls the video itself, so the item needs a resource living at a public address.</summary>
    public override bool Supports(ShareItem item)
        => ReachableSource(item) is not null;

    /// <inheritdoc />
    public override async Task ShareAsync(ShareItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (ReachableSource(item) is not { } source)
        {
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, PublishEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _accessToken().ConfigureAwait(false));
        request.Content = JsonContent.Create(new
        {
            post_info = new { title = item.Name ?? item.Resource!.Name, privacy_level = "SELF_ONLY" },
            source_info = new { source = "PULL_FROM_URL", video_url = source },
        });

        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }
}
