# Forma.AddOns.Emojis.Twemoji

The emoji artwork pack for `Forma.Blazor`. It draws emoji from
[Twemoji](https://github.com/jdecked/twemoji) sprite sheets served as static web assets, and ships
the catalog — names, search keywords, and groups — that `FormaEmojiPicker` browses.

## Install and register

```pwsh
dotnet add package 2Site.Forma.AddOns.Emojis.Twemoji
```

```csharp
using Forma.AddOns.Emojis.Twemoji;

builder.Services.AddFormaTwemojiPack();
```

That registers the pack's `IEmojiProvider`, which `FormaEmoji` draws through, and its
`IEmojiCatalog`, which fills `FormaEmojiPicker`. Registration uses `TryAdd`, so a host that
registered either contract first keeps its own. The sprite sheets serve from
`_content/Forma.AddOns.Emojis.Twemoji`, which the package's own code addresses — a host references
nothing there directly.

## Without this package

`FormaEmoji` still renders, falling back to the reader's own platform font — so the same emoji
looks different per device rather than the same everywhere. `FormaEmojiPicker` shows its empty
state, because a catalog is data nothing can synthesize.

## Artwork and licenses

The graphics are [Twemoji](https://github.com/jdecked/twemoji), copyright 2020 Twitter, Inc and
other contributors and copyright 2023 the Twemoji contributors, used under
[CC-BY 4.0](https://creativecommons.org/licenses/by/4.0/) — a license that makes this attribution a
condition of shipping the artwork. The names and search keywords come from the
[Unicode CLDR annotations](https://cldr.unicode.org), used under the Unicode License. Both notices
are reproduced in [`THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md), which ships inside this
package.

## Regenerating the artwork

`scripts/Get-TwemojiPack.ps1` rebuilds both halves: the sprite sheets into `wwwroot/assets/` and the catalog
JSON into `Assets/` in this project. Run it to move to a newer Twemoji or CLDR release.
