# Forma.Framework.Icons.Glyphs

Forma's own icon set, as the `IIconProvider` that `Forma.Framework.Icons` defines: every `Glyph`
drawn by hand in both variants, every `Brand` as its official mark, and any character. The artwork
is embedded in the assembly, so a host loads no icon font and serves no asset. It is the provider
`Forma.Blazor` draws through unless a host registers its own.

## Install and register

```pwsh
dotnet add package FormaUI.NET-Framework.Icons.Glyphs
```

```csharp
using Forma.Framework.Icons.Glyphs;

builder.Services.AddFormaGlyphServices();
```

That registers `GlyphIconProvider` as the `IIconProvider` singleton. Registration uses `TryAdd`,
so a host that registered a provider first keeps its own. `AddFormaUI()` in `Forma.Blazor` makes
this call itself, so a host of the components installs nothing and calls nothing to draw the set.

Outside dependency injection, `new GlyphIconProvider()` is the same provider; it takes no
arguments and reads its artwork once, on first use.

## What it draws

| Icon | Artwork |
| --- | --- |
| A `GlyphIcon` | An `IconVector`. `GlyphVariant.Default` is the outline, its paths stroked; `GlyphVariant.Alternate` is the solid shape, its paths filled, or heavier strokes for a glyph with nothing to fill, such as an arrow or a chevron. Every glyph is drawn on two grids, and the size picks the one whose stroke lands on whole pixels there: `0 0 24 24` for `IconSize.Small` and `IconSize.Large`, `0 0 32 32` for `IconSize.Inline`, `IconSize.Medium` and `IconSize.Display`. |
| A `BrandIcon` | A `BrandIconArtwork`: the mark as one solid path, with the brand's name as its `Title`. The same mark answers every size. |
| A `CharacterIcon` | An `IconCharacter` holding the character itself. |

Every drawing is made for left-to-right reading. A glyph that points along the reading direction
declares on its `Glyph` member the direction it adapts for, and `FormaIcon` turns the artwork over
where text runs that way;
the provider answers the one drawing either way.

`Glyph.None` and `Brand.None` name no artwork, and both members throw
`ArgumentOutOfRangeException` for them. A `Brand` value with no mark compiled answers `false`
and `IconArtwork.None` from `TryGetArtwork`, and `GetArtwork` throws.

```csharp
using System.Text;
using Forma.Framework.Icons;
using Forma.Framework.Icons.Glyphs;

IIconProvider provider = new GlyphIconProvider();

var check = (IconVector)provider.GetArtwork(Glyph.Check, IconSize.Small);
// check.ViewBox is "0 0 24 24"; each path has StrokeWidth 2

var heart = (IconVector)provider.GetArtwork(Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate), IconSize.Medium);
// heart.ViewBox is "0 0 32 32"; each path has StrokeWidth 0, so it is filled

var gitHub = (BrandIconArtwork)provider.GetArtwork(Brand.GitHub, IconSize.Inline);
// gitHub.Title is "GitHub"

if (provider.TryGetArtwork(new Rune('Ω'), IconSize.Inline, out var omega))
{
    // omega is an IconCharacter whose Value is 'Ω'
}
```

## Artwork and licenses

The glyphs are Forma's own drawings, authored to
[the glyph artwork specification](../../../docs/glyph-artwork-spec.md) and licensed with the rest
of the repository.

The brand marks are never drawn by hand: each is the official mark, taken from a pinned release
of an open icon set. Marks from [Simple Icons](https://simpleicons.org) are used under
[CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md); marks from
[Bootstrap Icons](https://icons.getbootstrap.com) are used under the
[MIT license](https://github.com/twbs/icons/blob/main/LICENSE), whose notice is reproduced in
[`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md), shipped inside this package. Neither license
covers the marks the drawings depict: each logo remains the trademark of its owner and is subject
to that owner's brand guidelines, and the accepted use is linking to a presence on that brand's
platform.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
