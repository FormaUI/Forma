namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The post a TikTok publish request makes.</summary>
internal sealed record TikTokPostInfo(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("privacy_level")] string PrivacyLevel);
