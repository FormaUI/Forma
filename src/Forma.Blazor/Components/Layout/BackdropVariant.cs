namespace Forma.Blazor.Components.Layout;

/// <summary>The look a <see cref="FormaBackdrop"/> takes. Every backdrop has exactly one, and none draws a line.</summary>
public enum BackdropVariant
{
    /// <summary>A backdrop lying in its parent, told apart from the backdrop beside it by tone alone.</summary>
    Flat,

    /// <summary>A backdrop standing off the canvas as a card: the corner radius, the small shadow, and its content clipped to its corners.</summary>
    Raised,

    /// <summary>A backdrop set back a tone from the backdrop around it: a header strip, a toolbar band, a table header.</summary>
    Inset,

    /// <summary>The backdrop other backdrops are laid on: the canvas tone, with one spacing step of it showing around what it holds.</summary>
    Canvas,
}
