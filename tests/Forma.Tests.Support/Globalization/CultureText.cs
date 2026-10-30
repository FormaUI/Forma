namespace Forma.Tests.Globalization;

using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>Text as this platform's culture data writes it.</summary>
public static partial class CultureText
{
    /// <summary>Writes each English clock time in <paramref name="text"/>, such as <c>2:00 PM</c>, as <paramref name="culture"/> writes it on this platform.</summary>
    /// <remarks>Culture data differs by platform: ICU writes a narrow no-break space before the day period where Windows writes a space, so an expectation spelled out once reads correctly on both.</remarks>
    /// <param name="text">The text, its clock times written in English.</param>
    /// <param name="culture">The culture whose writing the times take.</param>
    /// <returns>The text with each time rewritten.</returns>
    public static string FormatTimes(string text, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(culture);

        return ClockTime().Replace(text, match =>
        {
            var time = TimeOnly.Parse(match.Value, CultureInfo.InvariantCulture);
            return time.ToString(match.Groups["seconds"].Success ? "T" : "t", culture);
        });
    }

    [GeneratedRegex(@"\b\d{1,2}:\d{2}(?<seconds>:\d{2})? [AP]M\b")]
    private static partial Regex ClockTime();
}
