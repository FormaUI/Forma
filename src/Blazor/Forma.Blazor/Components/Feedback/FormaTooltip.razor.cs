namespace Forma.Blazor.Components.Feedback;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A short text describing the control that names it in its <c>aria-describedby</c>, shown beside the control on hover, focus and tap.</summary>
/// <remarks>
/// The tooltip holds text alone. The page's tooltip script, which Forma's interop package installs on every page, shows it
/// above the control in the top layer: after a short delay for a resting pointer, at once for the keyboard's focus and for
/// a tap. Escape and a press elsewhere hide it; it stays while the pointer is over the control or the tooltip, or the
/// control holds the focus.
/// </remarks>
public sealed partial class FormaTooltip : FormaElementBase
{
    /// <summary>The text the tooltip shows.</summary>
    [Parameter, EditorRequired] public string Text { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentException.ThrowIfNullOrWhiteSpace(Text);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-tooltip");
}
