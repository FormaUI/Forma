namespace Forma.UI.Blazor.Components;

using System.Globalization;

using Forma.UI.Style.Icons;

using Microsoft.AspNetCore.Components;

/// <summary>Renders an icon as inline SVG, so no icon font has to be loaded.</summary>
public sealed partial class FormaIcon : ComponentBase
{
    private IconArtwork _artwork = default!;

    /// <summary>The icon to draw.</summary>
    [Parameter, EditorRequired] public Icon Icon { get; set; }

    /// <summary>Extra classes applied to the SVG element.</summary>
    [Parameter] public string? Class { get; set; }

    [Inject] private IIconProvider Provider { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-icon" : $"forma-icon {Class}";

    /// <inheritdoc />
    protected override void OnParametersSet()
        => _artwork = Provider.GetIcon(Icon);

    private static string? Fade(IconPath path)
        => path.Opacity < 1 ? path.Opacity.ToString("0.###", CultureInfo.InvariantCulture) : null;
}
