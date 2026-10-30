# Forma.Framework.Emojis

The emoji contracts for Forma: `Emoji`, the value that names one emoji; `IEmojiProvider`, which
gives the artwork an emoji is drawn with; and `IEmojiCatalog`, the emoji a picker browses and
searches. Implement a provider to draw emoji with your own artwork, and a catalog to choose the
emoji `FormaEmojiPicker` offers.

## Install

```pwsh
dotnet add package formaui-net.Framework.Emojis
```

## Naming an emoji

An `Emoji` is one character, however many scalar values it takes: a flag, a skin-toned hand and a
family sequence are each one emoji. A `string` converts implicitly, and `TryCreate` checks text
that may hold more or less than one character:

```csharp
using Forma.Framework.Emojis;

Emoji smile = "😀";
Emoji family = "👨‍👩‍👧";

if (Emoji.TryCreate(userInput, out var emoji))
{
    // emoji.Sequence holds the scalar values
}
```

## Providing artwork

| Type | What it is |
| --- | --- |
| `IEmojiProvider` | Provides artwork for emoji: `GetArtwork(Emoji)` returns the `EmojiArtwork` to draw it with. |
| `EmojiArtwork` | The artwork used to draw an emoji: `EmojiArtwork.Unicode`, the platform's emoji font; `EmojiArtwork.Sprite(symbol, sheet)`, a symbol in a sprite sheet; or `EmojiArtwork.Image(url)`, a standalone image. |
| `EmojiSprite` | Sprite artwork: its `Symbol` and the `Sheet` it is loaded from. |
| `EmojiImage` | Image artwork: its `Url`. |

A provider answers every emoji with artwork, and `EmojiArtwork.Unicode` hands an emoji to the
platform's font. This one draws each emoji from an image named by its code points:

```csharp
using System.Globalization;

using Forma.Framework.Emojis;

public sealed class CdnEmojiProvider : IEmojiProvider
{
    public EmojiArtwork GetArtwork(Emoji emoji)
    {
        var codepoints = string.Join("-", emoji.Sequence.EnumerateRunes()
            .Select(rune => rune.Value.ToString("x", CultureInfo.InvariantCulture)));

        return EmojiArtwork.Image($"https://cdn.example.test/emoji/{codepoints}.svg");
    }
}
```

Register it as the `IEmojiProvider` before `AddFormaUIServer()`, which registers its default provider
only where none is registered:

```csharp
builder.Services.AddSingleton<IEmojiProvider, CdnEmojiProvider>();
builder.AddFormaUIServer();
```

## Providing a catalog

| Type | What it is |
| --- | --- |
| `IEmojiCatalog` | The emoji a host offers: `TryFindByGroupId` lists a category, `TryGetByEmoji` finds one emoji's entry, and `Find` returns the entries whose name or keywords match every whitespace-separated term. |
| `EmojiInfo` | One emoji in a catalog: its `Emoji`, English `Name`, search `Keywords` and `GroupId`. |
| `EmojiGroup` | One category: its `Id` and its English `Title`, which is also its localization key. |
| `NullEmojiCatalog` | The catalog holding no emoji, as `NullEmojiCatalog.Instance`. |

Each entry's `GroupId` is one of the categories the picker lists: `FormaEmojiPicker.DefaultGroups`
holds `smileys`, `people`, `animals`, `food`, `activities`, `travel`, `objects`, `symbols` and
`flags`, and the picker's `Groups` parameter takes your own.

```csharp
using System.Diagnostics.CodeAnalysis;

using Forma.Framework.Emojis;

public sealed class TeamEmojiCatalog : IEmojiCatalog
{
    private static readonly IReadOnlyList<EmojiInfo> Entries =
    [
        new() { Emoji = "🚴", Name = "person biking", Keywords = ["bicycle", "cyclist"], GroupId = "activities" },
        new() { Emoji = "🏆", Name = "trophy", Keywords = ["prize", "win"], GroupId = "activities" },
    ];

    public bool TryFindByGroupId(string groupId, out IReadOnlyList<EmojiInfo> emojis)
    {
        emojis = Entries.Where(entry => entry.GroupId == groupId).ToList();
        return emojis.Count > 0;
    }

    public bool TryGetByEmoji(Emoji emoji, [MaybeNullWhen(false)] out EmojiInfo info)
    {
        info = Entries.FirstOrDefault(entry => entry.Emoji == emoji);
        return info is not null;
    }

    public IReadOnlyList<EmojiInfo> Find(string text)
    {
        var terms = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return terms.Length == 0
            ? []
            : Entries.Where(entry => terms.All(term => Matches(entry, term))).ToList();
    }

    private static bool Matches(EmojiInfo entry, string term)
        => entry.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
            || entry.Keywords.Any(keyword => keyword.Contains(term, StringComparison.OrdinalIgnoreCase));
}
```

```csharp
builder.Services.AddSingleton<IEmojiCatalog, TeamEmojiCatalog>();
```

## Related packages

- [`formaui-net.Framework.Emojis.Unicode`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Emojis.Unicode/README.md)
  is the default provider, drawing every emoji with the reader's own emoji font.
- [`formaui-net.AddOns.Emojis.Twemoji`](https://github.com/FormaUI/Forma/blob/main/src/Forma.AddOns/Forma.AddOns.Emojis.Twemoji/README.md)
  supplies Twemoji artwork and a catalog.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
