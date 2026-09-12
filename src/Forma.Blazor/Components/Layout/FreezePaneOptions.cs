namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Common.Interop.Behaviors;

/// <summary>How a freeze pane moves its content: along which axis, by what, and the names its arrows carry.</summary>
public sealed record FreezePaneOptions
{
    /// <summary>The axis the content scrolls along.</summary>
    public ScrollAxis Axis { get; init; } = ScrollAxis.Inline;

    /// <summary>What moves the content: the browser's scrollbar, or arrows shown only where there is somewhere to go.</summary>
    public ScrollNavigation Navigation { get; init; } = ScrollNavigation.Scrollbar;

    /// <summary>Scrolls inline content with a wheel turned anywhere over the pane, keeping the turn from the page while the pointer stays there.</summary>
    public bool PageOnWheelGesture { get; init; } = true;

    /// <summary>Accessible name for the arrow scrolling toward the start, replacing the pane's own.</summary>
    public string? ScrollBackLabel { get; init; }

    /// <summary>Accessible name for the arrow scrolling toward the end, replacing the pane's own.</summary>
    public string? ScrollForwardLabel { get; init; }

    /// <summary>Accessible name for the draggable edge of the frozen region, replacing the pane's own.</summary>
    public string? FrozenEdgeLabel { get; init; }

    /// <summary>Inline scrolling by the scrollbar, with the pane's own arrow names.</summary>
    public static FreezePaneOptions Default { get; } = new();

    /// <summary>Inline scrolling by arrows, with the pane's own arrow names.</summary>
    public static FreezePaneOptions Arrows { get; } = new() { Navigation = ScrollNavigation.Arrows };
}
