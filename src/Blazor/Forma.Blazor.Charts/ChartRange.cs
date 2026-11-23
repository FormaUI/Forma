namespace Forma.Blazor.Components.Charts;

/// <summary>The span of time a chart covers, and the periods it divides it into or the moments its points stand at.</summary>
public enum ChartRange
{
    /// <summary>An hour of one-minute periods.</summary>
    Hour = 0,

    /// <summary>A day of one-hour periods.</summary>
    Day = 1,

    /// <summary>Seven days of one-day periods.</summary>
    SevenDays = 5,

    /// <summary>Thirty days of one-day periods.</summary>
    ThirtyDays = 2,

    /// <summary>Thirteen months of one-month periods.</summary>
    ThirteenMonths = 3,

    /// <summary>The span from a chart's start to its end, each point at its own time.</summary>
    Span = 4,
}
