namespace Forma.Blazor.Components.Layout;

/// <summary>The look a <see cref="FormaFrame"/> takes. Every frame has exactly one, and none draws a line.</summary>
public enum FrameVariant
{
    /// <summary>A frame lying in its parent, told apart from the frame beside it by tone alone.</summary>
    Flat,

    /// <summary>A frame standing off the canvas as a card: the corner radius, the small shadow, and its content clipped to its corners.</summary>
    Raised,

    /// <summary>A frame set back a tone from the frame around it: a header strip, a toolbar band, a table header.</summary>
    Inset,

    /// <summary>The frame other frames are laid on: the canvas tone, with one spacing step of it showing around what it holds.</summary>
    Canvas,
}
