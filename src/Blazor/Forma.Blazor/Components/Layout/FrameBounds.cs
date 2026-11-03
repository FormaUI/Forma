namespace Forma.Blazor.Components.Layout;

using Microsoft.AspNetCore.Components;

/// <summary>The region a positioned <see cref="FormaFrame"/> is held within: the viewport, or an element of the page.</summary>
public sealed record FrameBounds
{
    private FrameBounds(ElementReference? element)
    {
        Element = element;
    }

    /// <summary>The browser's visible content area.</summary>
    public static FrameBounds Viewport { get; } = new(element: null);

    /// <summary>The element the frame is held within, where <see langword="null"/> is the viewport.</summary>
    public ElementReference? Element { get; }

    /// <summary>The region of one element of the page.</summary>
    public static FrameBounds Container(ElementReference element)
        => new(element);
}
