namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>What an observation of a scroll container's <see cref="ScrollState"/> measures.</summary>
public sealed record ScrollWatchOptions
{
    /// <summary>The axis the container scrolls along.</summary>
    public ScrollAxis Axis { get; init; } = ScrollAxis.Inline;

    /// <summary>The selector of the parts that stay at the container's start.</summary>
    public string? FrozenSelector { get; init; }

    /// <summary>Whether the state carries the container's visible extent along the axis.</summary>
    public bool ReportsViewportExtent { get; init; }
}
