namespace Forma.Framework.Localization.Json.Tests;

using System.Reflection;

using Forma.Framework.Localization;
using Forma.Framework.Localization.Json.Tests.Fixtures;
using Forma.Tests.Localization;

using Microsoft.Extensions.Logging;

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
            // Arrange — the host's root file overrides Save; nothing of the host's names Open, so the pack's Open stands.
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
            // Arrange — the same file under another assembly's name completes none of this assembly's.
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

    public sealed class Loading
    {
        [Fact]
        public void Should_name_each_pack_the_assembly_was_built_against_once_in_order()
        {
            // Arrange — a build that recorded one pack twice and one blank.
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
            // Arrange — the localization library stands in for a pack, being an assembly the runtime can load by name.
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
            // Arrange — a deployment that lost the pack's file, which still renders in the language its keys are written in.
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
