namespace Forma.UI.Blazor.Components.Inputs;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.UI.Style.Glyphs;

/// <summary>The emoji the picker offers, and the search over them.</summary>
public static class EmojiCatalog
{
    /// <summary>The path each category's sprite is served from.</summary>
    public const string SpriteBasePath = "_content/Forma.UI/emojis";

    /// <summary>The Unicode emoji version the catalogue was generated from.</summary>
    public const string Version = "15.1";

    private const string ResourceFolder = "Forma.UI.Blazor.Assets.Emojis";

    private static readonly ConcurrentDictionary<string, IReadOnlyList<Emoji>> Loaded = new(StringComparer.Ordinal);

    private static readonly (string Id, string Title, Glyph Icon)[] GroupSource =
    [
        ("smileys", "Smileys & Emotion", Glyph.FaceSmile),
        ("people", "People & Body", Glyph.User),
        ("animals", "Animals & Nature", Glyph.Paw),
        ("food", "Food & Drink", Glyph.Utensils),
        ("activities", "Activities", Glyph.Futbol),
        ("travel", "Travel & Places", Glyph.Plane),
        ("objects", "Objects", Glyph.Lightbulb),
        ("symbols", "Symbols", Glyph.Heart),
        ("flags", "Flags", Glyph.Flag),
    ];

    /// <summary>The categories in the order the picker presents them.</summary>
    public static IReadOnlyList<EmojiGroup> Groups { get; } =
        [.. GroupSource.Select(group => new EmojiGroup { Id = group.Id, Title = group.Title, Icon = group.Icon })];

    /// <summary>Every emoji in the catalogue, which reads every category.</summary>
    public static IReadOnlyList<Emoji> All => [.. Groups.SelectMany(group => InGroup(group.Id))];

    /// <summary>Returns the emoji belonging to the named category, reading it on first use.</summary>
    public static IReadOnlyList<Emoji> InGroup(string groupId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);

        var known = Groups.Any(group => string.Equals(group.Id, groupId, StringComparison.Ordinal));

        return known ? Loaded.GetOrAdd(groupId, Load) : [];
    }

    /// <summary>Returns the emoji whose name or keywords begin a word matching every whitespace-separated term.</summary>
    public static IReadOnlyList<Emoji> Search(string? query)
    {
        var terms = (query ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (terms.Length == 0)
        {
            return [];
        }

        return [.. All.Where(emoji => terms.All(term => Matches(emoji, term)))];
    }

    private static bool Matches(Emoji emoji, string term)
        => StartsAWord(emoji.Name, term) || emoji.Keywords.Any(keyword => StartsAWord(keyword, term));

    // Matching the start of a word rather than any substring keeps "cat" off "intoxicated".
    private static bool StartsAWord(string text, string term)
    {
        var index = 0;

        while (index <= text.Length - term.Length)
        {
            if (string.Compare(text, index, term, 0, term.Length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }

            var next = text.IndexOf(' ', index, StringComparison.Ordinal);
            if (next < 0)
            {
                return false;
            }

            index = next + 1;
        }

        return false;
    }

    private static IReadOnlyList<Emoji> Load(string groupId)
    {
        var resourceName = $"{ResourceFolder}.{groupId}.json";

        using var stream = typeof(EmojiCatalog).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"The emoji catalogue resource '{resourceName}' is missing; regenerate it with scripts/fetch-emoji.ps1.");

        var raw = JsonSerializer.Deserialize(stream, EmojiCatalogueJsonContext.Default.RawCatalogue)
            ?? throw new InvalidOperationException($"The emoji catalogue resource '{resourceName}' could not be read.");

        return
        [
            .. raw.Emoji.Select((entry, position) => new Emoji
            {
                Character = entry.C,
                SpriteId = entry.S,
                ChunkIndex = position / raw.ChunkSize,
                Name = entry.N,
                Keywords = entry.K,
                GroupId = groupId,
            })
        ];
    }
}

/// <summary>One category's catalogue as scripts/fetch-emoji.ps1 writes it.</summary>
internal sealed record RawCatalogue(
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("group")] string Group,
    [property: JsonPropertyName("chunkSize")] int ChunkSize,
    [property: JsonPropertyName("emoji")] IReadOnlyList<RawEmoji> Emoji);

/// <summary>One catalogue entry as scripts/fetch-emoji.ps1 writes it.</summary>
internal sealed record RawEmoji(
    [property: JsonPropertyName("c")] string C,
    [property: JsonPropertyName("s")] string S,
    [property: JsonPropertyName("n")] string N,
    [property: JsonPropertyName("k")] IReadOnlyList<string> K);

[JsonSerializable(typeof(RawCatalogue))]
internal sealed partial class EmojiCatalogueJsonContext : JsonSerializerContext;
