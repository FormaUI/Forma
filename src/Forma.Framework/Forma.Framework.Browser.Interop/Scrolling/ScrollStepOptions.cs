namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>How a step moves a scroll container: along which axis, to which stop, and up to which frozen parts.</summary>
/// <remarks>A step comes to rest with the next stop drawn up to the far edge of the frozen parts, measured as the step runs, and past the last stop goes on to the container's end; with no stops it moves most of a page.</remarks>
public sealed record ScrollStepOptions
{
    /// <summary>The axis the step moves along.</summary>
    public ScrollAxis Axis { get; init; } = ScrollAxis.Inline;

    /// <summary>The selector of the parts a step comes to rest on.</summary>
    public string? StopSelector { get; init; }

    /// <summary>The selector of the parts that stay at the container's start, which a stop is drawn up to.</summary>
    public string? FrozenSelector { get; init; }
}
