namespace Forma.Framework.Localization.Tests;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.Tests.Shared;

using Xunit;

public sealed class NullStringLocaleFactoryTests
{
    [Fact]
    public void Should_be_one_instance()
    {
        // Act
        IStringLocaleFactory first = NullStringLocaleFactory.Instance;
        IStringLocaleFactory second = NullStringLocaleFactory.Instance;

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public void Should_create_a_locale_for_any_type_and_any_base_name()
    {
        // Arrange
        var factory = NullStringLocaleFactory.Instance;

        // Act
        var byType = factory.Create(typeof(NullStringLocaleFactoryTests));
        var byName = factory.Create("Forma.Blazor.Components.Layout.FormaFrame", "Forma.Blazor");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.NotNull(byType);
            Assert.NotNull(byName);
        });
    }

    [Fact]
    public void Should_reject_a_missing_type_or_a_blank_name()
    {
        // Arrange
        var factory = NullStringLocaleFactory.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
        Assert.Throws<ArgumentException>(() => factory.Create(" ", "Forma.Blazor"));
        Assert.Throws<ArgumentException>(() => factory.Create("Forma.Blazor.FormaFrame", " "));
    }

    public sealed class Locale
    {
        [Fact]
        public void Should_answer_a_key_with_itself()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act
            var text = locale["Open a Placed Frame"];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal("Open a Placed Frame", text.Value);
                Assert.Equal("Open a Placed Frame", text.Name);
                Assert.True(text.ResourceNotFound);
            });
        }

        [Fact]
        public void Should_fill_the_holes_of_a_key_by_position()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act
            var text = locale["Move {Name} with the arrow keys; hold {Modifier} to resize it", "Inspector", "Shift"];

            // Assert
            Assert.Equal("Move Inspector with the arrow keys; hold Shift to resize it", text.Value);
        }

        [Fact]
        public void Should_resolve_a_plural_expression_for_the_source_language()
        {
            // Arrange
            using var culture = new CultureScope("he-IL");
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act
            var one = locale["[$ plural; one: # file; other: # files; $]", 1];
            var many = locale["[$ plural; one: # file; other: # files; $]", 3];

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal("1 file", one.Value);
                Assert.Equal("3 files", many.Value);
            });
        }

        [Fact]
        public void Should_leave_the_holes_open_in_a_pattern()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act
            var pattern = locale.Resolve("{Count} lines of output", 4);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.Equal("{Count} lines of output", pattern.Format);
                Assert.Single(pattern.Arguments);
                Assert.True(pattern.ResourceNotFound);
            });
        }

        [Fact]
        public void Should_fill_a_bound_locales_holes_in_its_culture_and_leave_the_original_reading_the_current_one()
        {
            // Arrange
            using var culture = new CultureFixture("en");
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act
            var bound = locale.WithCulture(CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("he"))["Stored {Amount}", 1.5];
            var original = locale["Stored {Amount}", 1.5];

            // Assert
            Assert.Equal(["Stored 1,5", "Stored 1.5"], [bound.Value, original.Value]);
        }

        [Fact]
        public void Should_reject_a_missing_culture_to_bind()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));
            var english = CultureInfo.GetCultureInfo("en");

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => locale.WithCulture(null!, english));
            Assert.Throws<ArgumentNullException>(() => locale.WithCulture(english, null!));
        }

        [Fact]
        public void Should_list_no_strings()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act & Assert
            Assert.Empty(locale.GetAllStrings(includeParentCultures: true));
        }

        [Fact]
        public void Should_reject_an_empty_key()
        {
            // Arrange
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Locale));

            // Act & Assert
            Assert.Throws<ArgumentException>(() => locale[string.Empty]);
            Assert.Throws<ArgumentException>(() => locale.Resolve(string.Empty));
        }
    }
}
