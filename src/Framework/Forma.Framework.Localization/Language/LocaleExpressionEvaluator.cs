namespace Forma.Framework.Localization.Language;

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Evaluates the <c>[$ [count] plural; one: text; other: text; $]</c> and <c>[$ [person] gender; female: text; other: text; $]</c> expressions inside localized text.</summary>
/// <param name="rules">The rules deciding which category a count falls into.</param>
/// <param name="logger">The logger an argument that is not a count, or not a gender, is reported to; null discards.</param>
/// <param name="diagnostics">The checks the application runs on its lookups, under which an argument that is not a count or a gender is a warning, or null where it leaves them unchecked.</param>
public sealed class LocaleExpressionEvaluator(IPluralRuleSet rules, ILogger? logger = null, LocalizationDiagnostics? diagnostics = null)
{
    // How many characters an evaluation writes on the stack before it moves to a pooled buffer.
    private const int StackLength = 256;

    // The field every expression falls back to.
    private const string OtherField = "other";

    // What reading a format found, kept for as long as the format lives.
    private static readonly ConditionalWeakTable<string, LocaleParseResult> ParseResults = [];

    private readonly IPluralRuleSet _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly LogLevel _unusableArgumentLevel = diagnostics is null ? LogLevel.Debug : LogLevel.Warning;
    private readonly ConcurrentDictionary<string, byte> _reportedPluralFormats = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _reportedGenderFormats = new(StringComparer.Ordinal);

    /// <summary>The keyword a plural expression names, whose fields carry the CLDR categories.</summary>
    public static LocaleKeyword PluralKeyword { get; } = new("plural", "zero", "one", "two", "few", "many", OtherField);

    /// <summary>The keyword a gender expression names, whose fields carry the genders a person is referred to by.</summary>
    public static LocaleKeyword GenderKeyword { get; } = new("gender", "female", "male", OtherField);

    /// <summary>The keywords an expression may name.</summary>
    public static LocaleKeywords Keywords { get; } = new(PluralKeyword, GenderKeyword);

    /// <summary>The format with every expression replaced by the field its argument selects.</summary>
    public string Evaluate(string format, CultureInfo culture, IReadOnlyList<LocalizedArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(culture);

        return Evaluate(format, culture, culture, arguments);
    }

    /// <summary>The format with every expression replaced, a plural's field chosen by the culture the text is written in and its count formatted for the culture it is shown in.</summary>
    public string Evaluate(string format, CultureInfo localeCulture, CultureInfo displayCulture, IReadOnlyList<LocalizedArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(localeCulture);
        ArgumentNullException.ThrowIfNull(displayCulture);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!format.Contains(LocaleExpressionParser.OpeningDelimiter, StringComparison.Ordinal))
        {
            return format;
        }

        // An expression the parser rejected stays in the text as written.
        var parseResult = ParseResults.GetValue(format, static text => LocaleExpressionParser.Parse(text, Keywords));
        if (parseResult.Expressions.Count == 0)
        {
            return format;
        }

        // The count is formatted in the display culture before it is written, so the builder is given none to format with.
        var builder = new DefaultInterpolatedStringHandler(0, 0, null, stackalloc char[StackLength]);
        var position = 0;

        var expressions = parseResult.Expressions;
        for (var index = 0; index < expressions.Count; index++)
        {
            var expression = expressions[index];
            builder.AppendFormatted(format.AsSpan(position, expression.Span.Start - position));

            var keyword = expression.KeywordSpan.Slice(format);
            if (keyword.SequenceEqual(PluralKeyword.Name))
            {
                AppendPluralField(ref builder, format, expression, localeCulture, displayCulture, arguments);
            }
            else if (keyword.SequenceEqual(GenderKeyword.Name))
            {
                AppendGenderField(ref builder, format, expression, arguments);
            }
            else
            {
                builder.AppendFormatted(format.AsSpan(expression.Span.Start, expression.Span.Length));
            }

            position = expression.Span.End;
        }

        builder.AppendFormatted(format.AsSpan(position));

        return builder.ToStringAndClear();
    }

    private void AppendPluralField(ref DefaultInterpolatedStringHandler builder, string text, LocaleExpression expression, CultureInfo localeCulture, CultureInfo displayCulture, IReadOnlyList<LocalizedArgument> arguments)
    {
        var name = expression.NameSpan.Slice(text);
        if (name.IsEmpty)
        {
            RequireSoleArgument(arguments);
        }

        var count = ToDecimal(name, text, Find(name, arguments));
        var operands = PluralOperands.From(count);
        var wantedCategory = _rules.Select(localeCulture.Name, operands) ?? PluralCategory.Other;

        var selectedField = Select(text, expression.Fields, GetCategoryName(wantedCategory)) ?? Select(text, expression.Fields, OtherField);
        if (selectedField is not { } field)
        {
            return;
        }

        Span<char> number = stackalloc char[96];
        var display = Format(count, operands.FractionDigits, displayCulture, number);

        // A '#' stands for the count formatted in the culture it is shown in, and a doubled '#' for a literal one.
        var remainingText = field.Slice(text);
        while (true)
        {
            var hash = remainingText.IndexOf('#');
            if (hash < 0)
            {
                builder.AppendFormatted(remainingText);

                return;
            }

            builder.AppendFormatted(remainingText[..hash]);

            if (hash + 1 < remainingText.Length && remainingText[hash + 1] == '#')
            {
                builder.AppendLiteral("#");
                remainingText = remainingText[(hash + 2)..];

                continue;
            }

            builder.AppendFormatted(display);
            remainingText = remainingText[(hash + 1)..];
        }
    }

    private void AppendGenderField(ref DefaultInterpolatedStringHandler builder, string text, LocaleExpression expression, IReadOnlyList<LocalizedArgument> arguments)
    {
        var name = expression.NameSpan.Slice(text);
        if (name.IsEmpty)
        {
            RequireSoleArgument(arguments);
        }

        var wantedField = GetGenderField(ToGender(name, text, Find(name, arguments)));

        if ((Select(text, expression.Fields, wantedField) ?? Select(text, expression.Fields, OtherField)) is { } field)
        {
            builder.AppendFormatted(field.Slice(text));
        }
    }

    private static LocaleTextSpan? Select(string text, IReadOnlyList<LocaleField> fields, ReadOnlySpan<char> name)
    {
        for (var index = 0; index < fields.Count; index++)
        {
            if (fields[index].NameSpan.Slice(text).SequenceEqual(name))
            {
                return fields[index].ValueSpan;
            }
        }

        return null;
    }

    private static object? Find(ReadOnlySpan<char> name, IReadOnlyList<LocalizedArgument> arguments)
    {
        for (var index = 0; index < arguments.Count; index++)
        {
            if (name.SequenceEqual(arguments[index].Name))
            {
                return arguments[index].Value;
            }
        }

        return null;
    }

    // An expression naming no argument reads the sole unnamed argument.
    private static void RequireSoleArgument(IReadOnlyList<LocalizedArgument> arguments)
    {
        if (arguments.Count != 1 || arguments[0].Name.Length != 0)
        {
            throw new InvalidOperationException(
                $"An expression naming no argument needs exactly one unnamed argument, but {arguments.Count} argument(s) were supplied. Name the argument and the expression.");
        }
    }

    // The count keeps the digits it was written with, so 0.5 does not round away.
    private static ReadOnlySpan<char> Format(decimal count, int fractionDigits, CultureInfo culture, Span<char> destination)
    {
        Span<char> specifier = stackalloc char[4];
        specifier[0] = 'N';
        _ = fractionDigits.TryFormat(specifier[1..], out var specifierLength, provider: CultureInfo.InvariantCulture);
        specifier = specifier[..(1 + specifierLength)];

        return count.TryFormat(destination, out var charsWritten, specifier, culture)
            ? destination[..charsWritten]
            : count.ToString(specifier.ToString(), culture);
    }

    // The name comes from the entry's text, so it can name an argument that is not a count, or none at all.
    private decimal ToDecimal(ReadOnlySpan<char> name, string format, object? value)
    {
        try
        {
            if (value is IConvertible convertible)
            {
                return convertible.ToDecimal(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
        }

        if (_reportedPluralFormats.TryAdd(format, 0))
        {
            LocalizationLog.PluralArgumentNotNumeric(_logger, _unusableArgumentLevel, name.ToString(), value?.GetType().Name ?? "null", format);
        }

        return 0m;
    }

    // An enum names a gender by its member's name and a string by its text; null names none.
    private string? ToGender(ReadOnlySpan<char> name, string format, object? value)
    {
        switch (value)
        {
            case null:
                return null;

            case string text:
                return text;

            case Enum member:
                return member.ToString();
        }

        if (_reportedGenderFormats.TryAdd(format, 0))
        {
            LocalizationLog.GenderArgumentNotEnumOrString(_logger, _unusableArgumentLevel, name.ToString(), value.GetType().Name, format);
        }

        return null;
    }

    // A gender matches the field it spells, whatever its case, and every other gender reads as other.
    private static string GetGenderField(string? gender)
    {
        foreach (var fieldName in GenderKeyword.FieldNames)
        {
            if (string.Equals(fieldName, gender, StringComparison.OrdinalIgnoreCase))
            {
                return fieldName;
            }
        }

        return OtherField;
    }

    private static string GetCategoryName(PluralCategory category)
        => category switch
        {
            PluralCategory.Zero => "zero",
            PluralCategory.One => "one",
            PluralCategory.Two => "two",
            PluralCategory.Few => "few",
            PluralCategory.Many => "many",
            _ => OtherField,
        };
}
