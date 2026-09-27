namespace Forma.Framework.Browser.Interop.Behaviors;

using Microsoft.AspNetCore.Components;

/// <summary>How a wheel turned over a target is answered: by moving a track along its inline axis, or by telling the owner which way it turned; either is a route on the input router under the action the caller names.</summary>
public sealed record ScrollWheel
{
    /// <summary>The action the turn is routed as, <c>{owner}.{action}</c>, which the host remaps the turn by.</summary>
    public required string Action { get; init; }

    /// <summary>The element moved by the wheel; <see langword="null"/> moves the target itself.</summary>
    public ElementReference? Track { get; init; }

    /// <summary>Whether the owner is told the direction instead of anything moving.</summary>
    public bool TellsOwner { get; init; }

    /// <summary>The key the reader holds for the wheel to be answered at all.</summary>
    public WheelModifier Modifier { get; init; }

    /// <summary>Moving the target itself, as <paramref name="action"/>.</summary>
    public static ScrollWheel MovesTarget(string action)
        => new() { Action = action };

    /// <summary>Moving <paramref name="track"/> by a wheel turned over the target, as <paramref name="action"/>.</summary>
    public static ScrollWheel Moves(string action, ElementReference track)
        => new() { Action = action, Track = track };

    /// <summary>Telling the owner which way the wheel turned, as <paramref name="action"/>, only with <paramref name="modifier"/> held when one is given.</summary>
    public static ScrollWheel Tells(string action, WheelModifier modifier = WheelModifier.None)
        => new() { Action = action, TellsOwner = true, Modifier = modifier };
}
