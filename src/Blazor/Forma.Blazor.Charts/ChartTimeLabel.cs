namespace Forma.Blazor.Components.Charts;

/// <summary>A label of a chart's time axis: where it stands, its text, and the tier deciding from what width it shows.</summary>
/// <param name="Position">Where the label stands along the axis, from 0 at its inline start to 1 at its end.</param>
/// <param name="Text">The label's text.</param>
/// <param name="Tier">The label's tier, 0 for one that always shows.</param>
internal sealed record ChartTimeLabel(double Position, string Text, int Tier);
