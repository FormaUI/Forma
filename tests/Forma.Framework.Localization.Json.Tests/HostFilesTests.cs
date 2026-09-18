namespace Forma.Framework.Localization.Json.Tests;

using System.Text.Json;

using Forma.Framework.Localization.Json.Tests.Fixtures;

public sealed class HostFilesTests
{
    private static IStringLocale CreateLocale<T>(LocalizationOptions? options = null)
        => new JsonStringLocaleFactory(options ?? new LocalizationOptions()).Create(typeof(T));

    public sealed class Precedence
    {
        [Fact]
        public void Should_keep_the_assemblys_entry_when_no_host_file_names_it()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("פתח", localized.Value);
        }

        [Fact]
        public void Should_prefer_the_file_named_for_the_assembly_over_the_assemblys_entry()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("assembly file", localized.Value);
        }

        [Fact]
        public void Should_prefer_a_file_named_for_part_of_the_namespace_over_the_assembly_file()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Cancel"];

            // Assert
            Assert.Equal("namespace file", localized.Value);
        }

        [Fact]
        public void Should_prefer_the_file_named_for_the_type_over_every_other()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Delete"];

            // Assert
            Assert.Equal("type file", localized.Value);
        }

        [Fact]
        public void Should_add_a_key_the_assembly_never_had()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Export"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("added", localized.Value),
                () => Assert.False(localized.ResourceNotFound));
        }

        [Fact]
        public void Should_prefer_a_host_file_in_a_form_the_assembly_does_not_use()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostDotted>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("host file inside .locales", localized.Value);
        }

        [Fact]
        public void Should_keep_the_assemblys_entry_the_host_file_in_another_form_leaves_out()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostDotted>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("embedded file", localized.Value);
        }
    }

    public sealed class Roots
    {
        private static IStringLocale CreateRootedLocale<T>()
            => CreateLocale<T>(new LocalizationOptions { LocalesPath = "Hosts/Rooted" });

        [Fact]
        public void Should_complete_the_types_name_from_the_root_the_hosts_settings_name()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateRootedLocale<Contoso.Widgets.ForeignRoot>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("type file under a declared root", localized.Value);
        }

        [Fact]
        public void Should_read_the_file_named_for_the_assembly_beneath_a_root_it_does_not_name()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateRootedLocale<Contoso.Widgets.ForeignRoot>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("assembly file under a declared root", localized.Value);
        }

        [Fact]
        public void Should_ignore_a_file_whose_path_does_not_meet_the_declared_root()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateRootedLocale<Contoso.Widgets.ForeignRoot>();

            // Act
            var localized = strings["Delete"];

            // Assert
            Assert.True(localized.ResourceNotFound);
        }
    }

    public sealed class Layout
    {
        [Fact]
        public void Should_read_a_section_nested_as_objects()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("Guardar", localized.Value);
        }

        [Fact]
        public void Should_lay_files_out_by_the_types_name_rather_than_the_embedded_folder()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<PathScoped>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("type file", localized.Value);
        }

        [Fact]
        public void Should_lay_files_out_by_the_full_name_of_a_type_rooted_outside_the_assembly()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Contoso.Widgets.ForeignRoot>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("type file", localized.Value);
        }

        [Fact]
        public void Should_read_the_assemblys_file_from_inside_the_assemblys_folder()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("assembly file inside the folder", localized.Value);
        }

        [Fact]
        public void Should_read_a_namespace_file_from_inside_its_folder()
        {
            // Arrange
            using var culture = new CultureScope("es");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Cancel"];

            // Assert
            Assert.Equal("namespace file inside the folder", localized.Value);
        }

        [Fact]
        public void Should_treat_a_null_value_as_the_key_itself()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var localized = strings["Reset"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Reset", localized.Value),
                () => Assert.False(localized.ResourceNotFound));
        }
    }

    public sealed class Cultures
    {
        [Fact]
        public void Should_localize_a_culture_the_assembly_does_not_ship()
        {
            // Arrange
            using var culture = new CultureScope("fr");
            var strings = CreateLocale<HostLocalized>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("Enregistrer", localized.Value);
        }

        [Fact]
        public void Should_walk_a_regional_culture_up_to_the_hosts_file()
        {
            // Arrange
            using var culture = new CultureScope("fr-CA");
            var strings = CreateLocale<HostLocalized>();

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Equal("Enregistrer", localized.Value);
        }

        [Fact]
        public void Should_list_the_hosts_keys_among_all_strings()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>();

            // Act
            var names = strings.GetAllStrings(includeParentCultures: false).Select(text => text.Name).ToList();

            // Assert
            Assert.Multiple(
                () => Assert.Contains("Export", names),
                () => Assert.Contains("Open", names),
                () => Assert.Equal(names.Count, names.Distinct(StringComparer.Ordinal).Count()));
        }
    }

    public sealed class Configuration
    {
        [Fact]
        public void Should_read_no_host_file_when_the_path_is_empty()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>(new LocalizationOptions { LocalesPath = string.Empty });

            // Act
            var localized = strings["Delete"];

            // Assert
            Assert.Equal("מחק", localized.Value);
        }

        [Fact]
        public void Should_read_no_host_file_when_the_folder_does_not_exist()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostOverridden>(new LocalizationOptions { LocalesPath = "nowhere" });

            // Act
            var localized = strings["Delete"];

            // Assert
            Assert.Equal("מחק", localized.Value);
        }
    }

    public sealed class Faults
    {
        [Fact]
        public void Should_reject_a_host_file_carrying_no_metadata()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostUnmarked>();

            // Act
            var thrown = Assert.Throws<LocalizationMetadataException>(() => strings["Save"]);

            // Assert
            Assert.Contains("HostUnmarked.he.json", thrown.Message, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_reject_a_value_that_is_not_text()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<HostMistyped>();

            // Act
            var thrown = Assert.Throws<JsonException>(() => strings["Save"]);

            // Assert
            Assert.Contains("'Save'", thrown.Message, StringComparison.Ordinal);
        }
    }
}
