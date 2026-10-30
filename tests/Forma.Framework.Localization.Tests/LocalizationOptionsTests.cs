namespace Forma.Framework.Localization.Tests;

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
        public void Should_take_the_application_as_the_host_s_assembly_where_its_build_embedded_the_host_s_files()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var client = new FakeAssembly("Contoso.Client") { Attributes = { new HostLocalesAttribute() } };
            var server = new FakeAssembly("Contoso.Server");

            // Act
            var clientOptions = new LocalizationOptions().WithRecordedLocales(client, loggerFactory.CreateLogger("packs"));
            var serverOptions = new LocalizationOptions().WithRecordedLocales(server, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Multiple(
                () => Assert.Same(client, clientOptions.HostLocaleAssembly),
                () => Assert.Null(serverOptions.HostLocaleAssembly));
        }

        [Fact]
        public void Should_return_the_same_options_when_the_build_recorded_none()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var application = new FakeAssembly("Contoso.App");
            var options = new LocalizationOptions { LocalePacks = [new FakeAssembly("Contoso.Pack")] };

            // Act
            var configured = options.WithRecordedLocales(application, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Same(options, configured);
        }
    }
}
