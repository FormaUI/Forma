namespace Forma.Framework.Input;

/// <summary>The modifier keys a binding requires; what is left <see langword="null"/> is decided by the binding's family.</summary>
public sealed record ModifierRequirement
{
    /// <summary>No modifier stated.</summary>
    public static ModifierRequirement None { get; } = new();

    /// <summary>The platform's primary shortcut modifier held: Command on Apple platforms, Control elsewhere.</summary>
    public static ModifierRequirement Shortcut { get; } = new() { Primary = true };

    /// <summary>Whether Control must be held.</summary>
    public bool? Control { get; init; }

    /// <summary>Whether Alt must be held.</summary>
    public bool? Alt { get; init; }

    /// <summary>Whether Shift must be held; <see langword="null"/> matches either way.</summary>
    public bool? Shift { get; init; }

    /// <summary>Whether Meta, the Command key on Apple platforms, must be held.</summary>
    public bool? Meta { get; init; }

    /// <summary>Whether the platform's primary shortcut modifier must be held: Command on Apple platforms, Control elsewhere.</summary>
    public bool Primary { get; init; }
}
