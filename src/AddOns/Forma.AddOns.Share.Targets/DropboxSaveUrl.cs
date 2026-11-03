namespace Forma.Framework.Share.Targets;

using System.Text.Json.Serialization;

/// <summary>The request asking Dropbox to save the file an address names.</summary>
internal sealed record DropboxSaveUrl(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("url")] string? Url);
