namespace Forma.Framework.Icons.Glyphs;

using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.Framework.Icons;

/// <summary>Provides the library's own artwork: every glyph and brand mark embedded in this assembly, and any character.</summary>
public sealed class FormaIconProvider : IIconProvider
{
    private const string GlyphResourceName = "Forma.Framework.Icons.Glyphs._assets.glyphs.json";
    private const string BrandResourceName = "Forma.Framework.Icons.Glyphs._assets.brands.json";

    private static readonly Lazy<FrozenDictionary<(Glyph Glyph, int Grid), IconArtwork[]>> Glyphs = new(LoadGlyphs);
    private static readonly Lazy<FrozenDictionary<Brand, IconArtwork>> Brands = new(LoadBrands);

    /// <summary>The shared provider, which holds no state of its own.</summary>
    public static FormaIconProvider Default { get; } = new();

    /// <inheritdoc />
    public bool TryGetArtwork(Icon icon, IconSize size, out IconArtwork artwork)
    {
        ArgumentNullException.ThrowIfNull(icon);

        switch (icon)
        {
            case GlyphIcon { Glyph: Glyph.None }:
                throw new ArgumentOutOfRangeException(nameof(icon), "Glyph.None names no artwork.");
            case BrandIcon { Brand: Brand.None }:
                throw new ArgumentOutOfRangeException(nameof(icon), "Brand.None names no artwork.");
            case GlyphIcon glyph:
                return TryGetGlyph(glyph, size, out artwork);
            case BrandIcon brand:
                if (Brands.Value.TryGetValue(brand.Brand, out var mark))
                {
                    artwork = mark;
                    return true;
                }

                artwork = IconArtwork.None;
                return false;
            case CharacterIcon character:
                artwork = IconArtwork.Character(character.Character);
                return true;
            default:
                artwork = IconArtwork.None;
                return false;
        }
    }

    /// <inheritdoc />
    public IconArtwork GetArtwork(Icon icon, IconSize size)
    {
        if (!TryGetArtwork(icon, size, out var artwork))
        {
            throw new ArgumentOutOfRangeException(nameof(icon), $"No artwork is embedded for {icon}; rebuild it with scripts/Build-GlyphPack.ps1 or scripts/Build-BrandPack.ps1.");
        }

        return artwork;
    }

    private static bool TryGetGlyph(GlyphIcon icon, IconSize size, out IconArtwork artwork)
    {
        var index = (int)icon.Variant;

        foreach (var grid in GridsFor(size))
        {
            if (Glyphs.Value.TryGetValue((icon.Glyph, grid), out var variants)
                && (uint)index < (uint)variants.Length
                && variants[index] is { } found)
            {
                artwork = found;
                return true;
            }
        }

        artwork = IconArtwork.None;
        return false;
    }

    // The grid whose stroke lands on whole pixels at the size, then the other as a fallback.
    private static ReadOnlySpan<int> GridsFor(IconSize size)
        => size is IconSize.Small or IconSize.Large ? [24, 32] : [32, 24];

    private static Stream OpenResource(string name, string script)
        => typeof(FormaIconProvider).GetTypeInfo().Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"The resource '{name}' is missing; rebuild it with {script}.");

    private static FrozenDictionary<(Glyph, int), IconArtwork[]> LoadGlyphs()
    {
        using var stream = OpenResource(GlyphResourceName, "scripts/Build-GlyphPack.ps1");

        var raw = JsonSerializer.Deserialize(stream, IconSetJsonContext.Default.RawGlyphSet)
            ?? throw new InvalidOperationException($"The resource '{GlyphResourceName}' could not be read.");

        var variantCount = Enum.GetValues<GlyphVariant>().Length;
        var set = new Dictionary<(Glyph, int), IconArtwork[]>(raw.Glyphs.Count);

        foreach (var (name, byGrid) in raw.Glyphs)
        {
            if (!Enum.TryParse<Glyph>(name, out var glyph) || glyph is Glyph.None)
            {
                continue;
            }

            foreach (var (gridName, byStyle) in byGrid)
            {
                var grid = int.Parse(gridName, CultureInfo.InvariantCulture);
                var variants = new IconArtwork[variantCount];

                foreach (var (styleName, style) in byStyle)
                {
                    if (TryMapVariant(styleName, out var variant))
                    {
                        variants[(int)variant] = IconArtwork.Vector(
                            $"0 0 {grid} {grid}",
                            [.. style.Paths.Select(path => new IconPath
                            {
                                Data = path.D,
                                Opacity = path.O ?? 1,
                                StrokeWidth = path.F is true ? 0 : style.Stroke,
                            })]);
                    }
                }

                set[(glyph, grid)] = variants;
            }
        }

        return set.ToFrozenDictionary();
    }

    private static FrozenDictionary<Brand, IconArtwork> LoadBrands()
    {
        using var stream = OpenResource(BrandResourceName, "scripts/Build-BrandPack.ps1");

        var raw = JsonSerializer.Deserialize(stream, IconSetJsonContext.Default.RawBrandSet)
            ?? throw new InvalidOperationException($"The resource '{BrandResourceName}' could not be read.");

        var set = new Dictionary<Brand, IconArtwork>(raw.Brands.Count);

        foreach (var (name, mark) in raw.Brands)
        {
            if (Enum.TryParse<Brand>(name, out var brand) && brand is not Brand.None)
            {
                set[brand] = IconArtwork.Brand(mark.ViewBox, [new IconPath { Data = mark.D }], mark.Title);
            }
        }

        return set.ToFrozenDictionary();
    }

    private static bool TryMapVariant(string styleName, out GlyphVariant variant)
    {
        if (string.Equals(styleName, "regular", StringComparison.OrdinalIgnoreCase))
        {
            variant = GlyphVariant.Default;
            return true;
        }

        if (string.Equals(styleName, "filled", StringComparison.OrdinalIgnoreCase))
        {
            variant = GlyphVariant.Alternate;
            return true;
        }

        variant = default;
        return false;
    }
}

/// <summary>The glyph set as scripts/Build-GlyphPack.ps1 writes it: glyph name, then grid size, then style name.</summary>
internal sealed record RawGlyphSet(
    [property: JsonPropertyName("glyphs")] Dictionary<string, Dictionary<string, Dictionary<string, RawGlyphStyle>>> Glyphs);

/// <summary>One style of one glyph: the stroke width its paths are drawn with, zero for a filled drawing, and the paths.</summary>
internal sealed record RawGlyphStyle(
    [property: JsonPropertyName("stroke")] double Stroke,
    [property: JsonPropertyName("paths")] RawGlyphPath[] Paths);

/// <summary>One path of one glyph: its data, an optional opacity, and whether it is filled inside a stroke drawing.</summary>
internal sealed record RawGlyphPath(
    [property: JsonPropertyName("d")] string D,
    [property: JsonPropertyName("o")] double? O,
    [property: JsonPropertyName("f")] bool? F);

/// <summary>The brand set as scripts/Build-BrandPack.ps1 writes it: brand name, then its mark.</summary>
internal sealed record RawBrandSet(
    [property: JsonPropertyName("brands")] Dictionary<string, RawBrandMark> Brands);

/// <summary>One brand mark: its view box, its single solid path, and the brand's name.</summary>
internal sealed record RawBrandMark(
    [property: JsonPropertyName("viewBox")] string ViewBox,
    [property: JsonPropertyName("d")] string D,
    [property: JsonPropertyName("title")] string Title);

[JsonSerializable(typeof(RawGlyphSet))]
[JsonSerializable(typeof(RawBrandSet))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class IconSetJsonContext : JsonSerializerContext;
