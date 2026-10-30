# Forma.Framework.Icons.Glyphs

Forma's own icon set, as an `IIconProvider` from `Forma.Framework.Icons`: every `Glyph` in both
variants, every `Brand` as its official mark, and any character. The artwork is embedded in the
assembly and draws as inline SVG, so icons need only this package.

## Install and register

```pwsh
dotnet add package formaui-net.Framework.Icons.Glyphs
```

```csharp
using Forma.Framework.Icons.Glyphs;

builder.Services.AddFormaGlyphServices();
```

`AddFormaGlyphServices` registers `GlyphIconProvider` as the `IIconProvider` singleton with
`TryAdd`, so a provider registered first is kept. `AddFormaUI()` in `Forma.Blazor` makes this call
itself. Outside dependency injection, `new GlyphIconProvider()` is the same provider.

## What it draws

| Icon | Artwork |
| --- | --- |
| `GlyphIcon` | An `IconVector`. `GlyphVariant.Default` is the outline, its paths stroked; `GlyphVariant.Alternate` is the solid shape, its paths filled, or stroked heavier for a glyph with nothing to fill, such as an arrow. Each glyph is drawn on a 24-unit grid for `IconSize.Small` and `IconSize.Large` and a 32-unit grid for the other sizes, so its strokes land on whole pixels. |
| `BrandIcon` | A `BrandIconArtwork`: the mark as one solid path, titled with the brand's name, the same at every size. |
| `CharacterIcon` | An `IconCharacter` holding the character. |

```csharp
using System.Text;

using Forma.Framework.Icons;
using Forma.Framework.Icons.Glyphs;

IIconProvider provider = new GlyphIconProvider();

var check = (IconVector)provider.GetArtwork(Glyph.Check, IconSize.Small);
// check.ViewBox is "0 0 24 24", and its paths are stroked

var heart = (IconVector)provider.GetArtwork(Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate), IconSize.Medium);
// heart.ViewBox is "0 0 32 32", and its paths are filled

var gitHub = (BrandIconArtwork)provider.GetArtwork(Brand.GitHub, IconSize.Inline);
// gitHub.Title is "GitHub"

if (provider.TryGetArtwork(new Rune('Ω'), IconSize.Inline, out var omega))
{
    // omega is an IconCharacter whose Value is 'Ω'
}
```

## Artwork and licenses

The glyphs are Forma's own drawings, made to
[the glyph artwork specification](https://github.com/FormaUI/Forma/blob/main/docs/glyph-artwork-specification.md)
and licensed with the rest of the repository.

Each brand mark is the brand's official mark, taken from a pinned release of an open icon set:
[Simple Icons](https://simpleicons.org), used under
[CC0 1.0](https://github.com/simple-icons/simple-icons/blob/develop/LICENSE.md), and
[Bootstrap Icons](https://icons.getbootstrap.com), used under the
[MIT license](https://github.com/twbs/icons/blob/main/LICENSE), whose notice ships in this package
as [`THIRD-PARTY-NOTICES.md`](https://github.com/FormaUI/Forma/blob/main/src/Forma.Framework/Forma.Framework.Icons.Glyphs/THIRD-PARTY-NOTICES.md).
Each logo remains the trademark of its owner and is subject to that owner's brand guidelines; use a
mark to link to a presence on that brand's platform.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
