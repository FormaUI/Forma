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

    public sealed class HiddenNames
    {
        [Fact]
        public void Should_read_a_locales_folder_the_system_hides()
        {
            // Arrange
            using var folder = new TempDirectory();
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/Toolbar.he.fake", CreateEntries(("Bold", "hidden folder")));
            File.SetAttributes(Path.GetDirectoryName(fullPath)!, FileAttributes.Directory | FileAttributes.Hidden);

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, string.Empty, "Bold", "hidden folder")], entries);
        }

        [Fact]
        public void Should_read_a_settings_file_the_system_hides()
        {
            // Arrange
            using var folder = new TempDirectory();
            var settingsPath = folder.Write("Contoso.Widgets/.locali.json", """{ "rootDir": "Strings" }""");
            File.SetAttributes(settingsPath, FileAttributes.Hidden);
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Strings/Bars/Toolbar.he.fake", CreateEntries(("Bold", "beneath the root dir")));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, string.Empty, "Bold", "beneath the root dir")], entries);
        }
    }

    public sealed class Settings
    {
        [Fact]
        public void Should_apply_the_hosts_own_settings_to_every_assembly()
        {
            // Arrange
            using var folder = new TempDirectory();
            folder.Write(".locali.json", """{ "paths": ["$RootDir/$AnchorName/$LocalesDir/$Culture$Extension"] }""");
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars/.locales/he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "the host's form")))));
            WriteFile(folder, format, "Contoso.Widgets/Bars.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "a default form")))));

            // Act
            var entries = Read(folder, format);

            // Assert
            Assert.Equal([new LocaleSourceEntry(fullPath, "Toolbar", "Bold", "the host's form")], entries);
        }

        [Fact]
        public void Should_leave_another_assemblys_settings_to_it()
        {
            // Arrange
            using var folder = new TempDirectory();
            folder.Write("Fabrikam.Gadgets/.locali.json", """{ "rootDir": "Strings" }""");
            var format = new FakeLocaleFileFormat();
            var fullPath = WriteFile(folder, format, "Contoso.Widgets/Bars.he.fake", CreateSections(("Toolbar", CreateEntries(("Bold", "default root")))));

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
            folder.Write("Contoso.Widgets/.locali.json", """{ "rootDir": ".." }""");

            // Act
            var thrown = Assert.Throws<LocaliSettingsException>(() => Read(folder, new FakeLocaleFileFormat()));

            // Assert
            Assert.Contains("'..'", thrown.Message, StringComparison.Ordinal);
        }
    }
}
