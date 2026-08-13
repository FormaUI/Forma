namespace Forma.Framework.Localization;

using System.Buffers;
using System.Globalization;

using Forma.Framework.Localization.Cldr;

/// <summary>Resolves the {index, plural, category {text}} construct inside a translated format.</summary>
/// <param name="rules">The rules deciding which category a count falls into.</param>
public sealed class PluralExpressionEvaluator(IPluralRuleSet rules)
{
    private const string Marker = ", plural,";
    private const int StackLimit = 256;

    private readonly IPluralRuleSet _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    /// <summary>Replaces every plural construct with the branch the count selects, leaving other holes open.</summary>
    public string Evaluate(string format, CultureInfo culture, IReadOnlyList<object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(culture);
        ArgumentNullException.ThrowIfNull(arguments);

        // Most translations carry no plural construct, and this keeps them clear of the stack buffer below.
        return format.Contains(Marker, StringComparison.Ordinal)
            ? EvaluateCore(format, culture, arguments)
            : format;
    }

    private string EvaluateCore(string format, CultureInfo culture, IReadOnlyList<object?> arguments)
    {
        var current = format;

        // Reused across constructs, because a stackalloc inside the loop would grow the frame each time.
        Span<char> number = stackalloc char[96];

        while (true)
        {
            var span = current.AsSpan();

            var marker = span.IndexOf(Marker, StringComparison.Ordinal);
            if (marker < 0)
            {
                return current;
            }

            var open = span[..marker].LastIndexOf('{');
            if (open < 0 || !int.TryParse(span[(open + 1)..marker], NumberStyles.None, CultureInfo.InvariantCulture, out var index))
            {
                return current;
            }

            var close = MatchingBrace(span, open);
            if (close < 0 || close < marker + Marker.Length)
            {
                return current;
            }

            var count = index < arguments.Count ? ToDecimal(arguments[index]) : 0m;
            var operands = PluralOperands.From(count);
            var wanted = _rules.Select(culture.TwoLetterISOLanguageName, operands) ?? PluralCategory.Other;

            var body = span[(marker + Marker.Length)..close];
            if (!TrySelectBranch(body, wanted, out var branch) && !TrySelectBranch(body, PluralCategory.Other, out branch))
            {
                branch = default;
            }

            var display = Format(count, operands.FractionDigits, culture, number);

            current = Compose(span[..open], branch, display, span[(close + 1)..]);
        }
    }

    // The count keeps the digits it was written with, so 0.5 does not round away.
    private static ReadOnlySpan<char> Format(decimal count, int fractionDigits, CultureInfo culture, Span<char> destination)
    {
        Span<char> specifier = stackalloc char[4];
        specifier[0] = 'N';
        _ = fractionDigits.TryFormat(specifier[1..], out var specifierLength, provider: CultureInfo.InvariantCulture);
        specifier = specifier[..(1 + specifierLength)];

        return count.TryFormat(destination, out var written, specifier, culture)
            ? destination[..written]
            : count.ToString(specifier.ToString(), culture);
    }

    private static string Compose(ReadOnlySpan<char> prefix, ReadOnlySpan<char> branch, ReadOnlySpan<char> display, ReadOnlySpan<char> suffix)
    {
        var length = prefix.Length + branch.Length + (branch.Count('#') * (display.Length - 1)) + suffix.Length;

        char[]? rented = null;
        var buffer = length <= StackLimit ? stackalloc char[StackLimit] : (rented = ArrayPool<char>.Shared.Rent(length));

        try
        {
            prefix.CopyTo(buffer);
            var written = prefix.Length;

            var rest = branch;
            while (true)
            {
                var hash = rest.IndexOf('#');
                if (hash < 0)
                {
                    rest.CopyTo(buffer[written..]);
                    written += rest.Length;

                    break;
                }

                rest[..hash].CopyTo(buffer[written..]);
                written += hash;

                display.CopyTo(buffer[written..]);
                written += display.Length;

                rest = rest[(hash + 1)..];
            }

            suffix.CopyTo(buffer[written..]);

            return new string(buffer[..(written + suffix.Length)]);
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    // The index comes from the translation, so it can name an argument that is not a count.
    private static decimal ToDecimal(object? value)
    {
        try
        {
            return value is IConvertible convertible ? convertible.ToDecimal(CultureInfo.InvariantCulture) : 0m;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            return 0m;
        }
    }

    private static ReadOnlySpan<char> CategoryName(PluralCategory category)
        => category switch
        {
            PluralCategory.Zero => "zero",
            PluralCategory.One => "one",
            PluralCategory.Two => "two",
            PluralCategory.Few => "few",
            PluralCategory.Many => "many",
            _ => "other",
        };

    private static int MatchingBrace(ReadOnlySpan<char> text, int open)
    {
        var depth = 0;

        for (var position = open; position < text.Length; position++)
        {
            if (text[position] == '{')
            {
                depth++;
            }
            else if (text[position] == '}' && --depth == 0)
            {
                return position;
            }
        }

        return -1;
    }

    private static bool TrySelectBranch(ReadOnlySpan<char> body, PluralCategory category, out ReadOnlySpan<char> branch)
    {
        var name = CategoryName(category);
        var position = 0;

        while (position < body.Length)
        {
            while (position < body.Length && body[position] is ' ' or ',')
            {
                position++;
            }

            var nameStart = position;
            while (position < body.Length && body[position] != '{')
            {
                position++;
            }

            if (position >= body.Length)
            {
                break;
            }

            var found = body[nameStart..position].Trim();

            var branchEnd = MatchingBrace(body, position);
            if (branchEnd < 0)
            {
                break;
            }

            if (found.SequenceEqual(name))
            {
                branch = body[(position + 1)..branchEnd];

                return true;
            }

            position = branchEnd + 1;
        }

        branch = default;

        return false;
    }
}
