namespace Forma.Blazor.Components.Windows;

using Forma.Blazor.Models;

/// <summary>What a <see cref="FormaWindow"/> lets the reader do to it from its title bar and edges, and how far.</summary>
public sealed record WindowOptions
{
    /// <summary>The defaults: closable, maximizable, minimizable, dockable and resizable, held within the viewport, with no limits of its own and a press away ignored.</summary>
    public static WindowOptions Default { get; } = new();

    /// <summary>Whether the title bar offers a close command.</summary>
    public bool Closable { get; init; } = true;

    /// <summary>Whether a floating window can fill the viewport and return.</summary>
    public bool Maximizable { get; init; } = true;

    /// <summary>Whether the title bar offers a minimize command.</summary>
    public bool Minimizable { get; init; } = true;

    /// <summary>Whether the title bar offers switching between docked and floating.</summary>
    public bool Dockable { get; init; } = true;

    /// <summary>Whether a floating window can be resized at its edges and corners.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>Whether the title bar offers a lock command, which holds a floating window where it is: neither moved, resized nor maximized until unlocked.</summary>
    public bool Lockable { get; init; }

    /// <summary>The smallest a floating window can be resized to; unset, the stylesheet's minimum holds.</summary>
    public Size? MinResizeSize { get; init; }

    /// <summary>The largest a floating window can be resized to; unset, the viewport is the limit.</summary>
    public Size? MaxResizeSize { get; init; }

    /// <summary>What a floating window does when the reader presses outside it.</summary>
    public WindowPressAway PressAway { get; init; } = WindowPressAway.Ignore;

    /// <summary>The region a floating window is moved, resized, restored and maximized within.</summary>
    public WindowBounds Bounds { get; init; } = WindowBounds.Viewport;
}
