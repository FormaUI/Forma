namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>The inline edge of its anchor a popover lines up with.</summary>
public enum PopoverAlignment
{
    /// <summary>The edge away from the nearer side of the viewport.</summary>
    /// <remarks>A popover over an anchor in the left half extends right, one in the right half extends left, and one at dead center extends toward the reading start.</remarks>
    Auto,

    /// <summary>The anchor's reading-start edge.</summary>
    Start,

    /// <summary>The anchor's reading-end edge.</summary>
    End,
}
