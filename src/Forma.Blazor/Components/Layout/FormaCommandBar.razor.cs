namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Common.Interop.Behaviours;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>One bar of commands on a scrollable track, with an arrow at whichever end the commands still continue past.</summary>
public sealed partial class FormaCommandBar : ComponentBase
{
    /// <summary>The bar's commands.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Content pinned past the end of the track, staying put while the commands scroll.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>How the bar's track scrolls; the track is always inline and always moved by arrows.</summary>
    [Parameter] public FreezePaneOptions Options { get; set; } = FreezePaneOptions.Arrows;

    /// <summary>Extra classes applied alongside forma-command-bar.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Hides the bar while keeping it mounted, so its track keeps its position and its attached behaviours across toggles.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Animates the transition between hidden and shown instead of snapping instantly.</summary>
    [Parameter] public bool Animated { get; set; }

    /// <summary>Additional attributes applied to the bar element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaCommandBar> L { get; set; } = default!;

    private string CssClass
        => string.IsNullOrWhiteSpace(Class) ? "forma-command-bar" : $"forma-command-bar {Class}";

    private FreezePaneOptions TrackOptions
        => Options with
        {
            Axis = ScrollAxis.Inline,
            Navigation = ScrollNavigation.Arrows,
            ScrollBackLabel = Options.ScrollBackLabel ?? L["Scroll Commands Back"],
            ScrollForwardLabel = Options.ScrollForwardLabel ?? L["Scroll Commands Forward"],
        };

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Options);
}
