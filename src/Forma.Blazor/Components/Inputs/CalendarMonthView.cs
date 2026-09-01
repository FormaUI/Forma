namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

/// <summary>One culture's calendar over a bounded month window: coordinates, names, clamping, and the day grid.</summary>
internal sealed class CalendarMonthView
{
    private const int FallbackYearSpan = 10;

    private readonly Calendar _calendar;
    private readonly DateTimeFormatInfo _format;

    private CalendarMonthView(Calendar calendar, DateTimeFormatInfo format, DateOnly? minDate, DateOnly? maxDate)
    {
        _calendar = calendar;
        _format = format;

        var today = DateOnly.FromDateTime(DateTime.Today);

        MinYear = minDate?.Year ?? today.Year - FallbackYearSpan;
        MaxYear = maxDate?.Year ?? today.Year + FallbackYearSpan;

        var minReference = minDate ?? new DateOnly(MinYear, 1, 1);
        var maxReference = maxDate ?? new DateOnly(MaxYear, 12, 1);

        MinMonth = MonthStart(YearOf(minReference), MonthOf(minReference));
        MaxMonth = MonthStart(YearOf(maxReference), MonthOf(maxReference));
    }

    /// <summary>Builds the view for a culture, preferring its non-Gregorian calendar when asked to.</summary>
    public static CalendarMonthView For(CultureInfo culture, bool useNonGregorianMonths, DateOnly? minDate, DateOnly? maxDate)
    {
        var calendar = useNonGregorianMonths
            ? (culture.OptionalCalendars.FirstOrDefault(candidate => candidate is not GregorianCalendar)
               ?? culture.DateTimeFormat.Calendar)
            : culture.DateTimeFormat.Calendar;

        var format = culture.DateTimeFormat;

        if (useNonGregorianMonths && calendar is not GregorianCalendar)
        {
            format = (DateTimeFormatInfo)culture.DateTimeFormat.Clone();
            format.Calendar = calendar;
        }

        return new CalendarMonthView(calendar, format, minDate, maxDate);
    }

    /// <summary>The first Gregorian year the window reaches.</summary>
    public int MinYear { get; }

    /// <summary>The last Gregorian year the window reaches.</summary>
    public int MaxYear { get; }

    /// <summary>The first month the window shows.</summary>
    public DateOnly MinMonth { get; }

    /// <summary>The last month the window shows.</summary>
    public DateOnly MaxMonth { get; }

    /// <summary>The weekday the grid's first column carries.</summary>
    public DayOfWeek FirstDayOfWeek
        => _format.FirstDayOfWeek;

    /// <summary>The calendar's year holding the given date.</summary>
    public int YearOf(DateOnly date)
        => _calendar.GetYear(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The calendar's month holding the given date.</summary>
    public int MonthOf(DateOnly date)
        => _calendar.GetMonth(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The day of the month the given date falls on.</summary>
    public int DayOf(DateOnly date)
        => _calendar.GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The first day of the given calendar month.</summary>
    public DateOnly MonthStart(int calendarYear, int calendarMonth)
        => DateOnly.FromDateTime(_calendar.ToDateTime(calendarYear, calendarMonth, 1, 0, 0, 0, 0));

    /// <summary>The date the given number of calendar months away.</summary>
    public DateOnly AddMonths(DateOnly date, int months)
        => DateOnly.FromDateTime(_calendar.AddMonths(date.ToDateTime(TimeOnly.MinValue), months));

    /// <summary>How many months the given calendar year holds.</summary>
    public int MonthsInYear(int calendarYear)
        => _calendar.GetMonthsInYear(calendarYear);

    // Named from a real date in the month rather than by index: the month-name table is the
    // 13-month leap-year one, so on a calendar whose common year is shorter (Hebrew drops
    // Adar II) indexing it directly mislabels every month past the missing slot.
    /// <summary>The month's name in the culture's own words.</summary>
    public string MonthName(int calendarYear, int calendarMonth)
        => MonthStart(calendarYear, calendarMonth).ToDateTime(TimeOnly.MinValue).ToString("MMMM", _format);

    /// <summary>The abbreviated weekday name of the grid column at the given offset.</summary>
    public string DayAbbreviation(int columnOffset)
        => _format.AbbreviatedDayNames[((int)FirstDayOfWeek + columnOffset) % 7];

    /// <summary>The date spelled out whole, for a day cell's accessible name.</summary>
    public string DayLabel(DateOnly date)
        => date.ToDateTime(TimeOnly.MinValue).ToString("D", _format);

    /// <summary>The given month pulled inside the window.</summary>
    public DateOnly Clamp(DateOnly month)
    {
        if (month < MinMonth)
        {
            return MinMonth;
        }

        if (month > MaxMonth)
        {
            return MaxMonth;
        }

        return month;
    }

    /// <summary>The month's day cells in grid order, with a null for each leading blank.</summary>
    public List<DateOnly?> DaysOf(DateOnly monthStart)
    {
        var count = _calendar.GetDaysInMonth(YearOf(monthStart), MonthOf(monthStart));
        var leadingBlanks = ((int)monthStart.DayOfWeek - (int)FirstDayOfWeek + 7) % 7;

        var days = new List<DateOnly?>(leadingBlanks + count);

        for (var i = 0; i < leadingBlanks; i++)
        {
            days.Add(null);
        }

        for (var offset = 0; offset < count; offset++)
        {
            days.Add(monthStart.AddDays(offset));
        }

        return days;
    }
}
