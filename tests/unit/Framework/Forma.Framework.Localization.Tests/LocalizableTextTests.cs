namespace Forma.Framework.Localization.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Localization;

using Xunit;

public sealed class LocalizableTextTests
{
    private static readonly Dictionary<string, string> Spanish = new()
    {
        ["Media"] = "Medios",
        ["{Count} stories in {Section}"] = "{Count} historias en {Section}",
        ["Sports"] = "Deportes",
    };

    public sealed class Creating
    {
        [Fact]
        public void Should_name_its_source_its_key_and_its_values()
        {
            // Act
            var text = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", 3, "Sports");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(typeof(LocalizableTextTests), text.Source),
                () => Assert.Equal("{Count} stories in {Section}", text.Key),
                () => Assert.Equal([3, "Sports"], text.Arguments));
        }

        [Fact]
        public void Should_equal_text_of_the_same_source_key_and_values()
        {
            // Arrange
            var text = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", 3, "Sports");

            // Act
            var same = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", 3, "Sports");
            var other = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", 4, "Sports");

            // Assert
            Assert.Multiple(
                () => Assert.Equal(text, same),
                () => Assert.Equal(text.GetHashCode(), same.GetHashCode()),
                () => Assert.NotEqual(text, other));
        }

        [Fact]
        public void Should_keep_its_values_from_the_array_it_was_given()
        {
            // Arrange
            object?[] values = [3, "Sports"];
            var text = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", values);

            // Act
            values[0] = 4;

            // Assert
            Assert.Equal([3, "Sports"], text.Arguments);
        }
    }

    public sealed class Localizing
    {
        [Fact]
        public void Should_look_its_key_up_in_its_sources_locale_when_shown()
        {
            // Arrange
            using var fixture = new CultureFixture("es");
            var factory = new StubStringLocaleFactory(Spanish);
            var text = LocalizableText.For<LocalizableTextTests>("Media");

            // Act
            var localized = factory.Localize(text);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Medios", localized.Value),
                () => Assert.Equal([typeof(LocalizableTextTests)], factory.RequestedTypes));
        }

        [Fact]
        public void Should_fill_its_holes_localizing_a_value_that_is_localizable_text_itself()
        {
            // Arrange
            using var fixture = new CultureFixture("es");
            var factory = new StubStringLocaleFactory(Spanish);
            var text = LocalizableText.For<LocalizableTextTests>("{Count} stories in {Section}", 3, LocalizableText.For<LocalizableTextTests>("Sports"));

            // Act
            var localized = factory.Localize(text);

            // Assert
            Assert.Equal("3 historias en Deportes", localized.Value);
        }

        [Fact]
        public void Should_format_its_values_in_the_culture_states_culture()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var factory = new StubStringLocaleFactory(new Dictionary<string, string> { ["{Total} raised"] = "{Total} recaudados" });
            var state = new FakeCultureState(CultureInfo.GetCultureInfo("de-DE"));
            var text = LocalizableText.For<LocalizableTextTests>("{Total} raised", 1234.5m);

            // Act
            var localized = factory.Localize(text, state);

            // Assert
            Assert.Equal("1234,5 recaudados", localized.Value);
        }
    }
}
