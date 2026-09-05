namespace Forma.Framework.Common.Interop.Behaviours;

using Microsoft.AspNetCore.Components;

/// <summary>How a wheel turned over a target is answered: by moving a track along its inline axis, or by telling the owner which way it turned.</summary>
public sealed record ScrollWheel
{
    /// <summary>The element moved by the wheel; <see langword="null"/> moves the target itself.</summary>
    public ElementReference? Track { get; init; }

    /// <summary>Whether the owner is told the direction instead of anything moving.</summary>
    public bool TellsOwner { get; init; }

    /// <summary>The key the reader holds for the wheel to be answered at all.</summary>
    public WheelModifier Modifier { get; init; }

    /// <summary>Moving the target itself.</summary>
    public static ScrollWheel MovesTarget { get; } = new();

    /// <summary>Moving <paramref name="track"/> by a wheel turned over the target.</summary>
    public static ScrollWheel Moves(ElementReference track)
        => new() { Track = track };

    /// <summary>Telling the owner which way the wheel turned, only with <paramref name="modifier"/> held when one is given.</summary>
    public static ScrollWheel Tells(WheelModifier modifier = WheelModifier.None)
        => new() { TellsOwner = true, Modifier = modifier };
}
