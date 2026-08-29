namespace Forma.UI.Blazor.Components.Design;

using System.Globalization;

using Forma.UI.Blazor.Design;
using Forma.UI.Design.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an icon as inline SVG, or as a single character set by the font, so no icon font has to be loaded.</summary>
public sealed partial class FormaIcon : ComponentBase
{
    private IconArtwork _artwork = default!;

    /// <summary>The icon to draw.</summary>
    [Parameter, EditorRequired] public Icon Icon { get; set; } = default!;

    /// <summary>The size to draw at, which selects both the icon's box and the artwork drawn for that box.</summary>
    [Parameter] public IconSize Size { get; set; }

    /// <summary>Extra classes applied to the rendered element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The provider supplying this icon's artwork, taken from the surrounding subtree or the host's registration when unset.</summary>
    [Parameter] public IIconProvider? Provider { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

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
            ?? (IIconProvider?)Services.GetService(typeof(IIconProvider));

        _artwork = provider is not null && provider.TryGetArtwork(Icon, Size, out var supplied)
            ? supplied
            : FormaIconProvider.Default.TryGetArtwork(Icon, Size, out var bundled) ? bundled : IconArtwork.None;
    }

    private static string Width(IconPath path)
        => path.StrokeWidth.ToString("0.###", CultureInfo.InvariantCulture);

    private static string? Fade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;
}
