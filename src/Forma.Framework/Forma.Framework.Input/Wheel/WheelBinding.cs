namespace Forma.Framework.Input.Wheel;

/// <summary>A wheel binding, matching a turn by axis and only the modifiers it states.</summary>
public sealed record WheelBinding : InputBinding
{
    /// <summary>Any turn on any axis.</summary>
    public static WheelBinding Any { get; } = new();

    /// <summary>The axis the turn must mostly move along; <see langword="null"/> matches either.</summary>
    public WheelAxis? Axis { get; init; }

    /// <summary>The modifiers the turn must be made with; only those stated are checked.</summary>
    public ModifierRequirement Modifiers { get; init; } = ModifierRequirement.None;

    /// <inheritdoc />
    public override string Type
        => "wheel";
}
