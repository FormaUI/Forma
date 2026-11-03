namespace Forma.Blazor.Components.Windows;

/// <summary>What a floating window does when the reader presses somewhere outside it.</summary>
public enum WindowPressAway
{
    /// <summary>The press is left alone and the window stays where it is.</summary>
    Ignore,

    /// <summary>The window goes to the back of the stacking order, and the press reaches whatever was pressed.</summary>
    SendToBack,
}
