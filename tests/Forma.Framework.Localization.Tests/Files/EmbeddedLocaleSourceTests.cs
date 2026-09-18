namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class EmbeddedLocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(FakeAssembly assembly, FakeLocaleFileFormat format, string typeName = Toolbar)
        => new EmbeddedLocaleSource(assembly, format).Read(assembly.GetName().Name!, typeName, "he");

    private static void AddFile(FakeAssembly assembly, FakeLocaleFileFormat format, string name, LocaleSection root)
    {
        assembly.Resources[name] = string.Empty;
        format.Files[name] = new LocaleFile(root);
    }

    private static LocaleSection CreateToolbarSection(string value)
        => CreateSections(("Toolbar", CreateEntries(("Bold", value))));

    public sealed class Locales
    {
        [Fact]
        public void Should_read_each_folders_locales_file_from_the_root_down_to_the_types_own()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets..locales.he.fake", CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "root file")))));
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.he.fake", CreateToolbarSection("folder file"));
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar..locales.he.fake", CreateEntries(("Bold", "type's own file")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal(
                [
                    new("Contoso.Widgets..locales.he.fake", "Bars.Toolbar", "Bold", "root file"),
                    new("Contoso.Widgets.Bars..locales.he.fake", "Toolbar", "Bold", "folder file"),
                    new("Contoso.Widgets.Bars.Toolbar..locales.he.fake", string.Empty, "Bold", "type's own file"),
                ],
                entries);
        }

        [Fact]
        public void Should_not_read_a_file_beside_its_type_unless_a_setting_names_that_form()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars.Toolbar.he.fake", CreateEntries(("Bold", "beside the type")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Empty(entries);
        }
    }

    public sealed class Roots
    {
        [Fact]
        public void Should_begin_the_names_where_the_embedded_config_file_sits()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Strings..locale.json"] = "{}" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Strings.Bars..locales.he.fake", CreateToolbarSection("beneath the root"));
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.he.fake", CreateToolbarSection("above the root"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Strings.Bars..locales.he.fake", "Toolbar", "Bold", "beneath the root")], entries);
        }

        [Fact]
        public void Should_count_anchors_from_the_root_namespace_the_config_names()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.Widgets.Core") { Resources = { ["Contoso..locale.json"] = """{ "rootNamespace": "Contoso.Widgets" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Bars..locales.he.fake", CreateToolbarSection("declared root"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Bars..locales.he.fake", "Toolbar", "Bold", "declared root")], entries);
        }

        [Fact]
        public void Should_read_the_file_named_for_an_assembly_whose_root_namespace_differs()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.Widgets.Core")
            {
                Resources = { ["Contoso..locale.json"] = """{ "rootNamespace": "Contoso.Widgets", "paths": ["$RootDir/$AssemblyName.$Culture$Extension"] }""" },
            };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Contoso.Widgets.Core.he.fake", CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "assembly file")))));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Contoso.Widgets.Core.he.fake", "Bars.Toolbar", "Bold", "assembly file")], entries);
        }

        [Fact]
        public void Should_refuse_two_config_files_for_one_assembly()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly)
            {
                Resources =
                {
                    ["Contoso.Widgets.Bars..locale.json"] = "{}",
                    ["Contoso.Widgets.Menus..locale.json"] = "{}",
                },
            };

            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains("Contoso.Widgets.Bars..locale.json", thrown.Message, StringComparison.Ordinal),
                () => Assert.Contains("Contoso.Widgets.Menus..locale.json", thrown.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_say_which_config_file_is_malformed()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets..locale.json"] = """{ "rootDir": "/strings" }""" } };

            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Contains("Contoso.Widgets..locale.json", thrown.Message, StringComparison.Ordinal);
        }
    }

    public sealed class RootDirs
    {
        [Theory]
        [InlineData("Contoso.Widgets..locale.json", "Strings/Main", "Contoso.Widgets.Strings.Main.Bars..locales.he.fake")]
        [InlineData("Contoso.Widgets.Build..locale.json", "../Contoso.Widgets.csproj", "Contoso.Widgets.Bars..locales.he.fake")]
        [InlineData("Contoso.Widgets.Src.Core..locale.json", "../../Contoso.slnx", "Contoso.Widgets.Bars..locales.he.fake")]
        [InlineData("Contoso.Widgets..locale.json", "./Contoso.Widgets.csproj", "Contoso.Widgets.Bars..locales.he.fake")]
        public void Should_begin_the_names_where_the_root_dir_leads(string configName, string rootDir, string fileName)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { [configName] = $$"""{ "rootDir": "{{rootDir}}" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, fileName, CreateToolbarSection("found"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fileName, "Toolbar", "Bold", "found")], entries);
        }

        [Fact]
        public void Should_begin_the_names_at_the_root_namespace_when_the_root_dir_climbs_past_it()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Deep.Nest..locale.json"] = """{ "rootNamespace": "Contoso.Widgets", "rootDir": "../../../.." }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets.Bars..locales.he.fake", CreateToolbarSection("at the root namespace"));
            AddFile(assembly, format, "Contoso.Widgets.Deep.Nest.Bars..locales.he.fake", CreateToolbarSection("where the settings sit"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars..locales.he.fake", "Toolbar", "Bold", "at the root namespace")], entries);
        }
    }

    // A file named for its culture alone never reaches a satellite, so these read a form the build moves there.
    public sealed class Satellites
    {
        private static FakeAssembly CreateAssemblyReadingBeside()
            => new(Assembly) { Resources = { ["Contoso.Widgets..locale.json"] = """{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }""" } };

        [Fact]
        public void Should_read_a_satellites_file_under_its_name_with_the_culture()
        {
            // Arrange
            var assembly = CreateAssemblyReadingBeside();
            assembly.Satellites["he"] = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Bars.Toolbar.fake"] = string.Empty } };
            var format = new FakeLocaleFileFormat { Files = { ["Contoso.Widgets.Bars.Toolbar.he.fake"] = new(CreateEntries(("Bold", "satellite file"))) } };

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.Toolbar.he.fake", string.Empty, "Bold", "satellite file")], entries);
        }

        [Fact]
        public void Should_read_the_assemblys_own_file_once_when_a_satellite_holds_one_of_the_same_name()
        {
            // Arrange
            var assembly = CreateAssemblyReadingBeside();
            assembly.Satellites["he"] = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Bars.Toolbar.fake"] = string.Empty } };
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
            AddFile(assembly, format, "Fabrikam.Gadgets..locales.he.fake", CreateSections(("Dial", CreateEntries(("Turn", "full name")))));
            AddFile(assembly, format, "Contoso.Widgets.Fabrikam.Gadgets..locales.he.fake", CreateSections(("Dial", CreateEntries(("Turn", "beneath the assembly")))));

            // Act
            var entries = Read(assembly, format, "Fabrikam.Gadgets.Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Fabrikam.Gadgets..locales.he.fake", "Dial", "Turn", "full name")], entries);
        }

        [Fact]
        public void Should_read_a_type_outside_any_namespace_beneath_the_root()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets..locales.he.fake", CreateSections(("Dial", CreateEntries(("Turn", "beneath the root")))));

            // Act
            var entries = Read(assembly, format, "Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets..locales.he.fake", "Dial", "Turn", "beneath the root")], entries);
        }
    }
}
