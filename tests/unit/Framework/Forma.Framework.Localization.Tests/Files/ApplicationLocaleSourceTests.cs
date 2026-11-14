namespace Forma.Framework.Localization.Tests.Files;

using static Forma.Framework.Localization.Tests.Support.LocaleSections;

public sealed class ApplicationLocaleSourceTests
{
    private const string Application = "Contoso.App";
    private const string Assembly = "Contoso.Widgets";
    private const string Toolbar = "Contoso.Widgets.Bars.Toolbar";

    private static IReadOnlyList<LocaleSourceEntry> Read(FakeAssembly application, FakeLocaleFileFormat format)
        => new ApplicationLocaleSource(application, format, _ => null).Read(Assembly, Toolbar, "he");

    // Registers the file under the name the source reports it by, which leads with the application.
    private static string AddFile(FakeAssembly application, FakeLocaleFileFormat format, string name, LocaleSection root)
    {
        application.Resources[name] = string.Empty;
        var displayName = $"{Application}/{name}";
        format.Files[displayName] = new LocaleFile(root);

        return displayName;
    }

    private static LocaleSection CreateToolbarSection(string value)
        => CreateSections(("Toolbar", CreateEntries(("Bold", value))));

    public sealed class Names
    {
        [Fact]
        public void Should_read_only_the_files_beneath_the_application_s_root()
        {
            // Arrange — a file in the application's root .locales for another assembly, and a resource outside the application's root.
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App..locales.Contoso.Widgets.Bars.he.fake", CreateToolbarSection("application's file"));
            AddFile(application, format, "Contoso.Widgets.Bars.he.fake", CreateToolbarSection("outside the root"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "application's file")], entries);
        }

        [Fact]
        public void Should_read_a_culture_folder_in_the_application_s_root_locales()
        {
            // Arrange — the file .locales/he/Contoso.Widgets/Bars.fake in the application's project
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App..locales.he.Contoso.Widgets.Bars.fake", CreateToolbarSection("culture folder"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "culture folder")], entries);
        }

        [Fact]
        public void Should_leave_the_application_s_own_strings_to_its_own_assembly()
        {
            // Arrange — a file for the application's own types, which its own assembly's reading answers.
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            AddFile(application, format, "Contoso.App.Pages..locales.he.fake", CreateSections(("Home", CreateEntries(("Bold", "own")))));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Empty(entries);
        }
    }
}
