namespace Forma.Blazor.Components.Charts;

/// <summary>The span of time a chart covers, and the periods it divides it into or the moments its points stand at.</summary>
public enum ChartRange
{
    /// <summary>An hour of one-minute periods.</summary>
    Hour,

    /// <summary>A day of one-hour periods.</summary>
    Day,

    /// <summary>Thirty days of one-day periods.</summary>
    ThirtyDays,

    /// <summary>Thirteen months of one-month periods.</summary>
    ThirteenMonths,

    /// <summary>The span from a chart's start to its end, each point at its own time.</summary>
    Span,
}
