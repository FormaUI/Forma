namespace Forma.Framework.Localization.Language;

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

/// <summary>Evaluates the <c>[$ [count] plural; one: text; other: text; $]</c> expression inside localized text.</summary>
/// <param name="rules">The rules deciding which category a count falls into.</param>
/// <param name="logger">The logger an argument that is not a count is reported to; null discards.</param>
/// <param name="diagnostics">The checks the application runs on its lookups, under which an argument that is not a count is a warning, or null where it leaves them unchecked.</param>
public sealed class PluralExpressionEvaluator(IPluralRuleSet rules, ILogger? logger = null, LocalizationDiagnostics? diagnostics = null)
{
    // How many characters an evaluation writes on the stack before it moves to a pooled buffer.
    private const int StackLength = 256;

    // What reading a format found, kept for as long as the format lives.
    private static readonly ConditionalWeakTable<string, LocaleParseResult> ParseResults = [];

    private readonly IPluralRuleSet _rules = rules ?? throw new ArgumentNullException(nameof(rules));
    private readonly ILogger _logger = logger ?? NullLogger.Instance;
    private readonly LogLevel _notNumericLevel = diagnostics is null ? LogLevel.Debug : LogLevel.Warning;
    private readonly ConcurrentDictionary<string, byte> _reportedFormats = new(StringComparer.Ordinal);

    /// <summary>The keyword a plural expression names, whose fields carry the CLDR categories.</summary>
    public static LocaleKeyword Keyword { get; } = new("plural", "zero", "one", "two", "few", "many", "other");

    private static LocaleKeywords KnownKeywords { get; } = new(Keyword);

    /// <summary>The format with every plural expression replaced by the field its count selects.</summary>
    public string Evaluate(string format, CultureInfo culture, IReadOnlyList<LocalizedArgument> arguments)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(culture);

        return Evaluate(format, culture, culture, arguments);
    }

    /// <summary>The format with every plural expression replaced, the field chosen by the culture the text is written in and the count formatted for the culture it is shown in.</summary>
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
        var parseResult = ParseResults.GetValue(format, static text => LocaleExpressionParser.Parse(text, KnownKeywords));
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

            if (expression.KeywordSpan.Slice(format).SequenceEqual(Keyword.Name))
            {
                AppendField(ref builder, format, expression, localeCulture, displayCulture, arguments);
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

    private void AppendField(ref DefaultInterpolatedStringHandler builder, string text, LocaleExpression expression, CultureInfo localeCulture, CultureInfo displayCulture, IReadOnlyList<LocalizedArgument> arguments)
    {
        var name = expression.NameSpan.Slice(text);
        if (name.IsEmpty)
        {
            RequireSoleArgument(arguments);
        }

        var count = ToDecimal(name, text, Find(name, arguments));
        var operands = PluralOperands.From(count);
        var wantedCategory = _rules.Select(localeCulture.Name, operands) ?? PluralCategory.Other;

        var selectedField = Select(text, expression.Fields, wantedCategory) ?? Select(text, expression.Fields, PluralCategory.Other);
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

    private static LocaleTextSpan? Select(string text, IReadOnlyList<LocaleField> fields, PluralCategory category)
    {
        var name = GetCategoryName(category);

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

        if (_reportedFormats.TryAdd(format, 0))
        {
            LocalizationLog.PluralArgumentNotNumeric(_logger, _notNumericLevel, name.ToString(), value?.GetType().Name ?? "null", format);
        }

        return 0m;
    }

    private static ReadOnlySpan<char> GetCategoryName(PluralCategory category)
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
