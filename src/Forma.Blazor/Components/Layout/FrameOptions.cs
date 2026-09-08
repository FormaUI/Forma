namespace Forma.Blazor.Components.Layout;

using Forma.Blazor.Models;

/// <summary>What the reader may do to a <see cref="FormaFrame"/> from its drag region and edges, how far, and where it is held.</summary>
public sealed record FrameOptions
{
    /// <summary>The defaults: movable and resizable, held within the viewport, with no limits of its own.</summary>
    public static FrameOptions Default { get; } = new();

    /// <summary>Whether the frame can be moved by its drag region and the arrow keys.</summary>
    public bool Movable { get; init; } = true;

    /// <summary>Whether the frame can be resized at its edges and corners, and with Shift and the arrow keys.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>The smallest the frame can be resized to; unset, the stylesheet's minimum holds.</summary>
    public Size? MinResizeSize { get; init; }

    /// <summary>The largest the frame can be resized to; unset, its bounds are the limit.</summary>
    public Size? MaxResizeSize { get; init; }

    /// <summary>The region the frame is moved, resized, restored and maximized within.</summary>
    public FrameBounds Bounds { get; init; } = FrameBounds.Viewport;
}
