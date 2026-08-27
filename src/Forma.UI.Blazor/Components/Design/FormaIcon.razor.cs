namespace Forma.UI.Blazor.Components.Design;

using System.Globalization;

using Forma.UI.Blazor.Design;
using Forma.UI.Design.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an icon as inline SVG, so no icon font has to be loaded.</summary>
public sealed partial class FormaIcon : ComponentBase
{
    private IconArtwork _artwork = default!;

    /// <summary>The icon to draw.</summary>
    [Parameter, EditorRequired] public Icon Icon { get; set; }

    /// <summary>Extra classes applied to the SVG element.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>The provider supplying this icon's artwork, taken from the surrounding subtree or the host's registration when unset.</summary>
    [Parameter] public IIconProvider? Provider { get; set; }

    [Inject] private IServiceProvider Services { get; set; } = default!;

    [CascadingParameter] private IIconProvider? CascadingProvider { get; set; }

    private string CssClass
        => (string.IsNullOrWhiteSpace(Class) ? "forma-icon" : $"forma-icon {Class}") + _artwork switch
        {
            IconCharacter when Icon.Variant is GlyphVariant.Alternate => " forma-icon-character forma-icon-character-alternate",
            IconCharacter => " forma-icon-character",
            _ => string.Empty,
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Nearest wins: this icon's own provider, then the one a surrounding subtree draws from, then the host's
        // registration. Absent all three is a host that wants no artwork package, not a fault, and the bundled
        // symbol font stands in.
        var provider = Provider
            ?? CascadingProvider
            ?? (IIconProvider?)Services.GetService(typeof(IIconProvider))
            ?? UnicodeIconProvider.Default;

        _artwork = provider.TryGetArtwork(Icon, out var artwork) ? artwork : IconArtwork.None;
    }

    private static string? Fade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;
}
