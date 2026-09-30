namespace Forma.Blazor.Components.Windows;

/// <summary>What a <see cref="FormaWindow"/> lets the reader do to it from its title bar, and how it answers a press outside it.</summary>
public sealed record WindowOptions
{
    /// <summary>The defaults: closable, maximizable and minimizable, with a press away ignored.</summary>
    public static WindowOptions Default { get; } = new();

    /// <summary>Whether the title bar offers a close command.</summary>
    public bool Closable { get; init; } = true;

    /// <summary>Whether the title bar offers a maximize command, which fills the window's bounds and returns.</summary>
    public bool Maximizable { get; init; } = true;

    /// <summary>Whether the title bar offers a minimize command.</summary>
    public bool Minimizable { get; init; } = true;

    /// <summary>Whether the title bar offers a lock command, which holds the window at its placement, size and maximization.</summary>
    public bool Lockable { get; init; }

    /// <summary>What the window does when the reader presses outside it.</summary>
    public WindowPressAway PressAway { get; init; } = WindowPressAway.Ignore;
}
