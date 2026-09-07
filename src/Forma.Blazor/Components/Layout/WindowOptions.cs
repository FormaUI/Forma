namespace Forma.Blazor.Components.Layout;

using Forma.Blazor.Models;

/// <summary>What a <see cref="FormaWindow"/> lets the reader do to it from its title bar and edges, and how far.</summary>
public sealed record WindowOptions
{
    /// <summary>The defaults: closable, maximizable, dockable and resizable, with no limits of its own.</summary>
    public static WindowOptions Default { get; } = new();

    /// <summary>Whether the title bar offers a close command.</summary>
    public bool Closable { get; init; } = true;

    /// <summary>Whether a floating window can fill the viewport and return.</summary>
    public bool Maximizable { get; init; } = true;

    /// <summary>Whether the title bar offers switching between docked and floating.</summary>
    public bool Dockable { get; init; } = true;

    /// <summary>Whether a floating window can be resized at its edges and corners.</summary>
    public bool Resizable { get; init; } = true;

    /// <summary>The smallest a floating window can be resized to; unset, the stylesheet's minimum holds.</summary>
    public Size? MinResizeSize { get; init; }

    /// <summary>The largest a floating window can be resized to; unset, the viewport is the limit.</summary>
    public Size? MaxResizeSize { get; init; }
}
