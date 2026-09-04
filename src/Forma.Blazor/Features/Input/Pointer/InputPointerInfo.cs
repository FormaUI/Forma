namespace Forma.Blazor.Features.Input.Pointer;

/// <summary>A pointer press, release, or move that raised an action.</summary>
public sealed record InputPointerInfo : InputInfo
{
    /// <summary>The pointer's identity for the length of its contact.</summary>
    public int PointerId { get; init; }

    /// <summary>The device behind the pointer.</summary>
    public PointerType PointerType { get; init; }

    /// <summary>The button that changed: 0 for the primary, 1 for the auxiliary, 2 for the secondary.</summary>
    public int Button { get; init; }

    /// <summary>The buttons held, as the bit mask the browser reports: 1 for the primary, 2 for the secondary, 4 for the auxiliary.</summary>
    public int Buttons { get; init; }

    /// <summary>The horizontal viewport position.</summary>
    public double ClientX { get; init; }

    /// <summary>The vertical viewport position.</summary>
    public double ClientY { get; init; }

    /// <summary>The modifiers held.</summary>
    public ModifierKeys Modifiers { get; init; } = new();
}
