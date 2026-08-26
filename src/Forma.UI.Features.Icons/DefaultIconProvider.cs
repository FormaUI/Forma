namespace Forma.UI.Features.Icons;

using System.Collections.Frozen;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Forma.UI.Design.Icons;

/// <summary>Provides the artwork this library ships, read from its embedded glyph set.</summary>
public sealed class DefaultIconProvider : IIconProvider
{
    private const string ResourceName = "Forma.UI.Features.Icons.Assets.glyphs.json";

    private static readonly Lazy<FrozenDictionary<Glyph, IconArtwork[]>> Set = new(Load);

    /// <inheritdoc />
    public IconArtwork GetArtwork(Icon icon)
    {
        if (icon.Glyph is Glyph.None)
        {
            throw new ArgumentOutOfRangeException(nameof(icon), "Glyph.None names no artwork.");
        }

        if (!Set.Value.TryGetValue(icon.Glyph, out var styles))
        {
            throw new ArgumentOutOfRangeException(nameof(icon), $"No artwork is embedded for {icon.Glyph}; regenerate it with scripts/fetch-glyphs.ps1.");
        }

        var style = (int)icon.Style;

        if ((uint)style >= (uint)styles.Length || styles[style] is not { } artwork)
        {
            throw new ArgumentOutOfRangeException(nameof(icon), $"{icon.Glyph} is not drawn at {icon.Style}.");
        }

        return artwork;
    }

    private static FrozenDictionary<Glyph, IconArtwork[]> Load()
    {
        using var stream = typeof(DefaultIconProvider).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The glyph resource '{ResourceName}' is missing; regenerate it with scripts/fetch-glyphs.ps1.");

        var raw = JsonSerializer.Deserialize(stream, GlyphSetJsonContext.Default.RawGlyphSet)
            ?? throw new InvalidOperationException($"The glyph resource '{ResourceName}' could not be read.");

        var styleCount = Enum.GetValues<GlyphStyle>().Length;
        var set = new Dictionary<Glyph, IconArtwork[]>(raw.Glyphs.Count);

        foreach (var (name, byStyle) in raw.Glyphs)
        {
            if (!Enum.TryParse<Glyph>(name, out var glyph) || glyph is Glyph.None)
            {
                continue;
            }

            var styles = new IconArtwork[styleCount];

            foreach (var (styleName, paths) in byStyle)
            {
                if (Enum.TryParse<GlyphStyle>(styleName, ignoreCase: true, out var style))
                {
                    styles[(int)style] = IconArtwork.Vector(
                        raw.ViewBox,
                        [.. paths.Select(path => new IconPath { Data = path.D, Opacity = path.O ?? 1 })]);
                }
            }

            set[glyph] = styles;
        }

        return set.ToFrozenDictionary();
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
