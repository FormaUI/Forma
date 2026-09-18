namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Framework.Localization.Json.Tests.Fixtures.Grouped;
using Forma.Framework.Localization.Json.Tests.Fixtures.Locales;
using Forma.Tests.Localization.Rooted;
using Forma.Tests.Localization.Rooted.Bars;

public sealed class EmbeddedFilesTests
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

    public sealed class Nesting
    {
        [Fact]
        public void Should_localize_a_nested_type_through_its_outer_types_file()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Outer.Inner>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("outer file", localized.Value);
        }

        [Fact]
        public void Should_prefer_a_nested_types_own_file_over_the_outer_types()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Outer.Inner>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("nested type's file", localized.Value);
        }

        [Fact]
        public void Should_keep_the_outer_types_own_strings_at_the_top_level()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Outer>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("outer type's own", localized.Value);
        }

        [Fact]
        public void Should_drop_a_nested_types_arity()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Outer.InnerGeneric<int>>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("outer file", localized.Value);
        }

        [Fact]
        public void Should_drop_an_outer_types_arity()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<OuterGeneric<int>.Inner>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("generic outer file", localized.Value);
        }

        [Fact]
        public void Should_reach_a_nested_type_through_the_hosts_outer_file()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<Outer.Inner>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("host outer file", localized.Value);
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

    public sealed class Locales
    {
        [Fact]
        public void Should_read_the_folders_file_inside_its_locales_folder()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<DottedWidget>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("folder file inside .locales", localized.Value);
        }

        [Fact]
        public void Should_prefer_the_culture_file_inside_the_folders_locales_folder_over_the_folders_file()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<DottedWidget>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("culture file inside .locales", localized.Value);
        }

        [Fact]
        public void Should_prefer_the_types_file_inside_its_parents_locales_folder_over_the_folders_files()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<DottedWidget>();

            // Act
            var localized = strings["Close"];

            // Assert
            Assert.Equal("type file inside the parent's .locales", localized.Value);
        }
    }

    public sealed class Settings
    {
        [Fact]
        public void Should_read_from_the_root_an_embedded_settings_file_names()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<RootedWidget>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("root file", localized.Value);
        }

        [Fact]
        public void Should_read_the_file_named_for_an_assembly_whose_root_namespace_differs()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<RootedWidget>();

            // Act
            var localized = strings["Close"];

            // Assert
            Assert.Equal("assembly file", localized.Value);
        }

        [Fact]
        public void Should_read_a_nested_folder_by_the_paths_its_own_settings_name()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<RootedToolbar>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("nested folder file", localized.Value);
        }

        [Fact]
        public void Should_not_read_a_form_the_nested_settings_leave_out()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<RootedToolbar>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.True(localized.ResourceNotFound);
        }
    }
}
