# Forma.AddOns.Emojis.Twemoji

The emoji artwork pack for `Forma.Blazor`. It implements `IEmojiProvider` and `IEmojiCatalog` from
`Forma.Framework.Emojis`: `FormaEmoji` draws from [Twemoji](https://github.com/jdecked/twemoji)
sprite sheets served as static web assets, and `FormaEmojiPicker` browses the catalog — names,
search keywords, and groups — that ships beside them.

## Install and register

```pwsh
dotnet add package FormaUI.NET-AddOns.Emojis.Twemoji
```

```csharp
using Forma.Framework.Emojis.Twemoji;

builder.Services.AddFormaTwemojiPack();
builder.Services.AddFormaUI();
```

That registers `TwemojiEmojiProvider` as the `IEmojiProvider` singleton and `TwemojiEmojiCatalog`
as the `IEmojiCatalog` singleton. Registration uses `TryAdd`, so a host that registered either
contract first keeps its own, and so the call goes before `AddFormaUI`, which registers the
platform-font provider the same way. The sprite sheets serve from
`_content/Forma.AddOns.Emojis.Twemoji/assets`, which the package's own code addresses — a host
references nothing there directly.

## What it provides

The catalog holds the fully qualified emoji of every group in `FormaEmojiPicker.DefaultGroups`,
without skin-tone variants. `TwemojiEmojiProvider.GetArtwork` returns `EmojiArtwork.Sprite` for each
of them, naming its symbol and the sheet that carries it, and `EmojiArtwork.Unicode` for any other
emoji — a skin-toned hand, or one from a newer release — so it draws with the reader's own font. An
unqualified spelling such as "⚠" draws as its fully qualified "⚠️".

## Without this package

`FormaEmoji` still renders, falling back to the reader's own platform font — so the same emoji
looks different per device rather than the same everywhere. `FormaEmojiPicker` shows its empty
state, because a catalog is data nothing can synthesize.

## Regenerating the artwork

`scripts/Get-TwemojiPack.ps1` rebuilds both halves: the sprite sheets into `wwwroot/assets/` and the
catalog JSON into `_assets/` in this project, from the Unicode emoji release the script pins, the
current CLDR English annotations, and the current Twemoji artwork. Run it to move to a newer
release of any of them; `-Format` writes the catalog indented for reading.

## Artwork and licenses

The graphics are [Twemoji](https://github.com/jdecked/twemoji), copyright 2020 Twitter, Inc and
other contributors and copyright 2023 the Twemoji contributors, used under
[CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/), whose attribution travels with the
artwork into every application that ships it. The names and search keywords come from the
[Unicode CLDR annotations](https://cldr.unicode.org), used under the
[Unicode License](https://www.unicode.org/license.txt). Both notices are reproduced in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), which ships inside this package.

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
