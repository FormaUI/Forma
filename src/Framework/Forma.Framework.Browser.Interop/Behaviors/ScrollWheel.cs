namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>How a wheel turned over a target is answered: by telling the owner which way it turned.</summary>
public sealed record ScrollWheel
{
    /// <summary>The action the turn is routed as on the input router, <c>{owner}.{action}</c>.</summary>
    public required string Action { get; init; }

    /// <summary>The key the reader holds for the wheel to be answered.</summary>
    public WheelModifier Modifier { get; init; }

    /// <summary>A scroll wheel that tells the owner which way it turned, routed as <paramref name="action"/> and answered with <paramref name="modifier"/> held.</summary>
    public static ScrollWheel Tells(string action, WheelModifier modifier = WheelModifier.None)
        => new() { Action = action, Modifier = modifier };
}
