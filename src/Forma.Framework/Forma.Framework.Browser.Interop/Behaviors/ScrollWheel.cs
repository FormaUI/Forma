namespace Forma.Framework.Browser.Interop.Behaviors;

using Microsoft.AspNetCore.Components;

/// <summary>How a wheel turned over a target is answered: by moving a track along its inline axis, or by telling the owner which way it turned.</summary>
public sealed record ScrollWheel
{
    /// <summary>The action the turn is routed as on the input router, <c>{owner}.{action}</c>.</summary>
    public required string Action { get; init; }

    /// <summary>The element moved by the wheel; <see langword="null"/> moves the target itself.</summary>
    public ElementReference? Track { get; init; }

    /// <summary>Whether the owner is told the direction instead of anything moving.</summary>
    public bool TellsOwner { get; init; }

    /// <summary>The key the reader holds for the wheel to be answered.</summary>
    public WheelModifier Modifier { get; init; }

    /// <summary>A scroll wheel that moves the target itself, routed as <paramref name="action"/>.</summary>
    public static ScrollWheel MovesTarget(string action)
        => new() { Action = action };

    /// <summary>A scroll wheel that moves <paramref name="track"/> by a wheel turned over the target, routed as <paramref name="action"/>.</summary>
    public static ScrollWheel Moves(string action, ElementReference track)
        => new() { Action = action, Track = track };

    /// <summary>A scroll wheel that tells the owner which way it turned, routed as <paramref name="action"/> and answered with <paramref name="modifier"/> held.</summary>
    public static ScrollWheel Tells(string action, WheelModifier modifier = WheelModifier.None)
        => new() { Action = action, TellsOwner = true, Modifier = modifier };
}
