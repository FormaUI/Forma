namespace Forma.Framework.Currencies;

using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>The currencies in use, by ISO 4217 code: their usual minor units and the symbol each is written with in a culture, from a snapshot of Unicode CLDR.</summary>
public static class CurrencyCatalog
{
    private const string ResourceName = "Forma.Framework.Currencies._assets.currencies.json";
    private const string RootLocale = "und";
    private const string NoBreakSpace = "\u00A0";

    // The most decimal places .NET writes a currency amount with.
    private const int MaxDecimalPlaces = 99;

    private static readonly Lazy<CurrencyData> Data = new(Load);

    /// <summary>The ISO 4217 codes of the currencies in use, in order.</summary>
    public static IReadOnlyList<string> Codes
        => Data.Value.Codes;

    /// <summary>Whether the code is the ISO 4217 code of a currency in use.</summary>
    /// <param name="code">The ISO 4217 code, such as <c>USD</c>.</param>
    public static bool Contains(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        return Data.Value.CodeSet.Contains(code);
    }

    /// <summary>The number of decimal places an amount in the currency usually carries.</summary>
    /// <param name="code">The ISO 4217 code of a currency in use, such as <c>USD</c>.</param>
    public static int GetMinorUnits(string code)
    {
        var data = GetData(code);

        return data.MinorUnits.TryGetValue(code, out var minorUnits) ? minorUnits : data.DefaultMinorUnits;
    }

    /// <summary>The symbol the currency is written with in the culture, or its ISO 4217 code where the culture has no symbol for it.</summary>
    /// <param name="code">The ISO 4217 code of a currency in use, such as <c>USD</c>.</param>
    /// <param name="culture">The culture the symbol is written in.</param>
    public static string GetSymbol(string code, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var data = GetData(code);

        for (var locale = GetLocale(data, culture); locale is not null; locale = GetParent(data, locale))
        {
            if (data.Symbols.TryGetValue(locale, out var symbols) && symbols.TryGetValue(code, out var symbol))
            {
                return symbol;
            }
        }

        return code;
    }

    /// <summary>The amount written in the currency as the culture writes money, with the currency's symbol in that culture.</summary>
    /// <remarks>
    /// The culture's currency pattern, separators and grouping place the symbol, which stands a no-break space apart from the
    /// digits where its edge beside them is a letter, as Unicode CLDR's currency spacing writes <c>CHF 5.00</c>.
    /// </remarks>
    /// <param name="amount">The amount.</param>
    /// <param name="code">The ISO 4217 code of a currency in use, such as <c>USD</c>.</param>
    /// <param name="culture">The culture the amount is written in.</param>
    /// <param name="decimalPlaces">The decimal places the amount is written with, where null takes the currency's minor units.</param>
    public static string FormatAmount(decimal amount, string code, CultureInfo culture, int? decimalPlaces = null)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(culture);

        if (decimalPlaces is { } places)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(places, nameof(decimalPlaces));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(places, MaxDecimalPlaces, nameof(decimalPlaces));
        }

        var symbol = GetSymbol(code, culture);
        var numberFormat = (NumberFormatInfo)culture.NumberFormat.Clone();
        numberFormat.CurrencyDecimalDigits = decimalPlaces ?? GetMinorUnits(code);
        numberFormat.CurrencySymbol = GetSymbolSide(numberFormat, amount < 0) switch
        {
            SymbolSide.BeforeDigits when IsSpacedFromDigits(symbol[^1]) => symbol + NoBreakSpace,
            SymbolSide.AfterDigits when IsSpacedFromDigits(symbol[0]) => NoBreakSpace + symbol,
            _ => symbol
        };

        return amount.ToString("C", numberFormat);
    }

    // The side of the digits the symbol touches in the culture's pattern for the amount's sign, apart where a space or a sign stands between.
    private static SymbolSide GetSymbolSide(NumberFormatInfo numberFormat, bool isNegative)
        => isNegative
            ? numberFormat.CurrencyNegativePattern switch
            {
                0 or 1 or 3 => SymbolSide.BeforeDigits,
                4 or 5 or 7 => SymbolSide.AfterDigits,
                _ => SymbolSide.Apart
            }
            : numberFormat.CurrencyPositivePattern switch
            {
                0 => SymbolSide.BeforeDigits,
                1 => SymbolSide.AfterDigits,
                _ => SymbolSide.Apart
            };

    // CLDR spaces a symbol from the digits where its edge beside them is neither a symbol nor a space, as CHF is and $ and R$ aren't.
    private static bool IsSpacedFromDigits(char edge)
        => !char.IsWhiteSpace(edge)
            && char.GetUnicodeCategory(edge) is not (UnicodeCategory.CurrencySymbol or UnicodeCategory.MathSymbol
                or UnicodeCategory.ModifierSymbol or UnicodeCategory.OtherSymbol);

    private static CurrencyData GetData(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        var data = Data.Value;

        if (!data.CodeSet.Contains(code))
        {
            throw new ArgumentException($"'{code}' is not the ISO 4217 code of a currency in use.", nameof(code));
        }

        return data;
    }

    // The CLDR locale a culture starts from: the culture's own where CLDR has it, else the nearest of the cultures .NET derives it from,
    // so zh-TW starts from zh-Hant rather than from zh.
    private static string GetLocale(CurrencyData data, CultureInfo culture)
    {
        for (var candidateCulture = culture; candidateCulture.Name.Length > 0; candidateCulture = candidateCulture.Parent)
        {
            if (data.Locales.Contains(candidateCulture.Name))
            {
                return candidateCulture.Name;
            }
        }

        return RootLocale;
    }

    // CLDR's parent of a locale: the one it names, else the locale with its last subtag removed, else the root.
    private static string? GetParent(CurrencyData data, string locale)
    {
        if (string.Equals(locale, RootLocale, StringComparison.Ordinal))
        {
            return null;
        }

        if (data.Parents.TryGetValue(locale, out var parent))
        {
            return parent;
        }

        var cutIndex = locale.LastIndexOf('-', StringComparison.Ordinal);

        return cutIndex > 0 ? locale[..cutIndex] : RootLocale;
    }

    private static CurrencyData Load()
    {
        using var stream = typeof(CurrencyCatalog).GetTypeInfo().Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"The resource '{ResourceName}' is missing; regenerate it with scripts/Update-CurrencyData.ps1.");

        var rawData = JsonSerializer.Deserialize(stream, CurrencyJsonContext.Default.RawCurrencyData)
            ?? throw new InvalidOperationException($"The resource '{ResourceName}' could not be read.");

        return new CurrencyData(
            Array.AsReadOnly([.. rawData.Codes]),
            rawData.Codes.ToFrozenSet(StringComparer.Ordinal),
            rawData.DefaultMinorUnits,
            rawData.MinorUnits.ToFrozenDictionary(StringComparer.Ordinal),
            rawData.Locales.ToFrozenSet(StringComparer.OrdinalIgnoreCase),
            rawData.Parents.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase),
            rawData.Symbols.ToFrozenDictionary(entry => entry.Key, entry => entry.Value.ToFrozenDictionary(StringComparer.Ordinal), StringComparer.OrdinalIgnoreCase));
    }

    private enum SymbolSide
    {
        Apart,
        BeforeDigits,
        AfterDigits
    }

    private sealed record CurrencyData(
        IReadOnlyList<string> Codes,
        FrozenSet<string> CodeSet,
        int DefaultMinorUnits,
        FrozenDictionary<string, int> MinorUnits,
        FrozenSet<string> Locales,
        FrozenDictionary<string, string> Parents,
        FrozenDictionary<string, FrozenDictionary<string, string>> Symbols);
}

/// <summary>The currency data as its embedded resource holds it.</summary>
internal sealed record RawCurrencyData(
    [property: JsonPropertyName("cldr")] string Cldr,
    [property: JsonPropertyName("defaultMinorUnits")] int DefaultMinorUnits,
    [property: JsonPropertyName("codes")] IReadOnlyList<string> Codes,
    [property: JsonPropertyName("minorUnits")] IReadOnlyDictionary<string, int> MinorUnits,
    [property: JsonPropertyName("locales")] IReadOnlyList<string> Locales,
    [property: JsonPropertyName("parents")] IReadOnlyDictionary<string, string> Parents,
    [property: JsonPropertyName("symbols")] IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Symbols);

[JsonSerializable(typeof(RawCurrencyData))]
internal sealed partial class CurrencyJsonContext : JsonSerializerContext;
