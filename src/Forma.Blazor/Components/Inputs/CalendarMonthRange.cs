namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

/// <summary>One culture's calendar over a bounded range of months.</summary>
internal sealed class CalendarMonthRange
{
    private const int DefaultYearRadius = 10;

    private readonly Calendar _calendar;
    private readonly DateTimeFormatInfo _dateFormat;

    private CalendarMonthRange(Calendar calendar, DateTimeFormatInfo format, DateOnly? minDate, DateOnly? maxDate)
    {
        _calendar = calendar;
        _dateFormat = format;

        var today = DateOnly.FromDateTime(DateTime.Today);

        MinYear = minDate?.Year ?? today.Year - DefaultYearRadius;
        MaxYear = maxDate?.Year ?? today.Year + DefaultYearRadius;

        var minReference = minDate ?? new DateOnly(MinYear, 1, 1);
        var maxReference = maxDate ?? new DateOnly(MaxYear, 12, 1);

        MinMonth = GetMonthStart(GetYear(minReference), GetMonth(minReference));
        MaxMonth = GetMonthStart(GetYear(maxReference), GetMonth(maxReference));
    }

    /// <summary>Builds the view for a culture, preferring its non-Gregorian calendar when asked to.</summary>
    public static CalendarMonthRange Create(CultureInfo culture, bool useNonGregorianMonths, DateOnly? minDate, DateOnly? maxDate)
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

        return new CalendarMonthRange(calendar, format, minDate, maxDate);
    }

    /// <summary>The first Gregorian year the range reaches.</summary>
    public int MinYear { get; }

    /// <summary>The last Gregorian year the range reaches.</summary>
    public int MaxYear { get; }

    /// <summary>The first month the range holds.</summary>
    public DateOnly MinMonth { get; }

    /// <summary>The last month the range holds.</summary>
    public DateOnly MaxMonth { get; }

    /// <summary>The weekday the grid's first column carries.</summary>
    public DayOfWeek FirstDayOfWeek
        => _dateFormat.FirstDayOfWeek;

    /// <summary>The calendar's year holding the given date.</summary>
    public int GetYear(DateOnly date)
        => _calendar.GetYear(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The calendar's month holding the given date.</summary>
    public int GetMonth(DateOnly date)
        => _calendar.GetMonth(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The day of the month the given date falls on.</summary>
    public int GetDayOfMonth(DateOnly date)
        => _calendar.GetDayOfMonth(date.ToDateTime(TimeOnly.MinValue));

    /// <summary>The first day of the given calendar month.</summary>
    public DateOnly GetMonthStart(int calendarYear, int calendarMonth)
        => DateOnly.FromDateTime(_calendar.ToDateTime(calendarYear, calendarMonth, 1, 0, 0, 0, 0));

    /// <summary>The date the given number of calendar months away.</summary>
    public DateOnly AddMonths(DateOnly date, int months)
        => DateOnly.FromDateTime(_calendar.AddMonths(date.ToDateTime(TimeOnly.MinValue), months));

    /// <summary>How many months the given calendar year holds.</summary>
    public int GetMonthsInYear(int calendarYear)
        => _calendar.GetMonthsInYear(calendarYear);

    // Named from a real date in the month rather than by index: the month-name table is the
    // 13-month leap-year one, so on a calendar whose common year is shorter (Hebrew drops
    // Adar II) indexing it directly mislabels every month past the missing slot.
    /// <summary>The month's name in the culture's own words.</summary>
    public string GetMonthName(int calendarYear, int calendarMonth)
        => GetMonthStart(calendarYear, calendarMonth).ToDateTime(TimeOnly.MinValue).ToString("MMMM", _dateFormat);

    /// <summary>The abbreviated weekday name of the grid column at the given offset.</summary>
    public string GetDayAbbreviation(int columnOffset)
        => _dateFormat.AbbreviatedDayNames[((int)FirstDayOfWeek + columnOffset) % 7];

    /// <summary>The date spelled out whole, for a day cell's accessible name.</summary>
    public string GetDayLabel(DateOnly date)
        => date.ToDateTime(TimeOnly.MinValue).ToString("D", _dateFormat);

    /// <summary>The given month pulled inside the range.</summary>
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
    public List<DateOnly?> GetDayCells(DateOnly monthStart)
    {
        var count = _calendar.GetDaysInMonth(GetYear(monthStart), GetMonth(monthStart));
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
