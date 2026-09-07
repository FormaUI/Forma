namespace Forma.Framework.Windows;

using Forma.Framework.Icons;

/// <summary>A window in a workspace: its identity, its origin, its parent, what it shows in a handle, and the state that decides what can be done with it.</summary>
public interface IWindow
{
    /// <summary>The window's identity, generated once and stable for its lifetime.</summary>
    Guid Id { get; }

    /// <summary>The title shown in the title bar and in the window's handle.</summary>
    string Title { get; }

    /// <summary>The icon shown before the title, if any.</summary>
    Icon? Icon { get; }

    /// <summary>The application or feature instance the window belongs to.</summary>
    WindowOrigin Origin { get; }

    /// <summary>The window that opened this one, or <see langword="null"/> for a root window.</summary>
    IWindow? Parent { get; }

    /// <summary>Whether the window is minimized on its own account.</summary>
    bool IsMinimized { get; }

    /// <summary>Whether the window refuses interaction.</summary>
    bool IsDisabled { get; }

    /// <summary>Whether the window blocks the context around it while it is visible.</summary>
    bool IsModal { get; }

    /// <summary>Whether the window floats over the page rather than standing docked in a layout.</summary>
    bool IsFloating { get; }
}
