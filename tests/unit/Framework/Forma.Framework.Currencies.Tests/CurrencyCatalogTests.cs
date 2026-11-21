namespace Forma.Framework.Currencies.Tests;

using System.Globalization;

using Xunit;

public sealed class CurrencyCatalogTests
{
    public sealed class Codes
    {
        [Fact]
        public void Should_list_the_currencies_in_use_in_order()
        {
            // Act
            var codes = CurrencyCatalog.Codes;

            // Assert
            Assert.Multiple(
                () => Assert.Subset(codes.ToHashSet(StringComparer.Ordinal), new HashSet<string>(["USD", "EUR", "ILS", "JPY", "GBP", "BHD"], StringComparer.Ordinal)),
                () => Assert.Equal(codes.Order(StringComparer.Ordinal), codes),
                () => Assert.All(codes, code => Assert.Matches("^[A-Z]{3}$", code)));
        }

        [Fact]
        public void Should_list_codes_no_caller_can_change()
        {
            // Act
            var codes = CurrencyCatalog.Codes;

            // Assert
            Assert.Multiple(
                () => Assert.False(codes is List<string>),
                () => Assert.True(codes is ICollection<string> { IsReadOnly: true }));
        }

        [Theory]
        [InlineData("USD", true)]
        [InlineData("ILS", true)]
        [InlineData("DEM", false)]
        [InlineData("usd", false)]
        [InlineData("", false)]
        public void Should_hold_a_currency_in_use_by_its_code(string code, bool expected)
        {
            // Act
            var contains = CurrencyCatalog.Contains(code);

            // Assert
            Assert.Equal(expected, contains);
        }
    }

    public sealed class MinorUnits
    {
        [Theory]
        [InlineData("USD", 2)]
        [InlineData("ILS", 2)]
        [InlineData("JPY", 0)]
        [InlineData("BHD", 3)]
        public void Should_give_the_decimal_places_an_amount_usually_carries(string code, int expected)
        {
            // Act
            var minorUnits = CurrencyCatalog.GetMinorUnits(code);

            // Assert
            Assert.Equal(expected, minorUnits);
        }

        [Fact]
        public void Should_refuse_a_code_that_is_not_a_currency_in_use()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CurrencyCatalog.GetMinorUnits("DEM"));
        }
    }

    public sealed class Symbols
    {
        [Theory]
        [InlineData("he-IL", "USD", "$")]
        [InlineData("en-US", "ILS", "₪")]
        [InlineData("he-IL", "ILS", "₪")]
        [InlineData("es-ES", "USD", "US$")]
        [InlineData("es-MX", "USD", "USD")]
        [InlineData("en-AU", "AUD", "$")]
        [InlineData("en-AU", "USD", "USD")]
        [InlineData("en-US", "AUD", "A$")]
        [InlineData("ja-JP", "JPY", "￥")]
        [InlineData("de-DE", "USD", "$")]
        public void Should_give_the_symbol_the_culture_writes_the_currency_with(string culture, string code, string expected)
        {
            // Act
            var symbol = CurrencyCatalog.GetSymbol(code, CultureInfo.GetCultureInfo(culture));

            // Assert
            Assert.Equal(expected, symbol);
        }

        [Theory]
        [InlineData("zh-TW", "CNY", "CN¥")]
        [InlineData("zh-TW", "TWD", "$")]
        [InlineData("zh-CN", "CNY", "¥")]
        [InlineData("zh-CN", "TWD", "NT$")]
        public void Should_start_from_the_locale_the_culture_derives_from_where_CLDR_has_no_locale_of_its_name(string culture, string code, string expected)
        {
            // Act
            var symbol = CurrencyCatalog.GetSymbol(code, CultureInfo.GetCultureInfo(culture));

            // Assert
            Assert.Equal(expected, symbol);
        }

        [Fact]
        public void Should_give_the_root_s_symbol_in_the_invariant_culture()
        {
            // Act
            var symbol = CurrencyCatalog.GetSymbol("USD", CultureInfo.InvariantCulture);

            // Assert
            Assert.Equal("US$", symbol);
        }

        [Fact]
        public void Should_give_the_code_where_no_locale_has_a_symbol_for_the_currency()
        {
            // Act
            var symbol = CurrencyCatalog.GetSymbol("BHD", CultureInfo.GetCultureInfo("en-US"));

            // Assert
            Assert.Equal("BHD", symbol);
        }

        [Fact]
        public void Should_refuse_a_code_that_is_not_a_currency_in_use()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CurrencyCatalog.GetSymbol("DEM", CultureInfo.InvariantCulture));
        }
    }

    public sealed class Amounts
    {
        [Theory]
        [InlineData(549, "USD", "en", "$549.00")]
        [InlineData(-1234.5, "USD", "en-US", "-$1,234.50")]
        [InlineData(549, "JPY", "en", "¥549")]
        [InlineData(549, "BHD", "en", "BHD\u00A0549.000")]
        public void Should_write_the_amount_as_the_culture_writes_money_with_the_currency_s_symbol_and_minor_units(double amount, string code, string culture, string expected)
        {
            // Act
            var text = CurrencyCatalog.FormatAmount((decimal)amount, code, CultureInfo.GetCultureInfo(culture));

            // Assert
            Assert.Equal(expected, text);
        }

        [Theory]
        [InlineData("EUR", "de-DE")]
        [InlineData("ILS", "he-IL")]
        [InlineData("USD", "en-US")]
        [InlineData("JPY", "ja-JP")]
        [InlineData("CHF", "de-CH")]
        public void Should_write_a_culture_s_own_currency_as_the_culture_s_own_format_does(string code, string culture)
        {
            // Arrange
            var formatCulture = CultureInfo.GetCultureInfo(culture);

            // Act
            var positive = CurrencyCatalog.FormatAmount(1234.5m, code, formatCulture);
            var negative = CurrencyCatalog.FormatAmount(-1234.5m, code, formatCulture);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1234.5m.ToString("C", formatCulture), positive),
                () => Assert.Equal((-1234.5m).ToString("C", formatCulture), negative));
        }

        [Fact]
        public void Should_write_a_currency_a_culture_has_no_symbol_of_its_own_for_in_that_culture()
        {
            // Act — a neutral culture names no currency of its own, which the culture's own currency format writes as ¤
            var text = CurrencyCatalog.FormatAmount(549, "USD", CultureInfo.GetCultureInfo("en"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("¤549.00", 549m.ToString("C", CultureInfo.GetCultureInfo("en"))),
                () => Assert.Equal("$549.00", text));
        }

        [Fact]
        public void Should_write_a_currency_in_the_reader_s_culture_rather_than_the_culture_s_own_currency()
        {
            // Act
            var text = CurrencyCatalog.FormatAmount(549, "USD", CultureInfo.GetCultureInfo("he-IL"), 0);

            // Assert
            Assert.Multiple(
                () => Assert.Contains("$", text, StringComparison.Ordinal),
                () => Assert.DoesNotContain("₪", text, StringComparison.Ordinal),
                () => Assert.Contains("549", text, StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(549, "CHF", "en", "CHF\u00A0549.00")]
        [InlineData(-549, "CHF", "en", "-CHF\u00A0549.00")]
        [InlineData(549, "USD", "en-AU", "USD\u00A0549.00")]
        [InlineData(549, "BRL", "en", "R$549.00")]
        public void Should_stand_a_symbol_ending_in_a_letter_a_no_break_space_from_the_digits(double amount, string code, string culture, string expected)
        {
            // Act
            var text = CurrencyCatalog.FormatAmount((decimal)amount, code, CultureInfo.GetCultureInfo(culture));

            // Assert
            Assert.Equal(expected, text);
        }

        [Theory]
        [InlineData(0, "$549")]
        [InlineData(3, "$549.250")]
        public void Should_write_the_decimal_places_asked_for_in_place_of_the_currency_s_minor_units(int decimalPlaces, string expected)
        {
            // Act
            var text = CurrencyCatalog.FormatAmount(549.25m, "USD", CultureInfo.GetCultureInfo("en"), decimalPlaces);

            // Assert
            Assert.Equal(expected, text);
        }

        [Fact]
        public void Should_refuse_a_code_outside_the_currencies_in_use()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => CurrencyCatalog.FormatAmount(5, "DEM", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(100)]
        public void Should_refuse_decimal_places_a_currency_amount_cannot_carry(int decimalPlaces)
        {
            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => CurrencyCatalog.FormatAmount(5, "USD", CultureInfo.InvariantCulture, decimalPlaces));
        }
    }
}
