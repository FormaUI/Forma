namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

/// <summary>The text a plain form posts for a value, in the invariant form Blazor's form binding reads back.</summary>
/// <remarks>
/// Blazor's form mapping hands <see cref="CultureInfo.InvariantCulture"/> to the reader of every posted value and takes no other
/// culture, so a value posts in the invariant form whatever culture the page renders in. A value of a type the mapping reads from
/// text round-trips: an enumeration by its name, a date and time by its clock with no zone, a date and time with an offset in the
/// round-trip form, and every other such type as the invariant culture writes it.
/// </remarks>
internal static class FormValue
{
    private static readonly HashSet<Type> TextTypes =
    [
        typeof(string), typeof(char), typeof(bool), typeof(Guid),
        typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong),
        typeof(Int128), typeof(UInt128), typeof(Half), typeof(float), typeof(double), typeof(decimal),
        typeof(DateTime), typeof(DateTimeOffset), typeof(DateOnly), typeof(TimeOnly), typeof(TimeSpan),
    ];

    /// <summary>Whether a value of <typeparamref name="T"/> posts as text Blazor's form binding reads back as the same value.</summary>
    public static bool IsPostable<T>()
        => (Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T)) is var type && (type.IsEnum || TextTypes.Contains(type));

    /// <summary>The text a plain form posts for <paramref name="value"/>, which is empty for a null value.</summary>
    public static string Format<T>(T value)
        => value switch
        {
            null => string.Empty,
            string text => text,
            bool flag => flag ? "true" : "false",
            DateTime dateTime => dateTime.ToString("yyyy-MM-ddTHH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            TimeOnly time => time.ToString("HH:mm:ss.FFFFFFF", CultureInfo.InvariantCulture),
            TimeSpan span => span.ToString("c", CultureInfo.InvariantCulture),
            Enum member => member.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    /// <summary>A date as the browser's date input holds it.</summary>
    public static string? FormatDate(DateOnly? date)
        => date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A time as the browser's time input holds it, to the second where <paramref name="hasSeconds"/>.</summary>
    public static string? FormatTime(TimeOnly? time, bool hasSeconds)
        => time?.ToString(hasSeconds ? "HH:mm:ss" : "HH:mm", CultureInfo.InvariantCulture);

    /// <summary>A date and time as the browser's local date and time input holds it, to the second where <paramref name="hasSeconds"/>.</summary>
    public static string? FormatClock(DateTime? clock, bool hasSeconds)
        => clock?.ToString(hasSeconds ? "yyyy-MM-ddTHH:mm:ss" : "yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    /// <summary>The seconds the browser's time input steps through for <paramref name="step"/>.</summary>
    public static string? FormatStep(TimeSpan? step)
        => step?.TotalSeconds.ToString(CultureInfo.InvariantCulture);
}
