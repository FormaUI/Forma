namespace Forma.UI.Blazor.Design;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.UI.Design.Icons;

/// <summary>Provides the library's own artwork for every glyph, read from the glyph set embedded in this assembly.</summary>
public sealed class FormaIconProvider : IIconProvider
{
    private const string ResourceName = "Forma.UI.Blazor.Assets.glyphs.json";

    private static readonly Lazy<FrozenDictionary<Glyph, IconArtwork[]>> Set = new(Load);

    /// <summary>The shared provider, which holds no state of its own.</summary>
    public static FormaIconProvider Default { get; } = new();

    /// <inheritdoc />
    public bool TryGetArtwork(Icon icon, out IconArtwork artwork)
    {
        if (icon.Glyph is Glyph.None)
        {
            throw new ArgumentOutOfRangeException(nameof(icon), "Glyph.None names no artwork.");
        }

        var variant = (int)icon.Variant;

        if (Set.Value.TryGetValue(icon.Glyph, out var variants)
            && (uint)variant < (uint)variants.Length
            && variants[variant] is { } found)
        {
            artwork = found;
            return true;
        }

        artwork = IconArtwork.None;
        return false;
    }

    /// <inheritdoc />
    public IconArtwork GetArtwork(Icon icon)
    {
        if (!TryGetArtwork(icon, out var artwork))
        {
            throw new ArgumentOutOfRangeException(nameof(icon), $"No artwork is embedded for {icon.Glyph} at {icon.Variant}; rebuild it with scripts/Build-GlyphPack.ps1.");
        }

        return artwork;
    }

    private static FrozenDictionary<Glyph, IconArtwork[]> Load()
    {
        using var stream = typeof(FormaIconProvider).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The glyph resource '{ResourceName}' is missing; rebuild it with scripts/Build-GlyphPack.ps1.");

        var raw = JsonSerializer.Deserialize(stream, GlyphSetJsonContext.Default.RawGlyphSet)
            ?? throw new InvalidOperationException($"The glyph resource '{ResourceName}' could not be read.");

        var variantCount = Enum.GetValues<GlyphVariant>().Length;
        var set = new Dictionary<Glyph, IconArtwork[]>(raw.Glyphs.Count);

        foreach (var (name, byStyle) in raw.Glyphs)
        {
            if (!Enum.TryParse<Glyph>(name, out var glyph) || glyph is Glyph.None)
            {
                continue;
            }

            var variants = new IconArtwork[variantCount];

            foreach (var (styleName, style) in byStyle)
            {
                if (TryMapVariant(styleName, out var variant))
                {
                    variants[(int)variant] = IconArtwork.Vector(
                        raw.ViewBox,
                        [.. style.Paths.Select(path => new IconPath
                        {
                            Data = path.D,
                            Opacity = path.O ?? 1,
                            StrokeWidth = path.F is true ? 0 : style.Stroke,
                        })]);
                }
            }

            set[glyph] = variants;
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

/// <summary>The glyph set as scripts/Build-GlyphPack.ps1 writes it.</summary>
internal sealed record RawGlyphSet(
    [property: JsonPropertyName("viewBox")] string ViewBox,
    [property: JsonPropertyName("glyphs")] Dictionary<string, Dictionary<string, RawGlyphStyle>> Glyphs);

/// <summary>One style of one glyph: the stroke width its paths are drawn with, zero for a filled drawing, and the paths.</summary>
internal sealed record RawGlyphStyle(
    [property: JsonPropertyName("stroke")] double Stroke,
    [property: JsonPropertyName("paths")] RawGlyphPath[] Paths);

/// <summary>One path of one glyph: its data, an optional opacity, and whether it is filled inside a stroke drawing.</summary>
internal sealed record RawGlyphPath(
    [property: JsonPropertyName("d")] string D,
    [property: JsonPropertyName("o")] double? O,
    [property: JsonPropertyName("f")] bool? F);

[JsonSerializable(typeof(RawGlyphSet))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class GlyphSetJsonContext : JsonSerializerContext;
