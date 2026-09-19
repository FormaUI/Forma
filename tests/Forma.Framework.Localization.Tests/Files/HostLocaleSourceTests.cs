namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class HostLocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(TempDirectory folder, FakeLocaleFileFormat format)
        => Read(folder, format, _ => null);

    private static IReadOnlyList<LocaleSourceEntry> Read(TempDirectory folder, FakeLocaleFileFormat format, Func<string, string?> rootNamespaces)
        => new HostLocaleSource(folder.Root, format, rootNamespaces).Read(Assembly, Toolbar, "he");

    // The format hands a file back by the full path the source names it by.
    private static string WriteFile(TempDirectory folder, FakeLocaleFileFormat format, string name, LocaleSection root)
    {
        var fullPath = Path.GetFullPath(folder.Write(name, string.Empty));
        format.Files[fullPath] = new LocaleFile(root);

        return fullPath;
    }

    private static LocaleSection CreateToolbarSection(string value)
        => CreateSections(("Toolbar", CreateEntries(("Bold", value))));

    public sealed class Layouts
    {
        [Fact]
        public void Should_read_a_folders_file_from_the_folder_itself()
        {
            // Arrange — the host's folder is itself a folder of locale files, so a folder's file may sit directly in it.
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/he.fake", CreateToolbarSection("the file in the folder itself"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the file in the folder itself")], entries);
        }

        [Fact]
        public void Should_read_a_folders_file_from_the_folders_own_locales_folder()
        {
            // Arrange — the path every source reads is the host's too.
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the file in the folder's own .locales"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the file in the folder's own .locales")], entries);
        }

        [Fact]
        public void Should_read_a_file_in_both_places_as_a_duplicate()
        {
            // Arrange — the two forms stand for one anchor, so both files are read as its entries and neither is an override of the other.
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var folderFile = WriteFile(folder, format, "Contoso.Widgets/Bars/he.fake", CreateToolbarSection("the file in the folder itself"));
            var localesFile = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the file in the folder's own .locales"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal(
                [
                    new LocaleSourceEntry(localesFile, "Toolbar", "Bold", "the file in the folder's own .locales"),
                    new LocaleSourceEntry(folderFile, "Toolbar", "Bold", "the file in the folder itself"),
                ],
                entries);
        }
    }

    public sealed class HiddenNames
    {
        [Fact]
        public void Should_read_a_locales_folder_the_system_hides()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("hidden folder"));
            File.SetAttributes(Path.GetDirectoryName(fullPath)!, FileAttributes.Directory | FileAttributes.Hidden);

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "hidden folder")], entries);
        }

    }

    public sealed class RootNamespaces
    {
        [Fact]
        public void Should_count_anchors_from_the_root_namespace_the_assembly_declares()
        {
            // Arrange — the assembly's own config names Contoso.Widgets.Bars, so its folder is laid out from there and the type's own folder sits right beneath it.
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Toolbar/.locales/he.fake", CreateEntries(("Bold", "declared root")));

            // Act
            var entries = Read(folder, format, assemblyName => assemblyName == Assembly ? "Contoso.Widgets.Bars" : null);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, string.Empty, "Bold", "declared root")], entries);
        }

        [Fact]
        public void Should_leave_another_assemblys_root_namespace_to_it()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the assembly's name"));

            // Act
            var entries = Read(folder, format, assemblyName => assemblyName == "Fabrikam.Gadgets" ? "Fabrikam" : null);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the assembly's name")], entries);
        }

        [Fact]
        public void Should_leave_a_config_file_in_the_hosts_folder_unread()
        {
            // Arrange — a config file is the assembly's own, embedded at its root; one a host lays in its folder, above or inside an assembly's, configures nothing.
            using var folder = new TempDirectory();
            folder.Write(".locale.json", """{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }""");
            folder.Write("Contoso.Widgets/.locale.json", """{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }""");
            var format = new FakeLocaleFileFormat();
            WriteFile(folder, format, "Contoso.Widgets/Bars.he.fake", CreateToolbarSection("the form the files name"));
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the default form"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the default form")], entries);
        }
    }
}
