namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The requests the share targets send, written through generated code rather than reflection.</summary>
[JsonSerializable(typeof(DropboxSaveUrl))]
[JsonSerializable(typeof(GoogleDriveMetadata))]
[JsonSerializable(typeof(TikTokPublish))]
[JsonSerializable(typeof(DiscordMessage))]
internal sealed partial class ShareTargetsJsonContext : JsonSerializerContext;
