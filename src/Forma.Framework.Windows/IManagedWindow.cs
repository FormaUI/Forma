namespace Forma.Framework.Windows;

/// <summary>A window a <see cref="WindowManager"/> drives: it is told when it becomes active, minimized, hidden or raised, asked whether it may close, and told when it has closed.</summary>
public interface IManagedWindow : IWindow
{
    /// <summary>Makes the window the current one, or stops it being so.</summary>
    ValueTask SetActiveAsync(bool active);

    /// <summary>Minimizes the window, or restores it to its preceding presentation.</summary>
    ValueTask SetMinimizedAsync(bool minimized);

    /// <summary>Hides the window because an ancestor is minimized, or shows it again; the window's own state is untouched.</summary>
    ValueTask SetHiddenAsync(bool hidden);

    /// <summary>Puts a floating window on top of every other floating window.</summary>
    ValueTask RaiseAsync();

    /// <summary>Asks whether the window may close, reporting <see langword="false"/> when it declines.</summary>
    ValueTask<bool> RequestCloseAsync();

    /// <summary>Closes the window after every window affected by the request has accepted.</summary>
    ValueTask PerformCloseAsync();
}
