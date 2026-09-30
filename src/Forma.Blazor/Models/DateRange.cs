namespace Forma.Blazor.Models;

/// <summary>A span of whole days from a first date to a last date, both included.</summary>
public readonly record struct DateRange
{
    /// <summary>Creates a range from its first and last dates.</summary>
    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentOutOfRangeException(nameof(end), end, $"The range's last date cannot come before its first date, {start}.");
        }

        Start = start;
        End = end;
    }

    /// <summary>The first date of the range.</summary>
    public DateOnly Start { get; }

    /// <summary>The last date of the range.</summary>
    public DateOnly End { get; }

    /// <summary>Whether the range is a single day.</summary>
    public bool IsSingleDay
        => Start == End;

    /// <summary>Whether a date falls within the range.</summary>
    public bool Contains(DateOnly date)
        => date >= Start && date <= End;

    /// <summary>Reads the range's first and last dates.</summary>
    public void Deconstruct(out DateOnly start, out DateOnly end)
    {
        start = Start;
        end = End;
    }

    /// <summary>Creates a range from two dates in either order.</summary>
    public static DateRange Between(DateOnly first, DateOnly second)
        => first <= second ? new(first, second) : new(second, first);
}
