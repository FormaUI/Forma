namespace Forma.Blazor.Models;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>A span of whole days from a first date to a last date, both included.</summary>
/// <remarks>As text, a range is ISO 8601's interval of its two dates, <c>2026-03-14/2026-03-20</c>, written and read the same in every culture.</remarks>
public readonly record struct DateRange : IParsable<DateRange>, IFormattable
{
    private const string DateFormat = "yyyy-MM-dd";

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

    /// <inheritdoc />
    public static DateRange Parse(string s, IFormatProvider? provider)
    {
        ArgumentNullException.ThrowIfNull(s);

        return TryParse(s, provider, out var range)
            ? range
            : throw new FormatException($"'{s}' isn't a range of two dates written as ISO 8601's interval, such as 2026-03-14/2026-03-20.");
    }

    /// <inheritdoc />
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out DateRange result)
    {
        result = default;

        if (s?.Split('/') is not [var first, var last]
            || !DateOnly.TryParseExact(first, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var start)
            || !DateOnly.TryParseExact(last, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var end)
            || end < start)
        {
            return false;
        }

        result = new(start, end);

        return true;
    }

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
        => $"{Start.ToString(DateFormat, CultureInfo.InvariantCulture)}/{End.ToString(DateFormat, CultureInfo.InvariantCulture)}";
}
