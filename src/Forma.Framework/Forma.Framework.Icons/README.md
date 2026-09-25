# Forma.Framework.Icons

The icon contract `Forma.Blazor` draws through: what an icon is — a glyph, a brand mark or a
character — the size it is drawn at, the artwork that draws it, and `IIconProvider`, which answers
an icon with its artwork. The package carries no artwork of its own. `Forma.Framework.Icons.Glyphs`
is the provider Forma ships, and a host that wants a different set implements `IIconProvider`
against this package.

## Install

```pwsh
dotnet add package FormaUI.NET-Framework.Icons
```

A host of `Forma.Blazor` already has it, restored beside the library. Install it directly in a
project that implements a provider or names icons without referencing the components.

## Naming an icon

| Type | What it is |
| --- | --- |
| `Icon` | What an icon draws: a glyph in a variant, a brand mark, or a single character. The hierarchy is closed to the three kinds below. |
| `GlyphIcon` | A `Glyph` in a `GlyphVariant`. |
| `BrandIcon` | A `Brand`. |
| `CharacterIcon` | A `Rune` set by the font. |
| `Glyph` | The marks a provider is asked to draw, named for their recognizable form — `ArrowLeft`, `ChevronDown`, `Folder` — or for that form's conventional meaning where the literal name would be obscure — `Search`, `Delete`, `Settings`. `None` names no glyph. |
| `GlyphVariant` | `Default`, the provider's primary representation, or `Alternate`, its alternate one. |
| `Brand` | The brands whose official mark a provider is asked to draw — `GitHub`, `X`, `YouTube`. `None` names no brand. |
| `IconSize` | The size an icon is drawn at: `Inline` with the surrounding text, `Small` on a button or in a dense row, `Medium` leading a panel or a tile, `Large` as a section's figure, `Display` as the figure of an empty state or a drop zone. |

A bare `Glyph`, `Brand` or `Rune` converts to an `Icon`, and the factories on `Icon` name the
variant:

```csharp
using System.Text;
using Forma.Framework.Icons;

Icon save = Glyph.Save;
Icon heart = Icon.FromGlyph(Glyph.Heart, GlyphVariant.Alternate);
Icon gitHub = Brand.GitHub;
Icon omega = new Rune('Ω');
```

`Icon.FromGlyph` takes the variant as an optional second argument, defaulting to
`GlyphVariant.Default`; `Icon.FromBrand` and `Icon.FromCharacter` are the same conversions as
methods. Icons are records, so two naming the same glyph in the same variant are equal and two
variants of one glyph are not.

## Direction

A glyph whose form points along the reading direction — an arrow or a chevron pointing left or
right, the curve of `Undo` and `Redo`, `Reply` and `Forward`, an arrow leaving a frame as in `Exit`
and `Open`, the marks a list starts with — is declared `GlyphDirectionSensitive` on its `Glyph`
member with the direction it adapts for, as the HTML `dir` token. `glyph.GetDirectionSensitivity()`
reads that direction, or null for a glyph that reads the same either way, and
`glyph.IsDirectionSensitive("rtl")` asks about one direction. `FormaIcon` draws such a glyph
turned over where text runs the direction it names, so `ArrowLeft` means back in both directions
and a component never turns a glyph itself. A glyph named for a side, `AlignLeft` or `LayoutLeft`, keeps
that side, as do a physical object such as `Search`, a clockwise motion such as `Refresh`, a media
control such as `TriangleRight`, and anything carrying letters or digits.

```csharp
Glyph.ArrowLeft.GetDirectionSensitivity();    // "rtl"
Glyph.ArrowLeft.IsDirectionSensitive("rtl");  // true
Glyph.AlignLeft.IsDirectionSensitive("rtl");  // false
```

## Artwork

| Type | What it is |
| --- | --- |
| `IconArtwork` | The artwork used to draw an icon. `IconArtwork.None` is nothing to draw, for an icon the provider cannot depict. The hierarchy is closed to the three kinds below. |
| `IconVector` | Artwork drawn as SVG geometry: a `ViewBox` and the `Paths` composing it, in painting order. |
| `BrandIconArtwork` | An `IconVector` carrying the brand's name as `Title`, for the mark's accessible name. |
| `IconCharacter` | Artwork drawn as a single character by the font: its `Value`. |
| `IconPath` | One path of vector artwork: its SVG path `Data`, its `Opacity` (1 by default), and the `StrokeWidth` it is drawn with in the artwork's own units — zero fills the path instead. |

A provider builds artwork through the factories on `IconArtwork`: `Vector(viewBox, paths)`,
`Brand(viewBox, paths, title)` and `Character(value)`.

## The provider

| Member | What it does |
| --- | --- |
| `bool TryGetArtwork(Icon icon, IconSize size, out IconArtwork artwork)` | Attempts to get the artwork for an icon at a size. Returns whether artwork exists for the icon exactly as requested; `artwork` is `IconArtwork.None` when it does not. |
| `IconArtwork GetArtwork(Icon icon, IconSize size)` | Gets the artwork for an icon at a size, and throws `ArgumentOutOfRangeException` when the icon has no artwork. |

The size lets a provider answer with artwork drawn for that size — a bolder grid for a small
box, say — while a provider with one drawing per icon answers the same artwork at every size.

## Implementing a provider

A provider answers the icons it carries and declines the rest with `IconArtwork.None`:

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

Register it as the `IIconProvider` before `AddFormaUI()`, which registers Forma's own provider
only when the host registered none:

```csharp
builder.Services.AddSingleton<IIconProvider, HouseIconProvider>();
builder.Services.AddFormaUI();
```

`FormaIcon` also takes a provider for one icon through its `Provider` parameter, or for a subtree
through a cascading value of type `IIconProvider`; the registered provider answers whatever such a
provider declines.

## Reading artwork

A consumer asks the provider and draws by the kind of artwork it gets back. `BrandIconArtwork`
derives from `IconVector`, so it is matched first:

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

## License

MIT, under the repository's [`LICENSE.md`](../../../LICENSE.md).
