namespace Forma.Blazor.Components.Layout;

/// <summary>The element a <see cref="FormaFrame"/> writes its position and size to, cascaded to the frame by the parent that presents it; the frame cascades <see cref="Self"/> on to what it holds.</summary>
public enum FrameBox
{
    /// <summary>The frame's own element, which stands in the viewport once it has a placement or is maximized.</summary>
    Self,

    /// <summary>The parent element that presents the frame, which the frame fills and which takes every move, resize and placement the frame's drag region and handles produce.</summary>
    Parent,
}
