namespace Forma.Blazor.Components.Symbols;

using System.Globalization;

using Forma.Framework.Css;
using Forma.Framework.Icons;
using Forma.Framework.Icons.Glyphs;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an icon as inline SVG or as a single character set by the font.</summary>
/// <param name="iconProvider">The registered icon provider, where the bundled glyphs draw an icon none is registered for.</param>
public sealed partial class FormaIcon(IIconProvider? iconProvider = null) : FormaElementBase
{
    private readonly IIconProvider _iconProvider = iconProvider ?? GlyphIconProvider.Instance;

    /// <summary>One vector path as the markup writes it: its data, its stroke width or null for a filled path, and its opacity.</summary>
    private readonly record struct VectorPathView(string Data, string? StrokeWidth, string? Opacity);

    private IconArtwork _artwork = default!;
    private VectorPathView[] _vectorPaths = [];
    private string? _characterText;

    /// <summary>The icon to draw.</summary>
    [Parameter, EditorRequired] public Icon Icon { get; set; } = default!;

    /// <summary>The size to draw at, which selects both the icon's box and the artwork drawn for that box.</summary>
    [Parameter] public IconSize Size { get; set; }

    /// <summary>The provider supplying this icon's artwork, where null takes the surrounding subtree's provider or the registered one.</summary>
    [Parameter] public IIconProvider? Provider { get; set; }

    /// <summary>The accessible name that makes the icon an image a reader announces, where null leaves it decorative.</summary>
    [Parameter] public string? Label { get; set; }

    [CascadingParameter] private IIconProvider? CascadingProvider { get; set; }

    // An icon is decorative unless it is named, so a reader hears the control or the text beside it once.
    private string? Role
        => Label is null ? null : "img";

    private string? AriaHidden
        => Label is null ? "true" : null;

    // Only a glyph drawn as geometry turns; the browser mirrors a character itself, and a brand mark never turns.
    private string? Direction
        => _artwork is IconVector and not BrandIconArtwork && Icon is GlyphIcon glyph ? glyph.Glyph.GetDirectionSensitivity() : null;

    private string? SizeClass
        => Size switch
        {
            IconSize.Small => "forma-icon-sm",
            IconSize.Medium => "forma-icon-md",
            IconSize.Large => "forma-icon-lg",
            IconSize.Display => "forma-icon-display",
            _ => null,
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Icon);

        if (Label is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        }

        // Nearest wins: this icon's own provider, then the surrounding subtree's; the registered one answers what those decline.
        var provider = Provider ?? CascadingProvider;

        _artwork = provider is not null && provider.TryGetArtwork(Icon, Size, out var suppliedArtwork)
            ? suppliedArtwork
            : _iconProvider.TryGetArtwork(Icon, Size, out var registeredArtwork) ? registeredArtwork : IconArtwork.None;

        _vectorPaths = _artwork is IconVector vector
            ? [.. vector.Paths.Select(path => new VectorPathView(path.Data, path.StrokeWidth > 0 ? GetWidth(path) : null, GetFade(path)))]
            : [];
        _characterText = _artwork is IconCharacter character ? character.Value.ToString() : null;
    }

    private static string GetWidth(IconPath path)
        => path.StrokeWidth.ToString("0.###", CultureInfo.InvariantCulture);

    private static string? GetFade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-icon")
            .Add(SizeClass)
            .Add("forma-icon-character", _characterText is not null)
            .Add("forma-icon-logical-direction", Direction is not null);
}
