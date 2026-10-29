namespace Forma.Framework.Culture;

using System.Globalization;

/// <summary>Compares the date, time and number formats cultures read and write with.</summary>
internal static class CultureFormats
{
    /// <summary>Whether two cultures read and write dates, times and numbers alike.</summary>
    /// <param name="first">The one culture.</param>
    /// <param name="second">The other culture.</param>
    /// <returns><see langword="true"/> where the two cultures carry the same calendar and the same date, time and number formats.</returns>
    public static bool AreEqual(CultureInfo first, CultureInfo second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return ReferenceEquals(first, second)
            || (AreEqual(first.DateTimeFormat, second.DateTimeFormat) && AreEqual(first.NumberFormat, second.NumberFormat));
    }

    private static bool AreEqual(DateTimeFormatInfo first, DateTimeFormatInfo second)
        => AreEqual(first.Calendar, second.Calendar)
            && first.Calendar.Eras.All(era => first.GetEraName(era) == second.GetEraName(era) && first.GetAbbreviatedEraName(era) == second.GetAbbreviatedEraName(era))
            && first.GetAllDateTimePatterns().AsSpan().SequenceEqual(second.GetAllDateTimePatterns())
            && first.CalendarWeekRule == second.CalendarWeekRule
            && first.FirstDayOfWeek == second.FirstDayOfWeek
            && first.ShortDatePattern == second.ShortDatePattern
            && first.LongDatePattern == second.LongDatePattern
            && first.ShortTimePattern == second.ShortTimePattern
            && first.LongTimePattern == second.LongTimePattern
            && first.FullDateTimePattern == second.FullDateTimePattern
            && first.MonthDayPattern == second.MonthDayPattern
            && first.YearMonthPattern == second.YearMonthPattern
            && first.DateSeparator == second.DateSeparator
            && first.TimeSeparator == second.TimeSeparator
            && first.AMDesignator == second.AMDesignator
            && first.PMDesignator == second.PMDesignator
            && first.DayNames.AsSpan().SequenceEqual(second.DayNames)
            && first.AbbreviatedDayNames.AsSpan().SequenceEqual(second.AbbreviatedDayNames)
            && first.ShortestDayNames.AsSpan().SequenceEqual(second.ShortestDayNames)
            && first.MonthNames.AsSpan().SequenceEqual(second.MonthNames)
            && first.AbbreviatedMonthNames.AsSpan().SequenceEqual(second.AbbreviatedMonthNames)
            && first.MonthGenitiveNames.AsSpan().SequenceEqual(second.MonthGenitiveNames)
            && first.AbbreviatedMonthGenitiveNames.AsSpan().SequenceEqual(second.AbbreviatedMonthGenitiveNames);

    // The calendar decides which dates a culture reads and writes: its kind, its eras and where a two-digit year falls.
    private static bool AreEqual(Calendar first, Calendar second)
        => first.GetType() == second.GetType()
            && first.TwoDigitYearMax == second.TwoDigitYearMax
            && first.Eras.AsSpan().SequenceEqual(second.Eras)
            && (first, second) switch
            {
                (GregorianCalendar firstGregorian, GregorianCalendar secondGregorian) => firstGregorian.CalendarType == secondGregorian.CalendarType,
                _ => true,
            };

    private static bool AreEqual(NumberFormatInfo first, NumberFormatInfo second)
        => first.NumberDecimalDigits == second.NumberDecimalDigits
            && first.NumberDecimalSeparator == second.NumberDecimalSeparator
            && first.NumberGroupSeparator == second.NumberGroupSeparator
            && first.NumberGroupSizes.AsSpan().SequenceEqual(second.NumberGroupSizes)
            && first.NumberNegativePattern == second.NumberNegativePattern
            && first.CurrencyDecimalDigits == second.CurrencyDecimalDigits
            && first.CurrencyDecimalSeparator == second.CurrencyDecimalSeparator
            && first.CurrencyGroupSeparator == second.CurrencyGroupSeparator
            && first.CurrencyGroupSizes.AsSpan().SequenceEqual(second.CurrencyGroupSizes)
            && first.CurrencyNegativePattern == second.CurrencyNegativePattern
            && first.CurrencyPositivePattern == second.CurrencyPositivePattern
            && first.CurrencySymbol == second.CurrencySymbol
            && first.PercentDecimalDigits == second.PercentDecimalDigits
            && first.PercentDecimalSeparator == second.PercentDecimalSeparator
            && first.PercentGroupSeparator == second.PercentGroupSeparator
            && first.PercentGroupSizes.AsSpan().SequenceEqual(second.PercentGroupSizes)
            && first.PercentNegativePattern == second.PercentNegativePattern
            && first.PercentPositivePattern == second.PercentPositivePattern
            && first.PercentSymbol == second.PercentSymbol
            && first.PerMilleSymbol == second.PerMilleSymbol
            && first.NegativeSign == second.NegativeSign
            && first.PositiveSign == second.PositiveSign
            && first.NaNSymbol == second.NaNSymbol
            && first.NegativeInfinitySymbol == second.NegativeInfinitySymbol
            && first.PositiveInfinitySymbol == second.PositiveInfinitySymbol
            && first.DigitSubstitution == second.DigitSubstitution
            && first.NativeDigits.AsSpan().SequenceEqual(second.NativeDigits);
}
