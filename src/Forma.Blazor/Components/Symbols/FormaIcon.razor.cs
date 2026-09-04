namespace Forma.Blazor.Components.Symbols;

using System.Globalization;

using Forma.Framework.Icons.Glyphs;
using Forma.Framework.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an icon as inline SVG, or as a single character set by the font, so no icon font has to be loaded.</summary>
/// <param name="registeredProvider">The host's registered provider, or null when it registered none.</param>
public sealed partial class FormaIcon(IIconProvider? registeredProvider = null) : ComponentBase
{
    /// <summary>One vector path as the markup writes it: its data, its stroke width or null for a filled path, and its opacity.</summary>
    private readonly record struct VectorPathView(string Data, string? StrokeWidth, string? Opacity);

    private IconArtwork _artwork = default!;
    private VectorPathView[] _vectorPaths = [];
    private string? _characterText;

    /// <summary>The icon to draw.</summary>
    [Parameter, EditorRequired] public Icon Icon { get; set; } = default!;

    /// <summary>The size to draw at, which selects both the icon's box and the artwork drawn for that box.</summary>
    [Parameter] public IconSize Size { get; set; }

    /// <summary>Extra classes applied to the rendered element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The provider supplying this icon's artwork, taken from the surrounding subtree or the host's registration when unset.</summary>
    [Parameter] public IIconProvider? Provider { get; set; }

    [CascadingParameter] private IIconProvider? CascadingProvider { get; set; }

    private string CssClass
    {
        get
        {
            var sizeClass = Size switch
            {
                IconSize.Small => " forma-icon-sm",
                IconSize.Medium => " forma-icon-md",
                IconSize.Large => " forma-icon-lg",
                IconSize.Display => " forma-icon-display",
                _ => string.Empty,
            };

            return string.IsNullOrWhiteSpace(Class) ? $"forma-icon{sizeClass}" : $"forma-icon{sizeClass} {Class}";
        }
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Icon);

        // Nearest wins: this icon's own provider, then the one a surrounding subtree draws from, then the host's
        // registration; the library's own artwork answers whatever none of those can.
        var provider = Provider
            ?? CascadingProvider
            ?? registeredProvider;

        _artwork = provider is not null && provider.TryGetArtwork(Icon, Size, out var supplied)
            ? supplied
            : FormaIconProvider.Default.TryGetArtwork(Icon, Size, out var bundled) ? bundled : IconArtwork.None;

        // The path attributes are written once here rather than formatted again on every render.
        _vectorPaths = _artwork is IconVector vector
            ? [.. vector.Paths.Select(path => new VectorPathView(path.Data, path.StrokeWidth > 0 ? Width(path) : null, Fade(path)))]
            : [];
        _characterText = _artwork is IconCharacter character ? character.Value.ToString() : null;
    }

    private static string Width(IconPath path)
        => path.StrokeWidth.ToString("0.###", CultureInfo.InvariantCulture);

    private static string? Fade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;
}
