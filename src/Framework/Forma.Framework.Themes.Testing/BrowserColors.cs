namespace Forma.Framework.Themes.Testing;

using System.Globalization;

using Forma.Framework.Css;

// Reads a color as a browser serializes a computed one: rgb(), rgba() or color(srgb …), each channel rounded to a byte.
internal static class BrowserColors
{
    public static bool TryParse(string text, out CssColor color)
    {
        color = default;
        var opening = text.IndexOf('(', StringComparison.Ordinal);

        if (opening < 0 || text[^1] != ')')
        {
            return false;
        }

        var function = text[..opening].Trim();
        var arguments = text[(opening + 1)..^1];
        var parts = arguments.Split([',', ' ', '/'], StringSplitOptions.RemoveEmptyEntries);

        if (function is "color")
        {
            if (parts.Length is < 4 or > 5 || parts[0] != "srgb")
            {
                return false;
            }

            parts = parts[1..];
        }
        else if (function is "rgb" or "rgba")
        {
            if (parts.Length is < 3 or > 4)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        var scale = function is "color" ? 255d : 1d;
        var channels = new byte[3];

        for (var index = 0; index < 3; index++)
        {
            if (!double.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out var channel))
            {
                return false;
            }

            channels[index] = (byte)Math.Clamp(Math.Round(channel * scale), 0, 255);
        }

        var alpha = 1d;

        if (parts.Length == 4 && !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out alpha))
        {
            return false;
        }

        color = CssColor.FromRgba(channels[0], channels[1], channels[2], Math.Clamp(alpha, 0, 1));

        return true;
    }
}
