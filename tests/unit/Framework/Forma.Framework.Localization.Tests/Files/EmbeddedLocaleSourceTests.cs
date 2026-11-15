namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Framework.Localization.Tests.Support.LocaleSections;

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
        // The names the build gives the root's, a folder's and a type's own file, and the names the SDK gives them, which a
        // project's own item keeps.
        [Theory]
        [InlineData("Contoso.Widgets/.locales/he.fake", "Contoso.Widgets/Bars/.locales/he.fake", "Contoso.Widgets/Bars/Toolbar/.locales/he.fake")]
        [InlineData("Contoso.Widgets..locales.he.fake", "Contoso.Widgets.Bars..locales.he.fake", "Contoso.Widgets.Bars.Toolbar..locales.he.fake")]
        public void Should_read_each_folders_locales_file_from_the_root_down_to_the_types_own(string rootFile, string folderFile, string typeFile)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, rootFile, CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "root file")))));
            AddFile(assembly, format, folderFile, CreateSections(("Toolbar", CreateEntries(("Italic", "folder file")))));
            AddFile(assembly, format, typeFile, CreateEntries(("Underline", "type's own file")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal(
                [
                    new(rootFile, "Bars.Toolbar", "Bold", "root file"),
                    new(folderFile, "Toolbar", "Italic", "folder file"),
                    new(typeFile, string.Empty, "Underline", "type's own file"),
                ],
                [.. entries.OrderBy(entry => entry.Key, StringComparer.Ordinal)]);
        }

        [Fact]
        public void Should_read_a_file_beside_its_type_outside_any_folder_of_locale_files()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets/Bars/Toolbar.he.fake", CreateEntries(("Bold", "beside the type")));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/Toolbar.he.fake", string.Empty, "Bold", "beside the type")], entries);
        }

        [Fact]
        public void Should_read_a_folder_and_a_dot_naming_one_namespace_alike()
        {
            // Arrange — Bars/he.fake and Bars.he.fake, which the build embeds under names of their own.
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets/Bars/he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "folder")))));
            AddFile(assembly, format, "Contoso.Widgets/Bars.he.fake", CreateSections(("Toolbar", CreateEntries(("Italic", "dot")))));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal(
                [
                    new("Contoso.Widgets/Bars/he.fake", "Toolbar", "Bold", "folder"),
                    new("Contoso.Widgets/Bars.he.fake", "Toolbar", "Italic", "dot"),
                ],
                [.. entries.OrderBy(entry => entry.Key, StringComparer.Ordinal)]);
        }
    }

    public sealed class Roots
    {
        [Fact]
        public void Should_begin_the_names_where_the_embedded_config_file_sits()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/Strings/.locale.json"] = "{}" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets/Strings/Bars/.locales/he.fake", CreateToolbarSection("beneath the root"));
            AddFile(assembly, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("above the root"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Strings/Bars/.locales/he.fake", "Toolbar", "Bold", "beneath the root")], entries);
        }

        [Fact]
        public void Should_count_anchors_from_the_root_namespace_the_config_names()
        {
            // Arrange — an assembly whose project's root namespace, Contoso, isn't its name.
            var assembly = new FakeAssembly("Contoso.Widgets.Core") { Resources = { ["Contoso/.locale.json"] = """{ "rootNamespace": "Contoso.Widgets" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso/Bars/.locales/he.fake", CreateToolbarSection("declared root"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso/Bars/.locales/he.fake", "Toolbar", "Bold", "declared root")], entries);
        }

        [Fact]
        public void Should_read_the_file_named_for_an_assembly_whose_root_namespace_differs()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.Widgets.Core")
            {
                Resources = { ["Contoso/.locale.json"] = """{ "rootNamespace": "Contoso.Widgets", "paths": ["$RootDir/$AssemblyName.$Culture$Extension"] }""" },
            };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso/Contoso.Widgets.Core.he.fake", CreateSections(("Bars.Toolbar", CreateEntries(("Bold", "assembly file")))));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso/Contoso.Widgets.Core.he.fake", "Bars.Toolbar", "Bold", "assembly file")], entries);
        }

        [Fact]
        public void Should_refuse_two_config_files_for_one_assembly()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly)
            {
                Resources =
                {
                    ["Contoso.Widgets/Bars/.locale.json"] = "{}",
                    ["Contoso.Widgets/Menus/.locale.json"] = "{}",
                },
            };

            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains("Contoso.Widgets/Bars/.locale.json", thrown.Message, StringComparison.Ordinal),
                () => Assert.Contains("Contoso.Widgets/Menus/.locale.json", thrown.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_say_which_config_file_is_malformed()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/.locale.json"] = """{ "rootDir": "/strings" }""" } };

            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(assembly, new FakeLocaleFileFormat()));

            // Assert
            Assert.Contains("Contoso.Widgets/.locale.json", thrown.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData(null, "Contoso.Widgets", ".locales")]
        [InlineData("{}", "Contoso.Widgets", ".locales")]
        [InlineData("""{ "rootDir": "Strings", "localesDir": ".strings" }""", "Contoso.Widgets/Strings", ".strings")]
        public void Should_answer_where_the_names_begin_and_the_folder_of_locale_files_they_leave_out(string? config, string rootDir, string localesDir)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            if (config is not null)
            {
                assembly.Resources["Contoso.Widgets/.locale.json"] = config;
            }

            var source = new EmbeddedLocaleSource(assembly, new FakeLocaleFileFormat());

            // Act
            var folders = (source.GetRootDir(Assembly), source.GetLocalesDir(Assembly));

            // Assert
            Assert.Equal((rootDir, localesDir), folders);
        }

        [Theory]
        [InlineData("Contoso.WidgetsExtra/Bars/.locales/he.fake")]
        [InlineData("Contoso/Bars/.locales/he.fake")]
        public void Should_read_nothing_from_a_folder_beside_the_assembly_s_root(string name)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, name, CreateToolbarSection("elsewhere"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Empty(entries);
        }
    }

    public sealed class RootDirs
    {
        [Theory]
        [InlineData("Strings", "Contoso.Widgets/Strings/Bars/.locales/he.fake")]
        [InlineData("Strings/Main", "Contoso.Widgets/Strings/Main/Bars/.locales/he.fake")]
        public void Should_begin_the_names_where_the_root_dir_leads(string rootDir, string fileName)
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/.locale.json"] = $$"""{ "rootDir": "{{rootDir}}" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, fileName, CreateToolbarSection("found"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fileName, "Toolbar", "Bold", "found")], entries);
        }

        [Fact]
        public void Should_read_nothing_beneath_a_folder_whose_name_only_starts_like_the_root_dir()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/.locale.json"] = """{ "rootDir": "Strings" }""" } };
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Contoso.Widgets/StringsExtra/Bars/.locales/he.fake", CreateToolbarSection("elsewhere"));

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Empty(entries);
        }
    }

    public sealed class Satellites
    {
        [Fact]
        public void Should_read_a_satellite_s_file_the_SDK_named_under_its_name_with_the_culture()
        {
            // Arrange — Bars/Toolbar.he.fake, which the SDK names without its culture in the satellite.
            var assembly = new FakeAssembly(Assembly);
            assembly.Satellites["he"] = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets.Bars.Toolbar.fake"] = string.Empty } };
            var format = new FakeLocaleFileFormat { Files = { ["Contoso.Widgets.Bars.Toolbar.he.fake"] = new(CreateEntries(("Bold", "satellite file"))) } };

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets.Bars.Toolbar.he.fake", string.Empty, "Bold", "satellite file")], entries);
        }

        [Fact]
        public void Should_read_a_satellite_s_file_its_item_named_under_that_name()
        {
            // Arrange — a name an item gave, which the satellite keeps, culture and all.
            var assembly = new FakeAssembly(Assembly);
            assembly.Satellites["he"] = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/Bars/Toolbar.he.fake"] = string.Empty } };
            var format = new FakeLocaleFileFormat { Files = { ["Contoso.Widgets/Bars/Toolbar.he.fake"] = new(CreateEntries(("Bold", "named file"))) } };

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/Bars/Toolbar.he.fake", string.Empty, "Bold", "named file")], entries);
        }

        [Fact]
        public void Should_leave_a_satellite_s_file_named_for_another_culture_unread()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
            assembly.Satellites["he"] = new FakeAssembly(Assembly) { Resources = { ["Contoso.Widgets/Bars/Toolbar.es.fake"] = string.Empty } };
            var format = new FakeLocaleFileFormat { Files = { ["Contoso.Widgets/Bars/Toolbar.es.fake"] = new(CreateEntries(("Bold", "another culture"))) } };

            // Act
            var entries = Read(assembly, format);

            // Assert
            Assert.Empty(entries);
        }

        [Fact]
        public void Should_read_the_assemblys_own_file_once_when_a_satellite_holds_one_of_the_same_name()
        {
            // Arrange
            var assembly = new FakeAssembly(Assembly);
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
            // Arrange — a name a project's own item spells from the start, and the name the build gives a file beneath the root.
            var assembly = new FakeAssembly(Assembly);
            var format = new FakeLocaleFileFormat();
            AddFile(assembly, format, "Fabrikam.Gadgets..locales.he.fake", CreateSections(("Dial", CreateEntries(("Turn", "full name")))));
            AddFile(assembly, format, "Contoso.Widgets/Fabrikam/Gadgets/.locales/he.fake", CreateSections(("Dial", CreateEntries(("Turn", "beneath the assembly")))));

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
            AddFile(assembly, format, "Contoso.Widgets/.locales/he.fake", CreateSections(("Dial", CreateEntries(("Turn", "beneath the root")))));

            // Act
            var entries = Read(assembly, format, "Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry("Contoso.Widgets/.locales/he.fake", "Dial", "Turn", "beneath the root")], entries);
        }
    }
}
