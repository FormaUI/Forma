namespace Forma.UI.Features.Icons.Phosphor;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.UI.Design.Icons;

/// <summary>Provides the Phosphor artwork this package ships, read from its embedded glyph set.</summary>
public sealed class PhosphorIconProvider : IIconProvider
{
    private const string ResourceName = "Forma.UI.Features.Icons.Phosphor.Assets.glyphs.json";

    private static readonly Lazy<FrozenDictionary<Glyph, IconArtwork[]>> Set = new(Load);

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
            throw new ArgumentOutOfRangeException(nameof(icon), $"No artwork is embedded for {icon.Glyph} at {icon.Variant}; regenerate it with scripts/fetch-glyphs.ps1.");
        }

        return artwork;
    }

    private static FrozenDictionary<Glyph, IconArtwork[]> Load()
    {
        using var stream = typeof(PhosphorIconProvider).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The glyph resource '{ResourceName}' is missing; regenerate it with scripts/fetch-glyphs.ps1.");

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

            foreach (var (styleName, paths) in byStyle)
            {
                if (TryMapVariant(styleName, out var variant))
                {
                    variants[(int)variant] = IconArtwork.Vector(
                        raw.ViewBox,
                        [.. paths.Select(path => new IconPath { Data = path.D, Opacity = path.O ?? 1 })]);
                }
            }

            set[glyph] = variants;
        }

        return set.ToFrozenDictionary();
    }

    // The glyph set speaks Phosphor's own vocabulary for its cuts; the outline is the primary representation.
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

/// <summary>The glyph set as scripts/fetch-glyphs.ps1 writes it.</summary>
internal sealed record RawGlyphSet(
    [property: JsonPropertyName("viewBox")] string ViewBox,
    [property: JsonPropertyName("glyphs")] Dictionary<string, Dictionary<string, RawGlyphPath[]>> Glyphs);

/// <summary>One path of one glyph as scripts/fetch-glyphs.ps1 writes it.</summary>
internal sealed record RawGlyphPath(
    [property: JsonPropertyName("d")] string D,
    [property: JsonPropertyName("o")] double? O);

[JsonSerializable(typeof(RawGlyphSet))]
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
internal sealed partial class GlyphSetJsonContext : JsonSerializerContext;
