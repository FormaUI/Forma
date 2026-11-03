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
}
