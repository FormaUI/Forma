# Forma.Framework.Icons

The icon contracts for Forma: `Icon`, which names what to draw, a glyph, a brand mark or a
character; `IconSize`, the size it is drawn at; `IconArtwork`, the geometry or character that draws
it; and `IIconProvider`, which answers an icon with its artwork. Reference it to draw Forma's
icons with a set of your own, or to name icons in a library of your own.

## Install

```pwsh
dotnet add package formaui-net.Framework.Icons --prerelease
```

## Naming an icon

| Type | What it is |
| --- | --- |
| `Icon` | What an icon draws: a `GlyphIcon`, a `BrandIcon` or a `CharacterIcon`. |
| `Glyph` | A mark named for its recognizable form, `ArrowLeft`, `ChevronDown`, `Folder`, or for its conventional meaning, `Search`, `Delete`, `Settings`. |
| `GlyphVariant` | `Default`, the provider's primary representation, or `Alternate`, its alternate one. |
| `Brand` | A brand whose official mark is drawn: `GitHub`, `X`, `YouTube`. |
| `IconSize` | `Inline` with the surrounding text, `Small` on a button or in a dense row, `Medium` leading a panel or a tile, `Large` as a section's figure, `Display` as the figure of an empty state or a drop zone. |

A `Glyph`, a `Brand` or a `Rune` converts to an `Icon`, and `Icon.FromGlyph` names a variant:

```csharp
using System.Text;

using Forma.Framework.Icons;

Icon save = Glyph.Save;
Icon heart = Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate);
Icon gitHub = Brand.GitHub;
Icon omega = new Rune('Ω');
```

Icons are records, so two icons naming the same glyph in the same variant are equal.

## Direction

Glyph artwork is drawn for left-to-right text. A glyph whose form points along the reading
direction, such as an arrow, a chevron or `Undo`, declares the direction it adapts for as the HTML
`dir` token, and `FormaIcon` draws it turned over where text runs that way:

```csharp
Glyph.ArrowLeft.GetDirectionSensitivity();    // "rtl"
Glyph.ArrowLeft.IsDirectionSensitive("rtl");  // true
Glyph.AlignLeft.GetDirectionSensitivity();    // null
```

## Artwork

| Type | What it is |
| --- | --- |
| `IconArtwork` | The artwork used to draw an icon: an `IconVector`, a `BrandIconArtwork` or an `IconCharacter`. `IconArtwork.None` is nothing to draw. |
| `IconVector` | SVG geometry: a `ViewBox` and the `Paths` composing it, in painting order. Built by `IconArtwork.Vector(viewBox, paths)`. |
| `BrandIconArtwork` | An `IconVector` carrying the brand's name as `Title`. Built by `IconArtwork.Brand(viewBox, paths, title)`. |
| `IconCharacter` | A single character drawn by the font: its `Value`. Built by `IconArtwork.Character(value)`. |
| `IconPath` | One path: its SVG path `Data`, its `Opacity`, and its `StrokeWidth` in the artwork's own units, where zero fills the path. |

## Implementing a provider

| Member | What it does |
| --- | --- |
| `bool TryGetArtwork(Icon icon, IconSize size, out IconArtwork artwork)` | Gets the artwork for an icon at a size, returning whether the provider has it. |
| `IconArtwork GetArtwork(Icon icon, IconSize size)` | Gets the artwork for an icon at a size, throwing `ArgumentOutOfRangeException` where the provider has none. |

The size lets a provider answer with artwork drawn for that size. A provider answers the icons it
carries and declines the rest with `IconArtwork.None`:

```csharp
using Forma.Framework.Icons;

public sealed class HouseIconProvider : IIconProvider
{
    private static readonly IconArtwork SaveArtwork = IconArtwork.Vector(
        "0 0 24 24",
        [
            new IconPath { Data = "M5 3h11l3 3v15H5z", StrokeWidth = 2 },
            new IconPath { Data = "M8 3v6h7V3", StrokeWidth = 2 },
        ]);

    public bool TryGetArtwork(Icon icon, IconSize size, out IconArtwork artwork)
    {
        switch (icon)
        {
            case GlyphIcon { Glyph: Glyph.Save }:
                artwork = SaveArtwork;
                return true;
            case CharacterIcon character:
                artwork = IconArtwork.Character(character.Character);
                return true;
            default:
                artwork = IconArtwork.None;
                return false;
        }
    }

    public IconArtwork GetArtwork(Icon icon, IconSize size)
        => TryGetArtwork(icon, size, out var artwork)
            ? artwork
            : throw new ArgumentOutOfRangeException(nameof(icon), $"No artwork for {icon}.");
}
```

Register it as the `IIconProvider` on each side that renders icons, in any order beside
`AddFormaUIServer()` and `AddFormaUIWebAssembly()`, which register no icon provider of their own:

```csharp
builder.Services.AddSingleton<IIconProvider, HouseIconProvider>();
builder.AddFormaUIServer();
```

`FormaIcon` also takes a provider for one icon through its `Provider` parameter, or for a subtree
as a cascading `IIconProvider`; the registered provider answers whatever that provider declines.

## Reading artwork

Draw by the kind of artwork the provider returns, matching `BrandIconArtwork` before the
`IconVector` it derives from:

```csharp
using Forma.Framework.Icons;

if (provider.TryGetArtwork(Glyph.Delete, IconSize.Small, out var artwork))
{
    switch (artwork)
    {
        case BrandIconArtwork brand:
            // brand.ViewBox, brand.Paths, brand.Title
            break;
        case IconVector vector:
            // vector.ViewBox, vector.Paths
            break;
        case IconCharacter character:
            // character.Value
            break;
    }
}
```

## Related packages

[`formaui-net.Framework.Icons.Glyphs`](https://github.com/FormaUI/Forma/blob/main/src/Framework/Forma.Framework.Icons.Glyphs/README.md)
is Forma's own icon set and the default provider.

## License

Apache License 2.0, under the repository's [`LICENSE`](https://github.com/FormaUI/Forma/blob/main/LICENSE).
