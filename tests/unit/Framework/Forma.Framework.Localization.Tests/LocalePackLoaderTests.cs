namespace Forma.Framework.Localization.Tests;

using System.Reflection;
using System.Reflection.Emit;

using Microsoft.Extensions.Logging;

public sealed class LocalePackLoaderTests
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
            var applications = LocalePackLoader.GetApplicationAssemblies(entry, [entry, library]);

            // Assert
            Assert.Equal([entry], applications);
        }

        [Fact]
        public void Should_read_them_from_each_assembly_that_embeds_a_host_s_files_names_one_or_records_a_pack_where_the_runtime_names_no_entry()
        {
            // Arrange — on a runtime naming no entry assembly, as .NET for Android's: an application embedding a host's files, a server
            // naming its client, a library recording a pack, the framework, and a dynamic assembly, which lists no resources.
            var application = new FakeAssembly("Contoso.App") { Resources = { [LocalePackSource.HostResourcePrefix + "Contoso.Library.he.json"] = "{}" } };
            var server = new FakeAssembly("Contoso.Server") { Attributes = { new LocaleReferenceAttribute("Contoso.Client") } };
            var library = new FakeAssembly("Contoso.Library") { Attributes = { new LocalePackAttribute("Contoso.Hebrew") } };
            var framework = new FakeAssembly("System.Runtime") { Resources = { ["System.Runtime.Strings.resources"] = string.Empty } };
            var dynamic = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Contoso.Dynamic"), AssemblyBuilderAccess.Run);

            // Act
            var applications = LocalePackLoader.GetApplicationAssemblies(null, [framework, application, dynamic, server, library]);

            // Assert
            Assert.Equal([application, server, library], applications);
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
            var names = LocalePackLoader.GetNames(assembly);

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
            var packs = LocalePackLoader.Load(assembly, loggerFactory.CreateLogger("packs"));

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
            var packs = LocalePackLoader.Load(assembly, loggerFactory.CreateLogger("packs"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("Forma.Framework.Localization", Assert.Single(packs).GetName().Name),
                () => Assert.Equal(LogLevel.Warning, Assert.Single(loggerFactory.Entries).Level),
                () => Assert.Contains("Contoso.Missing", Assert.Single(loggerFactory.Entries).Message, StringComparison.Ordinal));
        }
    }
}
