namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class LocalePackSourceTests
{
    private const string Pack = "Contoso.Pack";
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(FakeAssembly pack, FakeLocaleFileFormat format, string assemblyName = Assembly, string typeName = Toolbar, string cultureName = "he")
        => Read(pack, format, _ => null, assemblyName, typeName, cultureName);

    private static IReadOnlyList<LocaleSourceEntry> Read(FakeAssembly pack, FakeLocaleFileFormat format, Func<string, string?> rootNamespaces, string assemblyName = Assembly, string typeName = Toolbar, string cultureName = "he")
        => new LocalePackSource(pack, format, rootNamespaces).Read(assemblyName, typeName, cultureName);

    // Registers the file under the name the source reports it by, which leads with the pack.
    private static string AddFile(FakeAssembly pack, FakeLocaleFileFormat format, string name, LocaleSection root)
    {
        pack.Resources[name] = string.Empty;
        var displayName = $"{Pack}/{name}";
        format.Files[displayName] = new LocaleFile(root);

        return displayName;
    }

    private static LocaleSection CreateToolbarSection(string value)
        => CreateSections(("Toolbar", CreateEntries(("Bold", value))));

    public sealed class Layouts
    {
        [Fact]
        public void Should_read_a_folders_file_and_a_types_own_file_as_a_hosts_folder_does()
        {
            // Arrange — the folder's file directly in the folder, and the type's own file in the folder's .locales.
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            var folderFile = AddFile(pack, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("folder file"));
            var typeFile = AddFile(pack, format, "Contoso.Widgets.Bars..locales.Toolbar.he.fake", CreateEntries(("Bold", "type's own file")));

            // Act
            var entries = Read(pack, format);

            // Assert
            Assert.Equal(
                [
                    new(folderFile, "Toolbar", "Bold", "folder file"),
                    new(typeFile, string.Empty, "Bold", "type's own file"),
                ],
                entries);
        }

        [Fact]
        public void Should_read_a_file_only_under_the_name_of_the_assembly_it_localizes()
        {
            // Arrange
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            AddFile(pack, format, "Contoso.Pack.Bars.he.fake", CreateToolbarSection("under the pack's name"));

            // Act
            var entries = Read(pack, format);

            // Assert
            Assert.Empty(entries);
        }

        [Fact]
        public void Should_name_a_file_with_the_pack_in_front_so_a_report_says_which_package_to_open()
        {
            // Arrange
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            AddFile(pack, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("folder file"));

            // Act
            var entries = Read(pack, format);

            // Assert
            Assert.Equal("Contoso.Pack/Contoso.Widgets.Bars.he.fake", Assert.Single(entries).FileName);
        }
    }

    public sealed class Roots
    {
        [Fact]
        public void Should_lay_an_assemblys_folder_out_by_the_root_namespace_it_declares()
        {
            // Arrange — the assembly Contoso.Gadgets declares the root namespace Contoso.Things.
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            var file = AddFile(pack, format, "Contoso.Gadgets.Knobs.he.fake", CreateSections(("Dial", CreateEntries(("Turn", "declared root")))));

            // Act
            var entries = Read(pack, format, name => name == "Contoso.Gadgets" ? "Contoso.Things" : null, "Contoso.Gadgets", "Contoso.Things.Knobs.Dial");

            // Assert
            Assert.Equal([new LocaleSourceEntry(file, "Dial", "Turn", "declared root")], entries);
        }

        [Fact]
        public void Should_not_read_a_config_file_a_pack_carries()
        {
            // Arrange
            var pack = new FakeAssembly(Pack) { Resources = { ["Contoso.Widgets..locale.json"] = """{ "rootNamespace": "Elsewhere" }""" } };
            var format = new FakeLocaleFileFormat();
            var file = AddFile(pack, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("folder file"));

            // Act
            var entries = Read(pack, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(file, "Toolbar", "Bold", "folder file")], entries);
        }
    }

    public sealed class Cultures
    {
        [Fact]
        public void Should_yield_nothing_for_a_culture_the_pack_holds_no_file_for()
        {
            // Arrange
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            AddFile(pack, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("folder file"));

            // Act
            var entries = Read(pack, format, cultureName: "es");

            // Assert
            Assert.Empty(entries);
        }
    }

    public sealed class Prefix
    {
        [Fact]
        public void Should_read_only_the_files_under_its_prefix_by_their_names_after_it()
        {
            // Arrange — a host's file under the prefix, and a resource of the assembly's own beside it.
            var pack = new FakeAssembly(Pack);
            var format = new FakeLocaleFileFormat();
            var hostFile = AddFile(pack, format, ".locales.Contoso.Widgets.Bars.he.fake", CreateToolbarSection("host's file"));
            AddFile(pack, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("the assembly's own"));

            // Act
            var entries = new LocalePackSource(pack, format, _ => null, ".locales.").Read(Assembly, Toolbar, "he");

            // Assert
            Assert.Equal([new(hostFile, "Toolbar", "Bold", "host's file")], entries);
        }
    }
}
