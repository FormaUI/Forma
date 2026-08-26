namespace Forma.UI.Blazor.Design.Emojis;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.UI.Design.Emojis;

/// <summary>Reads the emoji catalog this library embeds, shared by the default catalog and provider.</summary>
internal static class EmojiAssetLoader
{
    /// <summary>The path each category's sprite sheet is served from.</summary>
    public const string SpriteBasePath = "_content/Forma.UI/emojis";

    /// <summary>The categories, in the order the picker presents them.</summary>
    public static readonly IReadOnlyList<EmojiGroup> Groups =
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

    private const string ResourceFolder = "Forma.UI.Blazor.Assets.Emojis";

    private static readonly ConcurrentDictionary<string, IReadOnlyList<EmojiAsset>> Loaded = new(StringComparer.Ordinal);

    /// <summary>Whether a group of this identifier exists in the catalog.</summary>
    public static bool IsKnownGroup(string groupId)
        => Groups.Any(group => string.Equals(group.Id, groupId, StringComparison.Ordinal));

    /// <summary>Returns the emoji belonging to the named category, reading it on first use.</summary>
    public static IReadOnlyList<EmojiAsset> InGroup(string groupId)
        => IsKnownGroup(groupId) ? Loaded.GetOrAdd(groupId, Load) : [];

    /// <summary>Every emoji in the catalog, which reads every category.</summary>
    public static IReadOnlyList<EmojiAsset> All()
        => [.. Groups.SelectMany(group => InGroup(group.Id))];

    private static IReadOnlyList<EmojiAsset> Load(string groupId)
    {
        var resourceName = $"{ResourceFolder}.{groupId}.json";

        using var stream = typeof(EmojiAssetLoader).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The emoji catalog resource '{resourceName}' is missing; regenerate it with scripts/fetch-emoji.ps1.");

        var raw = JsonSerializer.Deserialize(stream, EmojiCatalogJsonContext.Default.RawCatalog)
            ?? throw new InvalidOperationException($"The emoji catalog resource '{resourceName}' could not be read.");

        return
        [
            .. raw.Emoji.Select((entry, position) => new EmojiAsset
            {
                Emoji = entry.C,
                Name = entry.N,
                Keywords = entry.K,
                GroupId = groupId,
                SpriteId = entry.S,
                ChunkIndex = position / raw.ChunkSize,
            })
        ];
    }
}

/// <summary>One category's catalog as scripts/fetch-emoji.ps1 writes it.</summary>
internal sealed record RawCatalog(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("chunkSize")] int ChunkSize,
    [property: JsonPropertyName("emoji")] IReadOnlyList<RawEmoji> Emoji);

/// <summary>One catalog entry as scripts/fetch-emoji.ps1 writes it.</summary>
internal sealed record RawEmoji(
    [property: JsonPropertyName("c")] string C,
    [property: JsonPropertyName("s")] string S,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("k")] IReadOnlyList<string> K);

[JsonSerializable(typeof(RawCatalog))]
internal sealed partial class EmojiCatalogJsonContext : JsonSerializerContext;
