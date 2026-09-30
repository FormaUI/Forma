namespace Forma.Framework.Input.Pointer;

/// <summary>A pointer binding, matching by button, the buttons held, and device, and only the modifiers it states.</summary>
public sealed record PointerBinding : InputBinding
{
    /// <summary>Any press by any pointer.</summary>
    public static PointerBinding Down { get; } = new();

    /// <summary>A move while the primary button is held.</summary>
    public static PointerBinding Drag { get; } = new() { Phase = PointerPhase.Move, Buttons = 1 };

    /// <summary>The button pressed: 0 for the primary, 1 for the auxiliary, 2 for the secondary; <see langword="null"/> matches any.</summary>
    public int? Button { get; init; }

    /// <summary>The buttons that must be held, as the bit mask the browser reports: 1 for the primary, 2 for the secondary, 4 for the auxiliary; <see langword="null"/> matches any.</summary>
    public int? Buttons { get; init; }

    /// <summary>The device the input must come from; <see langword="null"/> matches any.</summary>
    public PointerType? PointerType { get; init; }

    /// <summary>The modifiers the input must be made with.</summary>
    public ModifierRequirement Modifiers { get; init; } = ModifierRequirement.None;

    /// <summary>Which part of the pointer's contact the binding matches.</summary>
    public PointerPhase Phase { get; init; }

    /// <inheritdoc />
    public override string Type
        => Phase switch
        {
            PointerPhase.Up => "pointerup",
            PointerPhase.Move => "pointermove",
            _ => "pointerdown",
        };
}
