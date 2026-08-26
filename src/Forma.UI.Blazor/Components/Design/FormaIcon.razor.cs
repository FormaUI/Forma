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

    [Inject] private IServiceProvider Services { get; set; } = default!;

    // Absent registration is a host that wants no artwork package, not a fault; the reader's own fonts stand in.
    private IIconProvider Provider
        => (IIconProvider?)Services.GetService(typeof(IIconProvider)) ?? UnicodeIconProvider.Default;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-icon" : $"forma-icon {Class}";

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _artwork = Provider.GetArtwork(Icon);

    private static string? Fade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;
}
