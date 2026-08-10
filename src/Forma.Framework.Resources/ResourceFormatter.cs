namespace Forma.Framework.Resources;

using System.Globalization;

/// <summary>Writes resource measurements as text for a reader.</summary>
public static class ResourceFormatter
{
    private const long Gb = Mb * 1024;

    private const long Kb = 1024;

    private const long Mb = Kb * 1024;

    private const long Tb = Gb * 1024;

    /// <summary>Writes a count of bytes in the largest unit that leaves a whole part.</summary>
    public static string FromByteCount(long byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);

        return byteCount switch
        {
            < Kb => string.Format(CultureInfo.CurrentCulture, "{0} B", byteCount),
            < Mb => string.Format(CultureInfo.CurrentCulture, "{0:0.#} KB", byteCount / (double)Kb),
            < Gb => string.Format(CultureInfo.CurrentCulture, "{0:0.#} MB", byteCount / (double)Mb),
            < Tb => string.Format(CultureInfo.CurrentCulture, "{0:0.#} GB", byteCount / (double)Gb),
            _ => string.Format(CultureInfo.CurrentCulture, "{0:0.#} TB", byteCount / (double)Tb)
        };
    }

    /// <summary>Writes a duration in the largest unit that leaves a whole part, from milliseconds up to hours.</summary>
    public static string FromDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Duration cannot be negative.");
        }

        if (duration.TotalSeconds < 1)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0:0} ms", duration.TotalMilliseconds);
        }

        if (duration.TotalMinutes < 1)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0:0.#} s", duration.TotalSeconds);
        }

        return duration.TotalHours >= 1
            ? string.Format(CultureInfo.CurrentCulture, "{0}:{1:00}:{2:00}", (int)duration.TotalHours, duration.Minutes, duration.Seconds)
            : string.Format(CultureInfo.CurrentCulture, "{0}:{1:00}", duration.Minutes, duration.Seconds);
    }

    /// <summary>Writes a transfer rate as a count of bytes over one second.</summary>
    public static string FromTransferRate(double bytesPerSecond)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bytesPerSecond);

        var rate = double.IsFinite(bytesPerSecond) ? (long)bytesPerSecond : 0;

        return string.Format(CultureInfo.CurrentCulture, "{0}/s", FromByteCount(rate));
    }
}
