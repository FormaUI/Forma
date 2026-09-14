namespace Forma.Framework.Input;

/// <summary>The modifier keys held when an input was raised.</summary>
public sealed record ModifierKeys
{
    /// <summary>Whether Control was held.</summary>
    public bool Control { get; init; }

    /// <summary>Whether Alt was held.</summary>
    public bool Alt { get; init; }

    /// <summary>Whether Shift was held.</summary>
    public bool Shift { get; init; }

    /// <summary>Whether Meta, the Command key on Apple platforms, was held.</summary>
    public bool Meta { get; init; }

    /// <summary>Whether the platform's primary shortcut modifier was held: Command on Apple platforms, Control elsewhere.</summary>
    public bool Primary { get; init; }
}
