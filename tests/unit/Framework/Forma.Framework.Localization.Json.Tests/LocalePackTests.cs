namespace Forma.Framework.Localization.Json.Tests;

using System.Reflection;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Tests.Localization;

public sealed class LocalePackTests
{
    private static IStringLocale CreateLocale<T>(params Assembly[] packs)
        => new JsonStringLocaleFactory(new LocalizationOptions { LocalePacks = packs }).Create(typeof(T));

    // A pack laid out for this assembly: a file for the Fixtures folder, with a section per type.
    private static FakeAssembly CreatePack(string name, string fixturesFile)
        => new(name) { Resources = { ["Forma.Framework.Localization.Json.Tests.Fixtures.he.json"] = fixturesFile } };

    public sealed class Layering
    {
        [Fact]
        public void Should_localize_a_type_the_assembly_ships_no_file_for()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var pack = CreatePack("Contoso.Pack", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "pack" } }""");
            var strings = CreateLocale<Unlocalized>(pack);

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("pack", localized.Value);
        }

        [Fact]
        public void Should_override_what_the_assembly_ships()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var pack = CreatePack("Contoso.Pack", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Colocated": { "Today": "pack" } }""");
            var strings = CreateLocale<Colocated>(pack);

            // Act
            var localized = strings["Today"];

            // Assert
            Assert.Equal("pack", localized.Value);
        }

        [Fact]
        public void Should_keep_what_the_assembly_ships_where_the_pack_says_nothing()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var pack = CreatePack("Contoso.Pack", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Colocated": { "Today": "pack" } }""");
            var strings = CreateLocale<Colocated>(pack);

            // Act
            var localized = strings["Last 7 days"];

            // Assert
            Assert.Equal("7 הימים האחרונים", localized.Value);
        }

        [Fact]
        public void Should_yield_to_the_hosts_file()
        {
            // Arrange — the host's root file overrides Save and leaves Open to the pack.
            using var culture = new CultureScope("he");
            var pack = CreatePack("Contoso.Pack", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "HostOverridden": { "Open": "pack", "Save": "pack" } }""");
            var strings = CreateLocale<HostOverridden>(pack);

            // Act
            var save = strings["Save"];
            var open = strings["Open"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("root file", save.Value),
                () => Assert.Equal("pack", open.Value));
        }

        [Fact]
        public void Should_read_packs_in_the_order_given_so_a_later_one_overrides_an_earlier()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var first = CreatePack("Contoso.First", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "first" } }""");
            var second = CreatePack("Contoso.Second", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "second" } }""");

            // Act
            var firstThenSecond = CreateLocale<Unlocalized>(first, second)["Open"];
            var secondThenFirst = CreateLocale<Unlocalized>(second, first)["Open"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("second", firstThenSecond.Value),
                () => Assert.Equal("first", secondThenFirst.Value));
        }

        [Fact]
        public void Should_not_read_a_pack_laid_out_for_another_assembly()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var pack = new FakeAssembly("Contoso.Pack") { Resources = { ["Contoso.Widgets.Fixtures.he.json"] = """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "pack" } }""" } };
            var strings = CreateLocale<Unlocalized>(pack);

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("Open", localized.Value);
        }

        [Fact]
        public void Should_read_no_pack_when_the_options_name_none()
        {
            // Arrange
            using var culture = new CultureScope("he");
            var strings = CreateLocale<Unlocalized>();

            // Act
            var localized = strings["Open"];

            // Assert
            Assert.Equal("Open", localized.Value);
        }
    }

    public sealed class HostAssembly
    {
        private const string Fixtures = "Forma.Framework.Localization.Json.Tests.Fixtures.he.json";

        private static IStringLocale CreateLocale<T>(Assembly host, string localesPath, params Assembly[] packs)
            => new JsonStringLocaleFactory(new LocalizationOptions { LocalePacks = packs, HostLocaleAssemblies = [host], LocalesPath = localesPath }).Create(typeof(T));

        [Fact]
        public void Should_override_the_packs_with_the_hosts_files_embedded_under_the_prefix_alone()
        {
            // Arrange — the host's assembly carries a file under the prefix and one of its own beside it.
            using var culture = new CultureScope("he");
            var pack = CreatePack("Contoso.Pack", """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "pack", "Save": "pack" } }""");
            var host = new FakeAssembly("Contoso.Client")
            {
                Resources =
                {
                    [LocalePackSource.HostResourcePrefix + Fixtures] = """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Open": "host" } }""",
                    [Fixtures] = """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Unlocalized": { "Save": "not the host's" } }""",
                },
            };
            var strings = CreateLocale<Unlocalized>(host, string.Empty, pack);

            // Act
            var open = strings["Open"];
            var save = strings["Save"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("host", open.Value),
                () => Assert.Equal("pack", save.Value));
        }

        [Fact]
        public void Should_yield_to_the_hosts_folder()
        {
            // Arrange — the host's folder's root file overrides Save, and the embedded file Open.
            using var culture = new CultureScope("he");
            var host = new FakeAssembly("Contoso.Client")
            {
                Resources =
                {
                    [LocalePackSource.HostResourcePrefix + Fixtures] = """{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "HostOverridden": { "Open": "embedded", "Save": "embedded" } }""",
                },
            };
            var strings = CreateLocale<HostOverridden>(host, new LocalizationOptions().LocalesPath);

            // Act
            var save = strings["Save"];
            var open = strings["Open"];

            // Assert
            Assert.Multiple(
                () => Assert.Equal("root file", save.Value),
                () => Assert.Equal("embedded", open.Value));
        }
    }

    // An application found where the runtime names no entry assembly, its keys written in Spanish and read in French, with a distinct French text in
    // every source, so a source discovery passes over shows as another source's text or as the key itself.
    public sealed class Discovery
    {
        // The localization library's own types ship no locale file, so a file for its root folder is the only one that localizes them.
        private const string RootFile = "Forma.Framework.Localization.fr.json";

        // The test assembly carries a pack's file and a host's file for the localization library, loadable by its name.
        private static readonly string TestAssembly = typeof(Discovery).Assembly.GetName().Name!;

        private static string CreateFile(string text)
            => $$"""{ "$localization": "SchemaVersion: 1.0.0; Culture: fr;", "LocalizationOptions": { "Open": "{{text}}" } }""";

        private static IStringLocale CreateDiscoveredLocale(LocalizationOptions options, params Assembly[] loaded)
        {
            var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
            var configured = LocalePackLoader.GetApplicationAssemblies(null, loaded).Aggregate(options, (recorded, application) => recorded.WithRecordedLocales(application, logger));

            return new JsonStringLocaleFactory(configured).Create(typeof(LocalizationOptions));
        }

        [Fact]
        public void Should_read_an_application_s_own_host_files()
        {
            // Arrange — an application whose only locales are a host's files it embedded, over a pack the options name
            using var culture = new CultureScope("fr");
            var application = new FakeAssembly("Contoso.App") { Resources = { [LocalePackSource.HostResourcePrefix + RootFile] = CreateFile("Ouvrir (application)") } };
            var pack = new FakeAssembly("Contoso.Pack") { Resources = { [RootFile] = CreateFile("Ouvrir (paquet nommé)") } };
            var strings = CreateDiscoveredLocale(new LocalizationOptions { SourceLanguage = "es", LocalesPath = string.Empty, LocalePacks = [pack] }, new FakeAssembly("System.Runtime"), application);

            // Act
            var open = strings["Open"];

            // Assert
            Assert.Equal("Ouvrir (application)", open.Value);
        }

        [Fact]
        public void Should_read_the_pack_an_application_records()
        {
            // Arrange — an application whose only locale is a recorded pack
            using var culture = new CultureScope("fr");
            var application = new FakeAssembly("Contoso.App") { Attributes = { new LocalePackAttribute(TestAssembly) } };
            var strings = CreateDiscoveredLocale(new LocalizationOptions { SourceLanguage = "es", LocalesPath = string.Empty }, new FakeAssembly("System.Runtime"), application);

            // Act
            var open = strings["Open"];

            // Assert
            Assert.Equal("Ouvrir (paquet)", open.Value);
        }

        [Fact]
        public void Should_read_the_host_files_of_the_client_a_server_names()
        {
            // Arrange — a server whose only locale is the client it names, over a pack the options name
            using var culture = new CultureScope("fr");
            var server = new FakeAssembly("Contoso.Server") { Attributes = { new LocaleReferenceAttribute(TestAssembly) } };
            var pack = new FakeAssembly("Contoso.Pack") { Resources = { [RootFile] = CreateFile("Ouvrir (paquet nommé)") } };
            var strings = CreateDiscoveredLocale(new LocalizationOptions { SourceLanguage = "es", LocalesPath = string.Empty, LocalePacks = [pack] }, new FakeAssembly("System.Runtime"), server);

            // Act
            var open = strings["Open"];

            // Assert
            Assert.Equal("Ouvrir (client)", open.Value);
        }

        [Fact]
        public void Should_let_the_server_s_own_host_files_override_its_client_s()
        {
            // Arrange — a server naming its client and embedding a host's files of its own for the same key
            using var culture = new CultureScope("fr");
            var server = new FakeAssembly("Contoso.Server")
            {
                Attributes = { new LocaleReferenceAttribute(TestAssembly) },
                Resources = { [LocalePackSource.HostResourcePrefix + RootFile] = CreateFile("Ouvrir (serveur)") },
            };
            var strings = CreateDiscoveredLocale(new LocalizationOptions { SourceLanguage = "es", LocalesPath = string.Empty }, server);

            // Act
            var open = strings["Open"];

            // Assert
            Assert.Equal("Ouvrir (serveur)", open.Value);
        }
    }
}
