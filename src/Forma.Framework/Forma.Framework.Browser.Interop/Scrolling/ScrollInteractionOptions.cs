namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>What moves a scroll container.</summary>
public sealed record ScrollInteractionOptions
{
    /// <summary>How each step moves the container.</summary>
    public ScrollStepOptions Steps { get; init; } = new();

    /// <summary>The buttons that step the container.</summary>
    public ScrollButtons? Buttons { get; init; }

    /// <summary>The action a vertical wheel turn over the container is routed as, <c>{owner}.{action}</c>.</summary>
    public string? WheelAction { get; init; }
}
