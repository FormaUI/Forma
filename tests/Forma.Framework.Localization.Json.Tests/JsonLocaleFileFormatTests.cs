namespace Forma.Framework.Localization.Json.Tests;

using System.Text;
using System.Text.Json;

public sealed class JsonLocaleFileFormatTests
{
    private static LocaleFile Read(string json, string cultureName = "he")
        => new JsonLocaleFileFormat().Read(new MemoryStream(Encoding.UTF8.GetBytes(json)), "file.json", cultureName);

    [Fact]
    public void Should_name_its_extension()
    {
        // Assert
        Assert.Equal(".json", new JsonLocaleFileFormat().Extension);
    }

    [Fact]
    public void Should_read_text_and_null_as_entries_and_leave_the_marker_out()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Save": "שמור", "Reset": null }""");

        // Assert
        Assert.Equal(
            [new LocaleFileEntry(string.Empty, "Save", "שמור"), new LocaleFileEntry(string.Empty, "Reset", null)],
            file.GetEntries());
    }

    [Fact]
    public void Should_read_an_object_as_a_section()
    {
        // Arrange
        var file = Read("""
            {
              "$localization": "SchemaVersion: 1.0.0; Culture: he;",
              "Bars.Toolbar": { "Bold": "מודגש" },
              "Menus": { "ShareMenu": { "Share": "שיתוף" } },
            }
            """);

        // Assert
        Assert.Equal(
            [new LocaleFileEntry("Bars.Toolbar", "Bold", "מודגש"), new LocaleFileEntry("Menus.ShareMenu", "Share", "שיתוף")],
            file.GetEntries());
    }

    [Fact]
    public void Should_read_the_root_the_file_declares()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he; RootNamespace: Contoso.Widgets;" }""");

        // Assert
        Assert.Equal("Contoso.Widgets", file.RootNamespace);
    }

    [Fact]
    public void Should_reject_a_value_that_is_not_text()
    {
        // Act
        var thrown = Assert.Throws<JsonException>(() => Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Save": 1 }"""));

        // Assert
        Assert.Contains("'Save'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_file_that_is_not_an_object()
    {
        // Assert
        Assert.Throws<JsonException>(() => Read("""[ "Save" ]"""));
    }

    [Fact]
    public void Should_reject_a_file_whose_culture_contradicts_its_name()
    {
        // Assert
        Assert.Throws<LocalizationMetadataException>(() => Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: es;" }""", "he"));
    }
}
