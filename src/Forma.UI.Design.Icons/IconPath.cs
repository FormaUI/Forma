namespace Forma.UI.Design.Icons;

/// <summary>One path composing SVG icon artwork.</summary>
public sealed record IconPath
{
    /// <summary>The SVG path data.</summary>
    public required string Data { get; init; }

    /// <summary>The opacity applied to the path.</summary>
    public double Opacity { get; init; } = 1;
}
