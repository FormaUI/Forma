namespace Forma.Blazor.Components.Windows;

/// <summary>Where a <see cref="FormaWindow"/> stands.</summary>
public enum WindowMode
{
    /// <summary>A region of the page, laid out by its parent like any other backdrop.</summary>
    Embedded,

    /// <summary>A box over the page that the reader can move and resize.</summary>
    Floating,
}
