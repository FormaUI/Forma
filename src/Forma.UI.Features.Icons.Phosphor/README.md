# Forma.UI.Features.Icons.Phosphor

The icon artwork pack for `Forma.UI.Blazor`. It draws every `Glyph` the library names as inline
SVG, from [Phosphor Icons](https://phosphoricons.com) artwork embedded in the assembly — so a host
loads no icon font and fetches nothing at run time.

## Install and register

```pwsh
dotnet add package 2Site.Forma.UI.Features.Icons.Phosphor
```

```csharp
using Forma.UI.Features.Icons.Phosphor;

builder.Services.AddFormaPhosphorPack();
```

That registers the pack's `IIconProvider`, which `FormaIcon` resolves its artwork through.
Registration uses `TryAdd`, so a host that registered its own provider first keeps it.

## Without this package

`FormaIcon` still renders: glyphs with an honest Unicode equivalent are drawn as characters from
the reader's own fonts, and the rest draw nothing. This pack is what turns the full set into
consistent vector artwork. It is one interchangeable artwork set among possible others — a host
preferring different artwork implements `IIconProvider` itself and registers it instead.

## Artwork and license

The artwork is [Phosphor Icons](https://phosphoricons.com), used under the MIT license reproduced
in [`THIRD-PARTY-NOTICES.md`](../../THIRD-PARTY-NOTICES.md), which ships inside this package. No
Phosphor source is redistributed; only the path data for the glyphs the library draws.

## Regenerating the artwork

`scripts/fetch-glyphs.ps1` reads the hand-maintained glyph-to-Phosphor map in
`assets/phosphor.json` and writes the artwork to `Assets/glyphs.json` in this project. Run it after
adding a `Glyph` to the design vocabulary or moving to a newer Phosphor release.
