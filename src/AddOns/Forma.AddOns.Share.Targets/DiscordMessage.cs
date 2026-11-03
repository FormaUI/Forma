namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The message a Discord webhook posts.</summary>
internal sealed record DiscordMessage([property: JsonPropertyName("content")] string Content);
