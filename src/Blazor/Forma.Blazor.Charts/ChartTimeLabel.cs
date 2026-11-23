namespace Forma.Blazor.Components.Charts;

/// <summary>A label of a chart's time axis: the period it names, its text, and the tier deciding from what width it shows.</summary>
/// <param name="Index">The index of the period the label names.</param>
/// <param name="Text">The label's text.</param>
/// <param name="Tier">The label's tier, 0 for one that always shows.</param>
internal sealed record ChartTimeLabel(int Index, string Text, int Tier);
