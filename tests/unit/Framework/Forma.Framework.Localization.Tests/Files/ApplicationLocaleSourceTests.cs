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
        var displayName = $"{application.GetName().Name}/{name}";
        format.Files[displayName] = new LocaleFile(root);

        return displayName;
    }

    private static LocaleSection CreateToolbarSection(string value)
        => CreateSections(("Toolbar", CreateEntries(("Bold", value))));

    public sealed class Names
    {
        [Fact]
        public void Should_read_the_files_beneath_the_application_s_root()
        {
            // Arrange — the file .locales/Contoso.Widgets/Bars.he.fake in the application's project.
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App/.locales/Contoso.Widgets/Bars.he.fake", CreateToolbarSection("application's file"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "application's file")], entries);
        }

        // A resource beside the application's root, one in a folder whose name only starts like the root, and one spelled
        // with a dot after the root, as the SDK names an item's file.
        [Theory]
        [InlineData("Contoso.Widgets/Bars.he.fake")]
        [InlineData("Contoso.AppExtras/.locales/Contoso.Widgets/Bars.he.fake")]
        [InlineData("Contoso.App.Extras/.locales/Contoso.Widgets/Bars.he.fake")]
        [InlineData("Contoso.App..locales.Contoso.Widgets.Bars.he.fake")]
        public void Should_read_nothing_outside_the_application_s_root(string name)
        {
            // Arrange
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            AddFile(application, format, name, CreateToolbarSection("outside the root"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Empty(entries);
        }

        [Fact]
        public void Should_read_a_culture_folder_in_the_application_s_root_locales()
        {
            // Arrange — the file .locales/he/Contoso.Widgets/Bars.fake in the application's project
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App/.locales/he/Contoso.Widgets/Bars.fake", CreateToolbarSection("culture folder"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "culture folder")], entries);
        }

        [Fact]
        public void Should_leave_out_the_folder_of_locale_files_the_application_s_config_names()
        {
            // Arrange — the file .strings/he/Contoso.Widgets/Bars.fake in a project whose config names .strings
            var application = new FakeAssembly(Application) { Resources = { ["Contoso.App/.locale.json"] = """{ "localesDir": ".strings" }""" } };
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App/.strings/he/Contoso.Widgets/Bars.fake", CreateToolbarSection("own folder"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "own folder")], entries);
        }

        [Fact]
        public void Should_begin_the_names_where_the_application_s_root_dir_leads()
        {
            // Arrange — a project whose config names Strings, and a file above it.
            var application = new FakeAssembly(Application) { Resources = { ["Contoso.App/.locale.json"] = """{ "rootDir": "Strings" }""" } };
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App/Strings/.locales/he/Contoso.Widgets/Bars.fake", CreateToolbarSection("beneath the root"));
            AddFile(application, format, "Contoso.App/.locales/he/Contoso.Widgets/Bars.fake", CreateToolbarSection("above the root"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "beneath the root")], entries);
        }

        [Fact]
        public void Should_begin_the_names_where_the_config_of_an_application_whose_root_namespace_differs_sits()
        {
            // Arrange — the application Contoso.App.Client, whose project's root namespace is Contoso.App.
            var application = new FakeAssembly("Contoso.App.Client") { Resources = { ["Contoso.App/.locale.json"] = "{}" } };
            var format = new FakeLocaleFileFormat();
            var applicationFile = AddFile(application, format, "Contoso.App/.locales/he/Contoso.Widgets/Bars.fake", CreateToolbarSection("project's root"));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Equal([new(applicationFile, "Toolbar", "Bold", "project's root")], entries);
        }

        [Fact]
        public void Should_leave_the_application_s_own_strings_to_its_own_assembly()
        {
            // Arrange — a file for the application's own types, which its own assembly's reading answers.
            var application = new FakeAssembly(Application);
            var format = new FakeLocaleFileFormat();
            AddFile(application, format, "Contoso.App/Pages/.locales/he.fake", CreateSections(("Home", CreateEntries(("Bold", "own")))));

            // Act
            var entries = Read(application, format);

            // Assert
            Assert.Empty(entries);
        }
    }
}
