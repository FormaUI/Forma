namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class EmbeddedLocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(FakeAssembly assembly, FakeLocaleFileFormat format, string typeName = Toolbar)
        => new EmbeddedLocaleSource(assembly, string.Empty, format).Read(assembly.GetName().Name!, typeName, "he");

    private static void AddFile(FakeAssembly assembly, FakeLocaleFileFormat format, string name, LocaleSection root)
    {
        assembly.Resources[name] = string.Empty;
        format.Files[name] = new LocaleFile(root, null);
    }

    public sealed class Roots
    {
        [Fact]
        public void Should_begin_the_names_where_an_embedded_settings_file_sits()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Strings..locali.json"] = "{}" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Strings.Bars.Toolbar.he.fake", CreateEntries(("Bold", "beneath the root")));
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar.he.fake", CreateEntries(("Bold", "above the root")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Strings.Bars.Toolbar.he.fake", string.Empty, "Bold", "beneath the root")], entries);
        }

        [Fact]
        public void Should_count_anchors_from_the_root_namespace_the_settings_name()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.Widgets.Core") { Resources = { ["Contoso..locali.json"] = """{ "rootNamespace": "Contoso.Widgets" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Bars.Toolbar.he.fake", CreateEntries(("Bold", "declared root")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Bars.Toolbar.he.fake", string.Empty, "Bold", "declared root")], entries);
        }

        [Fact]
        public void Should_read_the_file_named_for_an_assembly_whose_root_namespace_differs()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.Widgets.Core") { Resources = { ["Contoso..locali.json"] = """{ "rootNamespace": "Contoso.Widgets" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Contoso.Widgets.Core.he.fake", CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "assembly file")))));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Contoso.Widgets.Core.he.fake", "Bars.Toolbar", "Bold", "assembly file")], entries);
        }

        [Fact]
        public void Should_refuse_two_settings_files_that_each_mark_a_root()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly)
            {
                Resources =
                {
                    ["Contoso.Widgets.Bars..locali.json"] = "{}",
                    ["Contoso.Widgets.Menus..locali.json"] = "{}",
                },
            };

            // Act
            var thrown = Assert.Throws<LocaliSettingsException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains("Contoso.Widgets.Bars..locali.json", thrown.Message, StringComparison.Ordinal),
                () => Assert.Contains("Contoso.Widgets.Menus..locali.json", thrown.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_say_which_settings_file_is_malformed()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets..locali.json"] = """{ "rootDir": "/strings" }""" } };

            // Act
            var thrown = Assert.Throws<LocaliSettingsException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Contains("Contoso.Widgets..locali.json", thrown.Message, StringComparison.Ordinal);
        }
    }

    public sealed class RootDirs
    {
        [Theory]
        [InlineData("Contoso.Widgets..locali.json", "Strings/Main", "Contoso.Widgets.Strings.Main.Bars.Toolbar.he.fake")]
        [InlineData("Contoso.Widgets.Build..locali.json", "../Contoso.Widgets.csproj", "Contoso.Widgets.Bars.Toolbar.he.fake")]
        [InlineData("Contoso.Widgets.Src.Core..locali.json", "../../Contoso.slnx", "Contoso.Widgets.Bars.Toolbar.he.fake")]
        [InlineData("Contoso.Widgets..locali.json", "./Contoso.Widgets.csproj", "Contoso.Widgets.Bars.Toolbar.he.fake")]
        public void Should_begin_the_names_where_the_root_dir_leads(string settingsName, string rootDir, string fileName)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { [settingsName] = $$"""{ "rootDir": "{{rootDir}}" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, fileName, CreateEntries(("Bold", "found")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fileName, string.Empty, "Bold", "found")], entries);
        }

        [Fact]
        public void Should_begin_the_names_at_the_root_namespace_when_the_root_dir_climbs_past_it()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Deep.Nest..locali.json"] = """{ "rootNamespace": "Contoso.Widgets", "rootDir": "../../../.." }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar.he.fake", CreateEntries(("Bold", "at the root namespace")));
            AddFile(assembly, format, "Contoso.Widgets.Deep.Nest.Bars.Toolbar.he.fake", CreateEntries(("Bold", "where the settings sit")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.Toolbar.he.fake", string.Empty, "Bold", "at the root namespace")], entries);
        }
    }

    public sealed class Nesting
    {
        private static FakeAssembly CreateNestedAssembly()
            => new(Assembly)
            {
                Resources =
                {
                    ["Contoso.Widgets..locali.json"] = "{}",
                    ["Contoso.Widgets.Bars..locali.json"] = """{ "localesDir": "i18n", "paths": ["$RootDir/$AnchorName/$LocalesDir/$Culture$Extension"] }""",
                },
            };

        [Fact]
        public void Should_read_the_names_beneath_a_nested_settings_file_by_its_paths_alone()
        {
            // Arrange
            var assembly = CreateNestedAssembly();
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars.i18n.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "nested form")))));
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar.he.fake", CreateEntries(("Bold", "default form")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.i18n.he.fake", "Toolbar", "Bold", "nested form")], entries);
        }

        [Fact]
        public void Should_keep_the_roots_paths_outside_the_nested_folder()
        {
            // Arrange
            var assembly = CreateNestedAssembly();
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Menus.Menu.he.fake", CreateEntries(("Open", "default form")));

            // Act
            var entries = Read(assembly, format, "Contoso.Widgets.Menus.Menu");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Menus.Menu.he.fake", string.Empty, "Open", "default form")], entries);
        }
    }

    public sealed class Locales
    {
        [Fact]
        public void Should_read_every_locales_form_anchor_by_anchor_and_path_by_path()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "culture file")))));
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.Bars.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "folder file inside")))));
            AddFile(assembly, format, "Contoso.Widgets..locales.Bars.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "folder file in the parent's")))));
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.Toolbar.he.fake", CreateEntries(("Bold", "type file")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal(
                [
                    new("Contoso.Widgets..locales.Bars.he.fake", "Toolbar", "Bold", "folder file in the parent's"),
                    new("Contoso.Widgets.Bars..locales.Bars.he.fake", "Toolbar", "Bold", "folder file inside"),
                    new("Contoso.Widgets.Bars..locales.he.fake", "Toolbar", "Bold", "culture file"),
                    new("Contoso.Widgets.Bars..locales.Toolbar.he.fake", string.Empty, "Bold", "type file"),
                ],
                entries);
        }

        [Fact]
        public void Should_read_the_locales_file_at_the_root()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets..locales.he.fake", CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "root file")))));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets..locales.he.fake", "Bars.Toolbar", "Bold", "root file")], entries);
        }
    }

    public sealed class Satellites
    {
        [Fact]
        public void Should_read_a_satellites_file_under_its_name_with_the_culture()
        {
            // Arrange
            var satellite = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Bars.Toolbar.fake"] = string.Empty } };
            var assembly = new FakeAssembly(Assembly) { Satellites = { ["he"] = satellite } };
            var format = new FakeLocaleFileFormat { Files = { ["Contoso.Widgets.Bars.Toolbar.he.fake"] = new(CreateEntries(("Bold", "satellite file")), null) } };

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.Toolbar.he.fake", string.Empty, "Bold", "satellite file")], entries);
        }

        [Fact]
        public void Should_read_the_assemblys_own_file_once_when_a_satellite_holds_one_of_the_same_name()
        {
            // Arrange
            var satellite = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Bars.Toolbar.fake"] = string.Empty } };
            var assembly = new FakeAssembly(Assembly) { Satellites = { ["he"] = satellite } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar.he.fake", CreateEntries(("Bold", "one file")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.Toolbar.he.fake", string.Empty, "Bold", "one file")], entries);
        }
    }

    public sealed class OutsideTheRootNamespace
    {
        [Fact]
        public void Should_read_a_type_outside_the_root_namespace_by_its_full_name_from_the_start()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Fabrikam.Gadgets.Dial.he.fake", CreateEntries(("Turn", "full name")));
            AddFile(assembly, format, "Contoso.Widgets.Fabrikam.Gadgets.Dial.he.fake", CreateEntries(("Turn", "beneath the assembly")));

            // Act
            var entries = Read(assembly, format, "Fabrikam.Gadgets.Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Fabrikam.Gadgets.Dial.he.fake", string.Empty, "Turn", "full name")], entries);
        }

        [Fact]
        public void Should_read_a_type_outside_any_namespace_beneath_the_root()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Dial.he.fake", CreateEntries(("Turn", "beneath the root")));

            // Act
            var entries = Read(assembly, format, "Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Dial.he.fake", string.Empty, "Turn", "beneath the root")], entries);
        }
    }
}
