namespace Forma.Blazor.Components.Charts;

/// <summary>A kind of event a chart's points can record, such as a deposit or a fee, drawn in a shape of its own and named in the legend.</summary>
/// <param name="Name">The mark's name, as the legend, the readout and the table write it.</param>
public sealed record ChartMark(string Name);
