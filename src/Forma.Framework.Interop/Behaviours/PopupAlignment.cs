namespace Forma.Framework.Interop.Behaviours;

/// <summary>The inline edge of its anchor a popup lines up with.</summary>
public enum PopupAlignment
{
    /// <summary>The edge away from the nearer side of the viewport, so the popup extends toward the room: an anchor in the left half extends right, one in the right half extends left, and one at dead center extends toward the reading start.</summary>
    Auto,

    /// <summary>The anchor's reading-start edge.</summary>
    Start,

    /// <summary>The anchor's reading-end edge.</summary>
    End,
}
