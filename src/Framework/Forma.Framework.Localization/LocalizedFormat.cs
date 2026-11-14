namespace Forma.Framework.Localization;

using System.Globalization;
using System.Runtime.CompilerServices;

using Forma.Framework.Culture;

/// <summary>Renders a composite format whose holes name their arguments.</summary>
public static class LocalizedFormat
{
    // How many characters a rendering writes on the stack before it moves to a pooled buffer.
    private const int StackLength = 256;

    // How many characters a value written through its hole's format takes on the stack before it is written to a string instead.
    private const int FormattedLength = 128;

    // The names a format's holes use, read once for as long as the format lives.
    private static readonly ConditionalWeakTable<string, string[]> HoleNames = [];

    /// <summary>Pairs values with the names a format's holes use, in the order those holes first appear.</summary>
    /// <param name="format">The format the names are read from.</param>
    /// <param name="values">The values to pair, one per distinct hole.</param>
    /// <remarks>A format naming no hole takes a single value, which an expression without a name reads.</remarks>
    public static LocalizedArgument[] Bind(string format, params ReadOnlySpan<object?> values)
    {
        ArgumentNullException.ThrowIfNull(format);

        var names = GetNames(format);
        if (names.Length == 0)
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

        if (values.Length != names.Length)
        {
            throw new ArgumentException(
                $"'{format}' names {names.Length} hole(s) but {values.Length} value(s) were supplied.",
                nameof(values));
        }

        var arguments = new LocalizedArgument[names.Length];
        for (var index = 0; index < names.Length; index++)
        {
            arguments[index] = new LocalizedArgument(names[index], values[index]);
        }

        return arguments;
    }

    /// <summary>Renders a format, writing each hole's argument in the cultures supplied.</summary>
    /// <param name="format">The composite format, whose holes name their arguments.</param>
    /// <param name="arguments">The values the holes name.</param>
    /// <param name="culture">The culture the values are rendered in.</param>
    /// <param name="uiCulture">The UI culture in force while a value that takes no culture of its own writes itself.</param>
    /// <remarks>A hole naming an argument that was not supplied is written as its own text.</remarks>
    public static string Render(string format, IReadOnlyList<LocalizedArgument> arguments, CultureInfo culture, CultureInfo uiCulture)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(uiCulture);

        if (!format.AsSpan().ContainsAny('{', '}'))
        {
            return format;
        }

        // Each value is formatted in the culture before it is written, so the builder is given none to format with.
        var builder = new DefaultInterpolatedStringHandler(0, 0, null, stackalloc char[StackLength]);

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            if (!segment.IsHole)
            {
                builder.AppendFormatted(segment.Text);

                continue;
            }

            Append(ref builder, segment, arguments, culture, uiCulture);
        }

        return builder.ToStringAndClear();
    }

    private static string[] GetNames(string format)
        => format.AsSpan().ContainsAny('{', '}') ? HoleNames.GetValue(format, static text => [.. ReadNames(text)]) : [];

    private static List<string> ReadNames(string format)
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

    // A value is written in the culture it is rendered in, padded to the hole's alignment.
    private static void Append(ref DefaultInterpolatedStringHandler builder, LocalizedSegment segment, IReadOnlyList<LocalizedArgument> arguments, CultureInfo culture, CultureInfo uiCulture)
    {
        if (!TryFind(segment.Name, arguments, out var value))
        {
            Restate(ref builder, segment);

            return;
        }

        switch (value)
        {
            case null or string:
                builder.AppendFormatted((string?)value, segment.Alignment);
                break;

            case ISpanFormattable formattable:
                AppendFormatted(ref builder, formattable, segment, culture);
                break;

            case IFormattable formattable:
                builder.AppendFormatted(formattable.ToString(GetFormat(segment), culture), segment.Alignment);
                break;

            // A value that takes no culture, such as a record printing its members, reads the ambient one.
            default:
                builder.AppendFormatted(AmbientCulture.Run(culture, uiCulture, () => value.ToString()) ?? string.Empty, segment.Alignment);
                break;
        }
    }

    // A value is formatted where the hole writes it, through the hole's format, so it needs no string of its own.
    private static void AppendFormatted(ref DefaultInterpolatedStringHandler builder, ISpanFormattable value, LocalizedSegment segment, CultureInfo culture)
    {
        Span<char> formatted = stackalloc char[FormattedLength];

        if (value.TryFormat(formatted, out var charsWritten, segment.Format, culture))
        {
            builder.AppendFormatted(formatted[..charsWritten], segment.Alignment);
        }
        else
        {
            builder.AppendFormatted(value.ToString(GetFormat(segment), culture), segment.Alignment);
        }
    }

    private static string? GetFormat(LocalizedSegment segment)
        => segment.Format.IsEmpty ? null : segment.Format.ToString();

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

    private static void Restate(ref DefaultInterpolatedStringHandler builder, LocalizedSegment segment)
    {
        builder.AppendLiteral("{");
        builder.AppendFormatted(segment.Name);

        if (segment.Alignment != 0)
        {
            builder.AppendLiteral(",");
            builder.AppendLiteral(segment.Alignment.ToString(CultureInfo.InvariantCulture));
        }

        if (!segment.Format.IsEmpty)
        {
            builder.AppendLiteral(":");
            builder.AppendFormatted(segment.Format);
        }

        builder.AppendLiteral("}");
    }
}
