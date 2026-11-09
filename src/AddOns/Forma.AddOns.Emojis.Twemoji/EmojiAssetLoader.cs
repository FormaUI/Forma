namespace Forma.Framework.Emojis.Twemoji;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.Framework.Emojis;

/// <summary>Reads the emoji catalog this package embeds.</summary>
internal static class EmojiAssetLoader
{
    /// <summary>The base path the groups' sprite sheets are served from.</summary>
    public const string SpriteBasePath = "_content/Forma.AddOns.Emojis.Twemoji/assets";

    /// <summary>The emoji groups, in presentation order.</summary>
    public static IReadOnlyList<EmojiGroup> Groups { get; } =
    [
        new() { Id = "smileys", Title = "Smileys & Emotion" },
        new() { Id = "people", Title = "People & Body" },
        new() { Id = "animals", Title = "Animals & Nature" },
        new() { Id = "food", Title = "Food & Drink" },
        new() { Id = "activities", Title = "Activities" },
        new() { Id = "travel", Title = "Travel & Places" },
        new() { Id = "objects", Title = "Objects" },
        new() { Id = "symbols", Title = "Symbols" },
        new() { Id = "flags", Title = "Flags" },
    ];

    private const string ResourceFolder = "Forma.Framework.Emojis.Twemoji._assets";

    private static readonly ConcurrentDictionary<string, IReadOnlyList<EmojiAsset>> AssetsByGroup = new(StringComparer.Ordinal);

    /// <summary>Whether a group of this identifier exists in the catalog.</summary>
    public static bool IsKnownGroup(string groupId)
        => Groups.Any(group => string.Equals(group.Id, groupId, StringComparison.Ordinal));

    /// <summary>Returns the emoji of the group with this identifier.</summary>
    public static IReadOnlyList<EmojiAsset> LoadGroup(string groupId)
        => IsKnownGroup(groupId) ? AssetsByGroup.GetOrAdd(groupId, LoadGroupCore) : [];

    /// <summary>Returns every emoji in the catalog.</summary>
    public static IReadOnlyList<EmojiAsset> LoadAll()
        => [.. Groups.SelectMany(group => LoadGroup(group.Id))];

    private static IReadOnlyList<EmojiAsset> LoadGroupCore(string groupId)
    {
        var resourceName = $"{ResourceFolder}.{groupId}.json";

        using var stream = typeof(EmojiAssetLoader).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The emoji catalog resource '{resourceName}' is missing; regenerate it with scripts/Get-TwemojiPack.ps1.");

        var rawCatalog = JsonSerializer.Deserialize(stream, EmojiCatalogJsonContext.Default.RawCatalog)
            ?? throw new InvalidOperationException($"The emoji catalog resource '{resourceName}' could not be read.");

        return
        [
            .. rawCatalog.Emoji.Select((entry, position) => new EmojiAsset
            {
                Emoji = entry.C,
                Name = entry.N,
                Keywords = entry.K,
                GroupId = groupId,
                SpriteId = entry.S,
                ChunkIndex = position / rawCatalog.ChunkSize,
            })
        ];
    }
}

/// <summary>One group's catalog as its embedded resource holds it.</summary>
internal sealed record RawCatalog(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("chunkSize")] int ChunkSize,
    [property: JsonPropertyName("emoji")] IReadOnlyList<RawEmoji> Emoji);

/// <summary>One catalog entry as the embedded resource holds it.</summary>
internal sealed record RawEmoji(
    [property: JsonPropertyName("c")] string C,
    [property: JsonPropertyName("s")] string S,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("k")] IReadOnlyList<string> K);

[JsonSerializable(typeof(RawCatalog))]
internal sealed partial class EmojiCatalogJsonContext : JsonSerializerContext;
