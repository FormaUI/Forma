namespace Forma.Framework.Input.Keyboard;

/// <summary>A key binding: Control, Alt, and Meta must be absent unless stated, and Shift matches either way unless stated.</summary>
public sealed record KeyBinding : InputBinding
{
    /// <summary>Binds <paramref name="key"/>, compared without regard to case.</summary>
    public KeyBinding(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        Key = key;
    }

    /// <summary>The key, compared without regard to case.</summary>
    public string Key { get; }

    /// <summary>The modifiers the key must be pressed with.</summary>
    public ModifierRequirement Modifiers { get; init; } = ModifierRequirement.None;

    /// <summary>Whether the binding matches the key's release rather than its press.</summary>
    public bool Released { get; init; }

    /// <inheritdoc />
    public override string Type
        => Released ? "keyup" : "keydown";

    /// <summary>A key pressed with the platform's primary shortcut modifier held.</summary>
    public static KeyBinding Shortcut(string key, bool shift = false)
        => new(key) { Modifiers = ModifierRequirement.Shortcut with { Shift = shift } };
}
