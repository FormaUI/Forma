namespace Forma.Blazor.Components.Windows;

using Microsoft.AspNetCore.Components;

/// <summary>The region a floating window is held within: the viewport, or an element of the page.</summary>
public sealed record WindowBounds
{
    private WindowBounds(ElementReference? element)
    {
        Element = element;
    }

    /// <summary>The browser's visible content area.</summary>
    public static WindowBounds Viewport { get; } = new(element: null);

    /// <summary>The element the window is held within, or <see langword="null"/> for the viewport.</summary>
    public ElementReference? Element { get; }

    /// <summary>The region of one element of the page.</summary>
    public static WindowBounds Container(ElementReference element)
        => new(element);
}
