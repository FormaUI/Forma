namespace Forma.Framework.Localization;

using System.Globalization;
using System.Text;

using Forma.Framework.Localization.Language;
using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Evaluates the <c>[$ [0] plural; one: text; other: text; $]</c> expression inside localized text.</summary>
/// <param name="rules">The rules deciding which category a count falls into.</param>
public sealed class PluralExpressionEvaluator(IPluralRuleSet rules)
{
    private readonly IPluralRuleSet _rules = rules ?? throw new ArgumentNullException(nameof(rules));

    /// <summary>The keyword a plural expression names, whose fields carry the CLDR categories.</summary>
    public static LocaleKeyword Keyword { get; } = new("plural", "zero", "one", "two", "few", "many", "other");

    private static LocaleKeywords Known { get; } = new(Keyword);

    /// <summary>Replaces every plural expression with the field the count selects, leaving other holes open.</summary>
    public string Evaluate(string format, CultureInfo culture, IReadOnlyList<object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(culture);

        return Evaluate(format, culture, culture, arguments);
    }

    /// <summary>Replaces every plural expression, choosing the field by the culture the text is written in and formatting the count for the culture it is shown in.</summary>
    public string Evaluate(string format, CultureInfo localeCulture, CultureInfo displayCulture, IReadOnlyList<object?> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(localeCulture);
        ArgumentNullException.ThrowIfNull(displayCulture);
        ArgumentNullException.ThrowIfNull(arguments);

        // An expression the parser rejected is absent from the result, so its text survives rather than vanishing.
        var parsed = LocaleExpressionParser.Parse(format, Known);
        if (parsed.Expressions.Count == 0)
        {
            return format;
        }

        var builder = new StringBuilder(format.Length);
        var position = 0;

        foreach (var expression in parsed.Expressions)
        {
            builder.Append(format, position, expression.Span.Start - position);

            // An expression named for something else keeps its text, so whatever owns it can still find it.
            if (expression.KeywordSpan.Slice(format).SequenceEqual(Keyword.Name))
            {
                AppendField(builder, format, expression, localeCulture, displayCulture, arguments);
            }
            else
            {
                builder.Append(format, expression.Span.Start, expression.Span.Length);
            }

            position = expression.Span.End;
        }

        builder.Append(format, position, format.Length - position);

        return builder.ToString();
    }

    private void AppendField(StringBuilder builder, string text, LocaleExpression expression, CultureInfo localeCulture, CultureInfo displayCulture, IReadOnlyList<object?> arguments)
    {
        var count = expression.ArgumentIndex < arguments.Count ? ToDecimal(arguments[expression.ArgumentIndex]) : 0m;
        var operands = PluralOperands.From(count);
        var wanted = _rules.Select(localeCulture.Name, operands) ?? PluralCategory.Other;

        var selected = Select(text, expression.Fields, wanted) ?? Select(text, expression.Fields, PluralCategory.Other);
        if (selected is not { } field)
        {
            return;
        }

        Span<char> number = stackalloc char[96];
        var display = Format(count, operands.FractionDigits, displayCulture, number);

        // A '#' stands for the count formatted in the culture it is shown in.
        var rest = field.Slice(text);
        while (true)
        {
            var hash = rest.IndexOf('#');
            if (hash < 0)
            {
                builder.Append(rest);

                return;
            }

            builder.Append(rest[..hash]);
            builder.Append(display);

            rest = rest[(hash + 1)..];
        }
    }

    private static LocaleTextSpan? Select(string text, IReadOnlyList<LocaleField> fields, PluralCategory category)
    {
        var name = CategoryName(category);

        foreach (var field in fields)
        {
            if (field.NameSpan.Slice(text).SequenceEqual(name))
            {
                return field.ValueSpan;
            }
        }

        return null;
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
}
