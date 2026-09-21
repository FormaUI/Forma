# Forma.Framework.Emojis

The emoji contracts for `Forma.Blazor`: an `Emoji` value, the artwork a provider draws it with,
and the catalog a picker browses. `FormaEmoji` draws through `IEmojiProvider`, and
`FormaEmojiPicker` browses `IEmojiCatalog`; a host that wants its own artwork or its own emoji
data implements one of these and registers it.

## Install

```pwsh
dotnet add package Forma.Framework.Emojis
```

The package holds contracts and value types only. `Forma.Framework.Emojis.Unicode` ships the
default `IEmojiProvider`, drawing every emoji with the reader's own platform font, and
`Forma.AddOns.Emojis.Twemoji` is an artwork pack a host can install instead, supplying both a
provider and a catalog.

## The contract

| Type | What it is |
| --- | --- |
| `Emoji` | One emoji, named by the Unicode sequence that is it. Converts implicitly from `string`. |
| `IEmojiProvider` | Provides artwork for emoji: `GetArtwork(Emoji)` returns the `EmojiArtwork` to draw it with. |
| `EmojiArtwork` | The artwork used to draw an emoji: `Unicode`, `Sprite(symbol, sheet)`, or `Image(url)`. |
| `EmojiSprite` | Artwork drawn from a `Symbol` in a `Sheet` that is loaded before it resolves. |
| `EmojiImage` | Artwork drawn from a standalone image at `Url`. |
| `IEmojiCatalog` | The emoji a host offers, browsable by category and searchable by name and keyword. |
| `EmojiInfo` | One emoji in a catalog: its `Emoji`, English `Name`, search `Keywords`, and `GroupId`. |
| `EmojiGroup` | One category an emoji is filed under: its `Id` and its English `Title`. |
| `NullEmojiCatalog` | A catalog holding no emoji, standing in where a host installed none; `Instance` is the one instance. |

## Naming an emoji

An `Emoji` is a single character, however many scalar values it takes — a flag, a skin-toned hand,
or a family sequence is still one emoji. A `string` converts implicitly, and a sequence that may not
be one character goes through `TryCreate`:

```csharp
using Forma.Framework.Emojis;

Emoji smile = "😀";
Emoji family = "👨‍👩‍👧";

if (Emoji.TryCreate(userInput, out var emoji))
{
    // emoji.Sequence holds the scalar values
}
```

The constructor throws `ArgumentException` for an empty sequence or one longer than a character.

## Providing artwork

A provider answers `GetArtwork` for any emoji, choosing the platform font, a symbol in a sprite
sheet, or a standalone image. Register it as `IEmojiProvider` before `Forma.Blazor` registers its
defaults, and the components draw through it:

```csharp
using Forma.Framework.Emojis;

public sealed class CdnEmojiProvider : IEmojiProvider
{
    public EmojiArtwork GetArtwork(Emoji emoji)
    {
        var codepoints = string.Join("-", emoji.Sequence.EnumerateRunes().Select(rune => rune.Value.ToString("x")));

        return EmojiArtwork.Image($"https://cdn.example.test/emoji/{codepoints}.svg");
    }
}
```

```csharp
builder.Services.AddSingleton<IEmojiProvider, CdnEmojiProvider>();
builder.Services.AddFormaUI();
```

`EmojiArtwork.Unicode` hands the emoji to the reader's own font, so a provider that covers only part
of the set returns it for the rest.

## Providing a catalog

A catalog answers three questions: the members of a category by `TryFindByGroupId`, the entry for
one emoji by `TryGetByEmoji`, and the entries whose name or keywords match every
whitespace-separated term by `Find`. Each `EmojiInfo` names its category by `GroupId`, and the
category ids are the ones the picker's `Groups` parameter lists, `FormaEmojiPicker.DefaultGroups`
unless the host passes its own.

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

A host that registers no catalog gets `NullEmojiCatalog.Instance` in the picker, which browses and
finds nothing.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
