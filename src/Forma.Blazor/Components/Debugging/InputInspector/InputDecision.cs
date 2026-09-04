namespace Forma.Blazor.Components.Debugging.InputInspector;

using Forma.Blazor.Features.Input;

/// <summary>One resolution of the input router, as the inspector shows it.</summary>
public sealed record InputDecision
{
    /// <summary>The input that was resolved.</summary>
    public required InputInfo Input { get; init; }

    /// <summary>The element whose scope owns the input, described as a selector, or <see langword="null"/> when nothing owns it.</summary>
    public string? Owner { get; init; }

    /// <summary>The action the owner performs, or <see langword="null"/> when the input was left alone.</summary>
    public string? Action { get; init; }

    /// <summary>Which rule decided: preemptive, structural, native, active, or none.</summary>
    public required string Precedence { get; init; }

    /// <summary>Whether the winning route's bindings are its defaults or the host's, for a routed input.</summary>
    public string? Source { get; init; }

    /// <summary>What decided between rival bindings, when there were any.</summary>
    public string? Tie { get; init; }

    /// <summary>The router's own account of the decision.</summary>
    public required string Reason { get; init; }

    /// <summary>Whether the browser's default was prevented.</summary>
    public bool PreventDefault { get; init; }

    /// <summary>Whether the input stopped at the owner or carried on.</summary>
    public required string Propagation { get; init; }

    /// <summary>The active ordering at the time, foremost last.</summary>
    public IReadOnlyList<InputLayer> Stack { get; init; } = [];
}
