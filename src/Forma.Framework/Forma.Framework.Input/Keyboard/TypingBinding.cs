namespace Forma.Framework.Input.Keyboard;

/// <summary>A typing binding, matching any printable character typed other than a space, one composed through AltGr included.</summary>
public sealed record TypingBinding : InputBinding
{
    /// <summary>Marks the binding as matched by what was typed.</summary>
    public bool Typing { get; } = true;

    /// <inheritdoc />
    public override string Type
        => "keydown";
}
