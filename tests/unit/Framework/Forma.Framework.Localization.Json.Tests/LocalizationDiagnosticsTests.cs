namespace Forma.Framework.Localization.Json.Tests;

using System.Reflection;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Tests.Localization;

using Microsoft.Extensions.Logging;

public sealed class LocalizationDiagnosticsTests
{
    private const string TestAssembly = "Forma.Framework.Localization.Json.Tests";
    private const string ColocatedType = "Forma.Framework.Localization.Json.Tests.Fixtures.Colocated";
    private const int AssemblyNotLocalized = 5;
    private const int TypeNotLocalized = 6;
    private const int KeyNotLocalized = 7;

    // A factory reading the assembly's own files and the packs given, with the host's folder left unread unless one is named.
    private static JsonStringLocaleFactory CreateFactory(FakeLoggerFactory loggerFactory, LocalizationDiagnostics? diagnostics, string localesPath = "", params Assembly[] packs)
        => new(new LocalizationOptions { Diagnostics = diagnostics, LocalesPath = localesPath, LocalePacks = packs }, loggerFactory);

    // A pack laid out for this assembly: a file for the Fixtures folder.
    private static FakeAssembly CreatePack(string fixturesFile)
        => new("Contoso.Pack") { Resources = { ["Forma.Framework.Localization.Json.Tests.Fixtures.he.json"] = fixturesFile } };

    private static FakeLogEntry[] GetReports(FakeLoggerFactory loggerFactory)
        => [.. loggerFactory.Entries.Where(entry => entry.EventId.Id is AssemblyNotLocalized or TypeNotLocalized or KeyNotLocalized)];

    public sealed class Levels
    {
        [Fact]
        public void Should_report_an_assembly_with_no_entries_in_the_culture_once_in_place_of_its_types_and_keys()
        {
            // Arrange — no source holds a German entry for any of the assembly's types
            using var culture = new CultureScope("de");
            using var loggerFactory = new FakeLoggerFactory();
            var factory = CreateFactory(loggerFactory, new());

            // Act
            _ = factory.Create(typeof(Colocated))["Today"];
            _ = factory.Create(typeof(Colocated))["Last 7 days"];
            _ = factory.Create(typeof(Unlocalized))["Open"];

            // Assert
            var report = Assert.Single(GetReports(loggerFactory));
            Assert.Multiple(
                () => Assert.Equal(new EventId(AssemblyNotLocalized, "AssemblyNotLocalized"), report.EventId),
                () => Assert.Equal(LogLevel.Warning, report.Level),
                () => Assert.Equal(TestAssembly, report.Fields["Assembly"]),
                () => Assert.Equal(ColocatedType, report.Fields["Type"]),
                () => Assert.Equal("Today", report.Fields["Key"]),
                () => Assert.Equal("de", report.Fields["Culture"]));
        }

        [Fact]
        public void Should_report_a_type_with_no_entries_in_the_culture_once_in_place_of_its_keys()
        {
            // Arrange — the assembly holds French entries for another type, and none for this one
            using var culture = new CultureScope("fr");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));

            // Act
            _ = strings["Today"];
            _ = strings["Last 7 days"];

            // Assert
            var report = Assert.Single(GetReports(loggerFactory));
            Assert.Multiple(
                () => Assert.Equal(new EventId(TypeNotLocalized, "TypeNotLocalized"), report.EventId),
                () => Assert.Equal(LogLevel.Warning, report.Level),
                () => Assert.Equal(TestAssembly, report.Fields["Assembly"]),
                () => Assert.Equal(ColocatedType, report.Fields["Type"]),
                () => Assert.Equal("Today", report.Fields["Key"]),
                () => Assert.Equal("fr", report.Fields["Culture"]));
        }

        [Fact]
        public void Should_report_each_key_with_no_entry_once()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));

            // Act
            _ = strings["Absent from every file"];
            _ = strings["Absent from every file"];
            _ = strings.Resolve("Absent from every file too");
            _ = strings["Today"];

            // Assert
            var reports = GetReports(loggerFactory);
            Assert.Multiple(
                () => Assert.Equal(["Absent from every file", "Absent from every file too"], reports.Select(report => report.Fields["Key"])),
                () => Assert.All(reports, report => Assert.Equal(new EventId(KeyNotLocalized, "KeyNotLocalized"), report.EventId)),
                () => Assert.All(reports, report => Assert.Equal(LogLevel.Warning, report.Level)),
                () => Assert.All(reports, report => Assert.Equal(TestAssembly, report.Fields["Assembly"])),
                () => Assert.All(reports, report => Assert.Equal(ColocatedType, report.Fields["Type"])),
                () => Assert.All(reports, report => Assert.Equal("he", report.Fields["Culture"])));
        }

        [Fact]
        public void Should_report_a_key_where_a_culture_it_falls_back_to_holds_the_type()
        {
            // Arrange — Latin American Spanish has no file of its own, and Spanish holds the type's entries
            using var culture = new CultureScope("es-419");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));

            // Act
            _ = strings["Absent from every file"];

            // Assert
            var report = Assert.Single(GetReports(loggerFactory));
            Assert.Multiple(
                () => Assert.Equal(KeyNotLocalized, report.EventId.Id),
                () => Assert.Equal("es-419", report.Fields["Culture"]));
        }

        [Fact]
        public void Should_report_a_key_where_shared_entries_alone_hold_the_type()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var pack = CreatePack("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": { "Open": "shared" } }""");
            var strings = CreateFactory(loggerFactory, new(), packs: pack).Create(typeof(Unlocalized));

            // Act
            _ = strings["Close"];

            // Assert
            Assert.Equal(KeyNotLocalized, Assert.Single(GetReports(loggerFactory)).EventId.Id);
        }

        [Fact]
        public void Should_report_a_key_once_whichever_locale_of_the_factory_looks_it_up()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));
            var hebrew = CultureInfo.GetCultureInfo("he");

            // Act
            _ = strings["Absent from every file"];
            _ = strings.WithCulture(hebrew, hebrew)["Absent from every file"];

            // Assert
            Assert.Single(GetReports(loggerFactory));
        }
    }

    public sealed class Resolution
    {
        [Fact]
        public void Should_report_nothing_for_a_key_a_culture_it_falls_back_to_answers()
        {
            // Arrange
            using var culture = new CultureScope("es-419");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));

            // Act
            var localized = strings["Today"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Hoy", localized.Value),
                () => Assert.Empty(GetReports(loggerFactory)));
        }

        [Fact]
        public void Should_report_nothing_for_a_key_a_pack_answers()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var pack = CreatePack("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "pack" } }""");
            var strings = CreateFactory(loggerFactory, new(), packs: pack).Create(typeof(Unlocalized));

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("pack", localized.Value),
                () => Assert.Empty(GetReports(loggerFactory)));
        }

        [Fact]
        public void Should_report_nothing_for_a_key_the_host_files_answer()
        {
            // Arrange — the folder beside the application alone localizes the type
            using var culture = new CultureScope("fr");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new(), localesPath: ".locales").Create(typeof(HostLocalized));

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Enregistrer", localized.Value),
                () => Assert.Empty(GetReports(loggerFactory)));
        }

        [Fact]
        public void Should_report_nothing_for_a_key_a_shared_entry_answers()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var pack = CreatePack("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": { "Open": "shared" } }""");
            var strings = CreateFactory(loggerFactory, new(), packs: pack).Create(typeof(Unlocalized));

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("shared", localized.Value),
                () => Assert.Empty(GetReports(loggerFactory)));
        }

        [Fact]
        public void Should_report_nothing_for_a_key_a_null_entry_answers()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(NullValued));

            // Act
            var localized = strings["Save"];

            // Assert
            Assert.Multiple(
                () => Assert.False(localized.ResourceNotFound),
                () => Assert.Empty(GetReports(loggerFactory)));
        }

        [Fact]
        public void Should_report_nothing_for_a_key_whose_text_is_the_key_itself()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var pack = CreatePack("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "OK": "OK" } }""");
            var strings = CreateFactory(loggerFactory, new(), packs: pack).Create(typeof(Unlocalized));

            // Act
            var localized = strings["OK"];

            // Assert
            Assert.Multiple(
                () => Assert.False(localized.ResourceNotFound),
                () => Assert.Empty(GetReports(loggerFactory)));
        }
    }

    public sealed class Cultures
    {
        [Fact]
        public void Should_report_nothing_without_diagnostics()
        {
            // Arrange
            using var culture = new CultureScope("de");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, diagnostics: null).Create(typeof(Colocated));

            // Act
            _ = strings["Absent from every file"];

            // Assert
            Assert.Empty(loggerFactory.Entries);
        }

        [Fact]
        public void Should_check_the_culture_of_the_lookup_alone_by_default()
        {
            // Arrange — Hebrew localizes the key, Spanish does not
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated));

            // Act
            _ = strings["Stored {stored} of {total}.", 3, 9];

            // Assert
            Assert.Empty(GetReports(loggerFactory));
        }

        [Fact]
        public void Should_check_every_culture_named_beside_the_culture_of_the_lookup()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new() { Cultures = ["es", "he"] }).Create(typeof(Colocated));

            // Act
            var localized = strings["Stored {stored} of {total}.", 3, 9];

            // Assert
            var report = Assert.Single(GetReports(loggerFactory));
            Assert.Multiple(
                () => Assert.Equal("מתוך 9 נשמרו 3.", localized.Value),
                () => Assert.Equal(KeyNotLocalized, report.EventId.Id),
                () => Assert.Equal("es", report.Fields["Culture"]));
        }

        [Fact]
        public void Should_check_a_bound_locale_in_the_culture_it_is_bound_to()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var spanish = CultureInfo.GetCultureInfo("es");
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Colocated)).WithCulture(spanish, spanish);

            // Act
            _ = strings["Stored {stored} of {total}.", 3, 9];

            // Assert
            Assert.Equal("es", Assert.Single(GetReports(loggerFactory)).Fields["Culture"]);
        }

        [Fact]
        public void Should_leave_the_source_language_unchecked()
        {
            // Arrange — the keys are English, so an English culture shows them as its text
            using var culture = new CultureScope("en-GB");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new() { Cultures = ["en", "en-US"] }).Create(typeof(Colocated));

            // Act
            _ = strings["Absent from every file"];

            // Assert
            Assert.Empty(GetReports(loggerFactory));
        }

        [Fact]
        public void Should_report_a_culture_whose_files_cannot_be_read_and_keep_the_text_of_the_lookup()
        {
            // Arrange — the type's Hebrew file carries metadata that doesn't parse
            using var culture = new CultureScope("es");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new() { Cultures = ["he"] }).Create(typeof(MalformedMetadata));

            // Act
            var first = strings["Save"];
            var second = strings["Open"];

            // Assert
            var report = Assert.Single(loggerFactory.Entries, entry => entry.EventId.Id == 8);
            Assert.Multiple(
                () => Assert.Equal("Save", first.Value),
                () => Assert.Equal("Open", second.Value),
                () => Assert.Equal(LogLevel.Warning, report.Level),
                () => Assert.Equal("Forma.Framework.Localization.Json.Tests.Fixtures.MalformedMetadata", report.Fields["Type"]),
                () => Assert.Equal("he", report.Fields["Culture"]));
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("x-test")]
        public void Should_reject_a_name_that_does_not_name_a_language(string name)
        {
            // Arrange
            var options = new LocalizationOptions { Diagnostics = new() { Cultures = [name] } };

            // Act, Assert
            Assert.ThrowsAny<ArgumentException>(() => new JsonStringLocaleFactory(options));
        }
    }

    public sealed class Plurals
    {
        [Fact]
        public void Should_report_an_argument_that_is_not_a_count_as_a_warning()
        {
            // Arrange
            using var culture = new CultureScope("he");
            using var loggerFactory = new FakeLoggerFactory();
            var strings = CreateFactory(loggerFactory, new()).Create(typeof(Pluralized));

            // Act
            _ = strings["[$ plural; one: # file; other: # files; $]", "three"];

            // Assert
            var report = Assert.Single(loggerFactory.Entries, entry => entry.EventId.Id == 1);
            Assert.Equal(LogLevel.Warning, report.Level);
        }
    }
}
