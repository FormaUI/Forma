namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>What moves a scroll container while its interaction is attached.</summary>
public sealed record ScrollInteractionOptions
{
    /// <summary>How each step moves the container.</summary>
    public ScrollStepOptions Steps { get; init; } = new();

    /// <summary>The buttons that step the container.</summary>
    public ScrollButtons? Buttons { get; init; }

    /// <summary>The action a vertical wheel turn over the container is routed as on the input router, <c>{owner}.{action}</c>, moving the container along its inline axis.</summary>
    public string? WheelAction { get; init; }
}
