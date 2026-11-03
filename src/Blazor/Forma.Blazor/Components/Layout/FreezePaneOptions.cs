namespace Forma.Blazor.Components.Layout;

using Forma.Framework.Browser.Interop.Scrolling;

/// <summary>How a freeze pane moves its content: along which axis, by what, and the names its arrows carry.</summary>
public sealed record FreezePaneOptions
{
    /// <summary>The axis the content scrolls along.</summary>
    public ScrollAxis Axis { get; init; } = ScrollAxis.Inline;

    /// <summary>What moves the content.</summary>
    public ScrollNavigation Navigation { get; init; } = ScrollNavigation.Scrollbar;

    /// <summary>Whether a wheel turned over the pane scrolls its inline content, holding the turn from the page.</summary>
    public bool PageOnWheelGesture { get; init; } = true;

    /// <summary>Whether the viewport scrolls along the other axis too.</summary>
    public bool ScrollsCrossAxis { get; init; }

    /// <summary>The accessible name of the arrow scrolling toward the start, where <see langword="null"/> is the pane's own.</summary>
    public string? ScrollBackLabel { get; init; }

    /// <summary>The accessible name of the arrow scrolling toward the end, where <see langword="null"/> is the pane's own.</summary>
    public string? ScrollForwardLabel { get; init; }

    /// <summary>The accessible name of the draggable edge of the frozen region, where <see langword="null"/> is the pane's own.</summary>
    public string? FrozenEdgeLabel { get; init; }

    /// <summary>Inline scrolling by the scrollbar, with the pane's own arrow names.</summary>
    public static FreezePaneOptions Default { get; } = new();

    /// <summary>Inline scrolling by arrows, with the pane's own arrow names.</summary>
    public static FreezePaneOptions Arrows { get; } = new() { Navigation = ScrollNavigation.Arrows };
}
