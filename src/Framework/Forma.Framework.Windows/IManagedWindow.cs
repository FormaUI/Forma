namespace Forma.Framework.Windows;

/// <summary>A window a <see cref="WindowManager"/> drives: its activation, minimization, hiding, blocking, stacking and closing.</summary>
public interface IManagedWindow : IWindow
{
    /// <summary>Makes the window the current one, or stops it being so.</summary>
    ValueTask SetActiveAsync(bool active);

    /// <summary>Minimizes the window, or restores it to its preceding presentation.</summary>
    ValueTask SetMinimizedAsync(bool minimized);

    /// <summary>Hides the window under a minimized ancestor, or shows it again.</summary>
    ValueTask SetHiddenAsync(bool hidden);

    /// <summary>Bars the window from interaction behind a visible modal, or lets it interact again.</summary>
    ValueTask SetBlockedAsync(bool blocked);

    /// <summary>Puts a floating window on top of every other floating window.</summary>
    ValueTask RaiseAsync();

    /// <summary>Asks whether the window may close, reporting whether it accepts.</summary>
    ValueTask<bool> RequestCloseAsync();

    /// <summary>Closes the window.</summary>
    ValueTask PerformCloseAsync();
}
