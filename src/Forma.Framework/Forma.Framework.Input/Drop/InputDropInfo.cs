namespace Forma.Framework.Input.Drop;

/// <summary>A drop, or a drag passing over, that raised an action.</summary>
public sealed record InputDropInfo : InputInfo
{
    /// <summary>The data kinds the drag carries, as its data transfer names them.</summary>
    public IReadOnlyList<string> Data { get; init; } = [];

    /// <summary>The horizontal viewport position.</summary>
    public double ClientX { get; init; }

    /// <summary>The vertical viewport position.</summary>
    public double ClientY { get; init; }
}
