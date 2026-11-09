namespace Forma.Framework.Localization.Tests;

using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

public sealed class LocaleAssemblyLoaderTests
{
    public sealed class Loading
    {
        [Fact]
        public void Should_read_the_application_s_locales_from_the_entry_assembly()
        {
            // Arrange
            var entry = new FakeAssembly("Contoso.App");
            var library = new FakeAssembly("Contoso.Library") { Attributes = { new LocalePackAttribute("Contoso.Hebrew") } };

            // Act
            var applications = LocaleAssemblyLoader.GetApplicationAssemblies(entry, [entry, library]);

            // Assert
            Assert.Equal([entry], applications);
        }

        [Fact]
        public void Should_read_them_from_the_assembly_its_build_marked_as_the_application_where_the_runtime_names_no_entry()
        {
            // Arrange — on a runtime naming no entry assembly, as .NET for Android's: the application its build marked, a library recording
            // a pack for its own use, a client embedding a host's files and naming none, the framework, and a dynamic assembly.
            var application = new FakeAssembly("Contoso.App") { Attributes = { new LocaleApplicationAttribute(), new LocalePackAttribute("Contoso.Hebrew") } };
            var library = new FakeAssembly("Contoso.Library") { Attributes = { new LocalePackAttribute("Contoso.Spanish") } };
            var client = new FakeAssembly("Contoso.Client") { Resources = { [LocalePackSource.HostResourcePrefix + "Contoso.Library.he.json"] = "{}" } };
            var framework = new FakeAssembly("System.Runtime") { Resources = { ["System.Runtime.Strings.resources"] = string.Empty } };
            var dynamic = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Contoso.Dynamic"), AssemblyBuilderAccess.Run);

            // Act
            var applications = LocaleAssemblyLoader.GetApplicationAssemblies(null, [library, framework, client, dynamic, application]);

            // Assert
            Assert.Equal([application], applications);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Should_keep_the_application_s_packs_in_its_order_whichever_assembly_loads_first(bool isLibraryFirst)
        {
            // Arrange — the application records a pack and the host names a second, which a library records for its own use; a later pack
            // overrides an earlier one, so the order decides which of two conflicting translations is read.
            using var loggerFactory = new FakeLoggerFactory();
            var recorded = typeof(LocalizationOptions).Assembly;
            var named = typeof(LocaleAssemblyLoaderTests).Assembly;
            var application = new FakeAssembly("Contoso.App") { Attributes = { new LocaleApplicationAttribute(), new LocalePackAttribute(recorded.GetName().Name!) } };
            var library = new FakeAssembly("Contoso.Library") { Attributes = { new LocalePackAttribute(named.GetName().Name!) } };
            var options = new LocalizationOptions { LocalePacks = [named] };
            Assembly[] loaded = isLibraryFirst ? [library, application] : [application, library];

            // Act
            var configured = LocaleAssemblyLoader.GetApplicationAssemblies(null, loaded)
                .Aggregate(options, (recordedLocales, assembly) => recordedLocales.WithRecordedLocales(assembly, loggerFactory.CreateLogger("packs")));

            // Assert
            Assert.Equal([recorded, named], configured.LocalePacks);
        }

        [Fact]
        public void Should_name_each_pack_the_assembly_was_built_against_once_in_order()
        {
            // Arrange
            var assembly = new FakeAssembly("Contoso.App")
            {
                Attributes = { new LocalePackAttribute("Contoso.Hebrew"), new LocalePackAttribute("Contoso.Spanish"), new LocalePackAttribute("Contoso.Hebrew"), new LocalePackAttribute(" ") },
            };

            // Act
            var names = LocaleAssemblyLoader.GetPackNames(assembly);

            // Assert
            Assert.Equal(["Contoso.Hebrew", "Contoso.Spanish"], names);
        }

        [Fact]
        public void Should_load_a_pack_by_the_name_recorded()
        {
            // Arrange — the localization library stands in for a pack the runtime can load by name.
            using var loggerFactory = new FakeLoggerFactory();
            var assembly = new FakeAssembly("Contoso.App") { Attributes = { new LocalePackAttribute("Forma.Framework.Localization") } };

            // Act
            var packs = LocaleAssemblyLoader.LoadPacks(assembly, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Equal("Forma.Framework.Localization", Assert.Single(packs).GetName().Name);
        }

        [Fact]
        public void Should_report_a_pack_that_cannot_be_loaded_and_leave_it_out()
        {
            // Arrange — a recorded pack whose assembly is missing, beside one that loads.
            using var loggerFactory = new FakeLoggerFactory();
            var assembly = new FakeAssembly("Contoso.App") { Attributes = { new LocalePackAttribute("Contoso.Missing"), new LocalePackAttribute("Forma.Framework.Localization") } };

            // Act
            var packs = LocaleAssemblyLoader.LoadPacks(assembly, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Forma.Framework.Localization", Assert.Single(packs).GetName().Name),
                () => Assert.Equal(LogLevel.Warning, Assert.Single(loggerFactory.Entries).Level),
                () => Assert.Contains("Contoso.Missing", Assert.Single(loggerFactory.Entries).Message, StringComparison.Ordinal));
        }
    }
}
