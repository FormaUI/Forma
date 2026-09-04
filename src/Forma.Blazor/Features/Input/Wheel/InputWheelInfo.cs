namespace Forma.Blazor.Features.Input.Wheel;

/// <summary>A wheel turn that raised an action.</summary>
public sealed record InputWheelInfo : InputInfo
{
    /// <summary>The horizontal delta.</summary>
    public double DeltaX { get; init; }

    /// <summary>The vertical delta.</summary>
    public double DeltaY { get; init; }

    /// <summary>The unit of the deltas: 0 for pixels, 1 for lines, 2 for pages.</summary>
    public int DeltaMode { get; init; }

    /// <summary>The modifiers held.</summary>
    public ModifierKeys Modifiers { get; init; } = new();
}
