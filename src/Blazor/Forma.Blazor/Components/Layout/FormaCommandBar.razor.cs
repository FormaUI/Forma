namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>One bar of commands on a scrollable track, with an arrow at each end the commands continue past.</summary>
public sealed partial class FormaCommandBar : FormaElementBase
{
    /// <summary>The bar's commands.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The content pinned past the end of the track.</summary>
    [Parameter] public RenderFragment? TrailingContent { get; set; }

    /// <summary>How the bar's inline, arrow-driven track scrolls.</summary>
    [Parameter] public FreezePaneOptions Options { get; set; } = FreezePaneOptions.Arrows;

    /// <summary>Whether the bar is hidden, kept mounted with its track's position and behaviors.</summary>
    [Parameter] public bool Hidden { get; set; }

    /// <summary>Whether the bar animates between hidden and shown.</summary>
    [Parameter] public bool Animated { get; set; }

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

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-command-bar");
}
