namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures.Grouped;

public sealed class EmbeddedLocaleSourceTests
{
    private static IStringLocale CreateLocale<T>()
        => new JsonStringLocaleFactory().Create(typeof(T));

    public sealed class Layout
    {
        [Fact]
        public void Should_read_the_file_named_for_the_folder_from_inside_it()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<GroupedFirst>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("grouped file inside", localized.Value);
        }

        [Fact]
        public void Should_read_the_file_named_for_the_folder_from_beside_it()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<GroupedFirst>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("grouped file beside", localized.Value);
        }

        [Fact]
        public void Should_serve_every_type_in_the_folder_from_one_file()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<GroupedSecond>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("grouped file beside", localized.Value);
        }
    }

    public sealed class Precedence
    {
        [Fact]
        public void Should_prefer_the_file_named_for_the_type_over_the_folders_file()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<GroupedSecond>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("type file", localized.Value);
        }

        [Fact]
        public void Should_keep_the_folders_entry_for_a_key_the_types_file_lacks()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<GroupedSecond>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("grouped file inside", localized.Value);
        }
    }
}
