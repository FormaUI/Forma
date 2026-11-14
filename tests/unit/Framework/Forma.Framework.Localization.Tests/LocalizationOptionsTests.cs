namespace Forma.Framework.Localization.Tests;

using Microsoft.Extensions.Logging;

public sealed class LocalizationOptionsTests
{
    public sealed class RecordedLocales
    {
        [Fact]
        public void Should_lay_the_recorded_packs_before_the_named_ones_each_once()
        {
            // Arrange — the recorded pack is also among the named ones, after a pack of their own.
            using var loggerFactory = new FakeLoggerFactory();
            var recorded = typeof(LocalizationOptions).Assembly;
            var named = new FakeAssembly("Contoso.Pack");
            var application = new FakeAssembly("Contoso.App") { Attributes = { new LocalePackAttribute(recorded.GetName().Name!) } };
            var options = new LocalizationOptions { LocalePacks = [named, recorded] };

            // Act
            var configured = options.WithRecordedLocales(application, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Equal([recorded, named], configured.LocalePacks);
        }

        [Fact]
        public void Should_keep_every_other_setting()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var application = new FakeAssembly("Contoso.App") { Attributes = { new LocalePackAttribute(typeof(LocalizationOptions).Assembly.GetName().Name!) } };
            var options = new LocalizationOptions { SourceLanguage = "fr", RequireMatchingLocalization = true, LocalesPath = "strings" };

            // Act
            var configured = options.WithRecordedLocales(application, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("fr", configured.SourceLanguage),
                () => Assert.True(configured.RequireMatchingLocalization),
                () => Assert.Equal("strings", configured.LocalesPath));
        }

        [Fact]
        public void Should_take_the_application_as_an_assembly_whose_files_it_reads()
        {
            // Arrange — an application embedding files of its own alone, which it reads for any assembly they name
            using var loggerFactory = new FakeLoggerFactory();
            var application = new FakeAssembly("Contoso.App") { Resources = { ["Contoso.App.Strings.he.json"] = "{}" } };

            // Act
            var options = new LocalizationOptions().WithRecordedLocales(application, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Equal([application], options.HostLocaleAssemblies);
        }

        [Fact]
        public void Should_place_each_assembly_the_application_names_before_its_own_and_both_before_the_ones_named()
        {
            // Arrange — a server naming the localization library as its client, with options naming another
            using var loggerFactory = new FakeLoggerFactory();
            var client = typeof(LocalizationOptions).Assembly;
            var server = new FakeAssembly("Contoso.Server") { Attributes = { new LocaleReferenceAttribute(client.GetName().Name!) } };
            var named = new FakeAssembly("Contoso.Named");

            // Act
            var configured = new LocalizationOptions { HostLocaleAssemblies = [named, server] }.WithRecordedLocales(server, loggerFactory.CreateLogger("hosts"));

            // Assert
            Assert.Equal([client, server, named], configured.HostLocaleAssemblies);
        }

        [Fact]
        public void Should_report_a_named_assembly_that_cannot_be_loaded_and_leave_it_out()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var server = new FakeAssembly("Contoso.Server") { Attributes = { new LocaleReferenceAttribute("Contoso.Missing") } };

            // Act
            var configured = new LocalizationOptions().WithRecordedLocales(server, loggerFactory.CreateLogger("hosts"));

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal([server], configured.HostLocaleAssemblies),
                () => Assert.Equal(LogLevel.Warning, entry.Level),
                () => Assert.Contains("Contoso.Missing", entry.Message, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_add_the_application_alone_when_its_build_recorded_nothing()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var application = new FakeAssembly("Contoso.App");
            var options = new LocalizationOptions { LocalePacks = [new FakeAssembly("Contoso.Pack")] };

            // Act
            var configured = options.WithRecordedLocales(application, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Multiple(
                () => Assert.Same(options.LocalePacks, configured.LocalePacks),
                () => Assert.Equal([application], configured.HostLocaleAssemblies));
        }
    }
}
