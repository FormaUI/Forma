namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The request asking TikTok to publish the video an address names.</summary>
internal sealed record TikTokPublish(
    [property: JsonPropertyName("post_info")] TikTokPostInfo PostInfo,
    [property: JsonPropertyName("source_info")] TikTokSourceInfo SourceInfo);
