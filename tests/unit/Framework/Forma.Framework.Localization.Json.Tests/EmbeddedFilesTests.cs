namespace Forma.Framework.Localization.Json.Tests;

using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Framework.Localization.Json.Tests.Fixtures.Conflicted;
using Forma.Framework.Localization.Json.Tests.Fixtures.Grouped;
using Forma.Tests.Localization.Rooted;
using Forma.Tests.Localization.Rooted.Bars;

using Microsoft.Extensions.Logging;

public sealed class EmbeddedFilesTests
{
    private static IStringLocale CreateLocale<T>()
        => new JsonStringLocaleFactory().Create(typeof(T));

    public sealed class Layout
    {
        [Fact]
        public void Should_read_the_file_in_the_folders_own_locales_folder()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<GroupedFirst>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("folder file", localized.Value);
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
            Assert.Equal("folder file", localized.Value);
        }
    }

    public sealed class Conflicts
    {
        [Fact]
        public void Should_show_the_text_of_the_file_whose_name_sorts_last_and_warn_of_the_conflict_once()
        {
            // Arrange — the type's folder's file stands closest, and a rooted section in another folder's file, whose name sorts last, localizes the same key.
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = new JsonStringLocaleFactory(new LocalizationOptions(), loggerFactory).Create(typeof(Conflicted));

            // Act
            var saved = strings["Save"];
            var opened = strings["Open"];

            // Assert
            var warning = Assert.Single(loggerFactory.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains(".Conflicted.", StringComparison.Ordinal));
            Assert.Multiple(
                () => Assert.Equal("rooted section", saved.Value),
                () => Assert.Equal("folder file", opened.Value),
                () => Assert.Contains("Fixtures/Conflicted/.locales/he.json", warning.Message, StringComparison.Ordinal),
                () => Assert.Contains("Fixtures/Wording/.locales/he.json", warning.Message, StringComparison.Ordinal),
                () => Assert.Contains("'Save'", warning.Message, StringComparison.Ordinal));
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
        public void Should_settle_a_key_the_outer_and_nested_types_files_both_give_by_the_file_whose_name_sorts_last()
        {
            // Arrange — the outer type's file and the nested type's own both localize the key, and the nested type's name sorts last.
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
        public void Should_settle_a_key_the_types_folder_and_its_parents_file_both_give_by_the_file_whose_name_sorts_last()
        {
            // Arrange — the type's own folder's file and its parent folder's both localize the key, and the type folder's name sorts last.
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
            Assert.Equal("folder file", localized.Value);
        }
    }

    public sealed class Configs
    {
        [Fact]
        public void Should_read_from_the_root_an_embedded_config_file_names()
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
        public void Should_read_the_file_named_for_the_assembly_by_a_path_its_config_names()
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
        public void Should_read_a_folder_beneath_a_declared_root_by_the_roots_paths()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<RootedToolbar>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("folder file", localized.Value);
        }
    }
}
