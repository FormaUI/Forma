namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The metadata of a file uploaded to Google Drive.</summary>
internal sealed record GoogleDriveMetadata([property: JsonPropertyName("name")] string Name);
