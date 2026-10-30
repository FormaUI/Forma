namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>Where a TikTok publish request takes its video from.</summary>
internal sealed record TikTokSourceInfo(
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("video_url")] string? VideoUrl);
