namespace Forma.Framework.Localization;

using System.Globalization;
using System.Text;

/// <summary>Renders a composite format whose holes name their arguments.</summary>
public static class LocalizedFormat
{
    /// <summary>Pairs values with the names a format's holes use, in the order those holes first appear.</summary>
    /// <param name="format">The format the names are read from.</param>
    /// <param name="values">The values to pair, one per distinct hole.</param>
    /// <remarks>A format naming no hole takes a single value, which an expression without a name reads.</remarks>
    public static LocalizedArgument[] Bind(string format, params ReadOnlySpan<object?> values)
    {
        ArgumentNullException.ThrowIfNull(format);

        var names = GetNames(format);
        if (names.Count == 0)
        {
            return values.Length switch
            {
                0 => [],
                1 => [LocalizedArgument.Unnamed(values[0])],
                _ => throw new ArgumentException(
                    $"'{format}' names no hole, so it takes at most one value, but {values.Length} were supplied.",
                    nameof(values)),
            };
        }

        if (values.Length != names.Count)
        {
            throw new ArgumentException(
                $"'{format}' names {names.Count} hole(s) but {values.Length} value(s) were supplied.",
                nameof(values));
        }

        var arguments = new LocalizedArgument[names.Count];
        for (var index = 0; index < names.Count; index++)
        {
            arguments[index] = new LocalizedArgument(names[index], values[index]);
        }

        return arguments;
    }

    /// <summary>Renders a format, writing each hole's argument in the culture supplied.</summary>
    /// <param name="format">The composite format, whose holes name their arguments.</param>
    /// <param name="arguments">The values the holes name.</param>
    /// <param name="culture">The culture the values are rendered in.</param>
    /// <remarks>A hole naming an argument that was not supplied is written as its own text.</remarks>
    public static string Render(string format, IReadOnlyList<LocalizedArgument> arguments, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(culture);

        if (!format.AsSpan().ContainsAny('{', '}'))
        {
            return format;
        }

        var builder = new StringBuilder(format.Length);

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            if (!segment.IsHole)
            {
                builder.Append(segment.Text);

                continue;
            }

            Append(builder, segment, arguments, culture);
        }

        return builder.ToString();
    }

    private static List<string> GetNames(string format)
    {
        var names = new List<string>();

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            if (!segment.IsHole)
            {
                continue;
            }

            var name = segment.Name.ToString();
            if (!names.Contains(name, StringComparer.Ordinal))
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static void Append(StringBuilder builder, LocalizedSegment segment, IReadOnlyList<LocalizedArgument> arguments, CultureInfo culture)
    {
        if (!TryFind(segment.Name, arguments, out var value))
        {
            Restate(builder, segment);

            return;
        }

        var text = value is IFormattable formattable
            ? formattable.ToString(segment.Format.IsEmpty ? null : segment.Format.ToString(), culture)
            : value?.ToString() ?? string.Empty;

        var padding = Math.Abs(segment.Alignment) - text.Length;
        if (padding <= 0)
        {
            builder.Append(text);

            return;
        }

        if (segment.Alignment < 0)
        {
            builder.Append(text).Append(' ', padding);

            return;
        }

        builder.Append(' ', padding).Append(text);
    }

    private static bool TryFind(ReadOnlySpan<char> name, IReadOnlyList<LocalizedArgument> arguments, out object? value)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            if (name.SequenceEqual(arguments[index].Name))
            {
                value = arguments[index].Value;

                return true;
            }
        }

        value = null;

        return false;
    }

    private static void Restate(StringBuilder builder, LocalizedSegment segment)
    {
        builder.Append('{').Append(segment.Name);

        if (segment.Alignment != 0)
        {
            builder.Append(',').Append(segment.Alignment.ToString(CultureInfo.InvariantCulture));
        }

        if (!segment.Format.IsEmpty)
        {
            builder.Append(':').Append(segment.Format);
        }

        builder.Append('}');
    }
}
