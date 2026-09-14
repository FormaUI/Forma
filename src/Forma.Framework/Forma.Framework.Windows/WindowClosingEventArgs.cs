namespace Forma.Framework.Windows;

/// <summary>The arguments of a window being asked whether it may close; setting <see cref="Cancel"/> keeps it, and every window closing with it, open.</summary>
public sealed class WindowClosingEventArgs : EventArgs
{
    /// <summary>Whether the window declines to close.</summary>
    public bool Cancel { get; set; }
}
