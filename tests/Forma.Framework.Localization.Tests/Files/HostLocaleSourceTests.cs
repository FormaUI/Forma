namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Tests.Localization.LocaleSections;

public sealed class HostLocaleSourceTests
{
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(TempDirectory folder, FakeLocaleFileFormat format)
        => new HostLocaleSource(folder.Root, format).Read(Assembly, Toolbar, "he");

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
        public void Should_read_a_folders_culture_file_directly_inside_the_hosts_folder()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/he.fake", CreateToolbarSection("the host's root"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the host's root")], entries);
        }

        [Fact]
        public void Should_read_a_folders_own_locales_folder_as_every_source_does()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the folder's own"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the folder's own")], entries);
        }

        [Fact]
        public void Should_let_a_folders_own_locales_folder_win_where_both_layouts_stand_for_it()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var rootFile = WriteFile(folder, format, "Contoso.Widgets/Bars/he.fake", CreateToolbarSection("the host's root"));
            var ownFile = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the folder's own"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal(
                [
                    new LocaleSourceEntry(rootFile, "Toolbar", "Bold", "the host's root"),
                    new LocaleSourceEntry(ownFile, "Toolbar", "Bold", "the folder's own"),
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

        [Fact]
        public void Should_read_a_config_file_the_system_hides()
        {
            // Arrange
            using var folder = new TempDirectory();
            var configPath = folder.Write("Contoso.Widgets/.locale.json", """{ "rootDir": "Strings" }""");
            File.SetAttributes(configPath, FileAttributes.Hidden);
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Strings/Bars/.locales/he.fake", CreateToolbarSection("beneath the root dir"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "beneath the root dir")], entries);
        }
    }

    public sealed class Configs
    {
        [Fact]
        public void Should_leave_a_config_file_at_the_hosts_root_unread()
        {
            // Arrange — a config file belongs to one assembly's folder, and one above every folder configures none.
            using var folder = new TempDirectory();
            folder.Write(".locale.json", """{ "paths": ["$RootDir/$AnchorName.$Culture$Extension"] }""");
            var format = new FakeLocaleFileFormat();
            WriteFile(folder, format, "Contoso.Widgets/Bars.he.fake", CreateToolbarSection("the form the file names"));
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("the default form"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the default form")], entries);
        }

        [Fact]
        public void Should_leave_another_assemblys_config_to_it()
        {
            // Arrange
            using var folder = new TempDirectory();
            folder.Write("Fabrikam.Gadgets/.locale.json", """{ "rootDir": "Strings" }""");
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateToolbarSection("default root"));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "default root")], entries);
        }

        [Fact]
        public void Should_refuse_a_root_dir_that_leaves_the_assemblys_folder()
        {
            // Arrange
            using var folder = new TempDirectory();
            folder.Write("Contoso.Widgets/.locale.json", """{ "rootDir": ".." }""");

            // Act
            var thrown = Assert.Throws<LocaleConfigException>(() => Read(folder, new FakeLocaleFileFormat()));

            // Assert
            Assert.Contains("'..'", thrown.Message, StringComparison.Ordinal);
        }
    }
}
