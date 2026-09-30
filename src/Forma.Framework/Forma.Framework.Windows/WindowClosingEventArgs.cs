namespace Forma.Framework.Windows;

/// <summary>The arguments of a window being asked whether it may close.</summary>
public sealed class WindowClosingEventArgs : EventArgs
{
    /// <summary>Whether the window declines to close.</summary>
    public bool Cancel { get; set; }
}
