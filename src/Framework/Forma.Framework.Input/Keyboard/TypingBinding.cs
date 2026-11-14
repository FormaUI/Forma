namespace Forma.Framework.Input.Keyboard;

/// <summary>A typing binding, matching any printable character typed, one composed through AltGr included, and a space typed within the type-ahead's pause of the last character a typing binding matched.</summary>
public sealed record TypingBinding : InputBinding
{
    /// <summary>Marks the binding as matched by what was typed.</summary>
    public bool Typing { get; } = true;

    /// <summary>How long, in milliseconds, after a character a typing binding matched a space still counts as typed, the type-ahead's pause.</summary>
    public double Pause { get; } = TypeAhead.Pause.TotalMilliseconds;

    /// <inheritdoc />
    public override string Type
        => "keydown";
}
