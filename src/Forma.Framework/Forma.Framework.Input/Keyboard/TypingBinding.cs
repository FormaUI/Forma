namespace Forma.Framework.Input.Keyboard;

/// <summary>A binding matching any printable character typed other than a space, one composed through AltGr included, for type-ahead; the key typed arrives with the action.</summary>
public sealed record TypingBinding : InputBinding
{
    /// <summary>Marks the binding as matched by what was typed rather than by a named key.</summary>
    public bool Typing { get; } = true;

    /// <inheritdoc />
    public override string Type
        => "keydown";
}
