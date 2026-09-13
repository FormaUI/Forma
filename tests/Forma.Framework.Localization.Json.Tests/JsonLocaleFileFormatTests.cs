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
    public void Should_leave_the_entry_reserved_for_tools_out()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$tools": { "locali.ignore": ["LOC0113"], "other": 1 }, "Save": "שמור" }""");

        // Assert
        Assert.Equal([new LocaleFileEntry(string.Empty, "Save", "שמור")], file.GetEntries());
    }

    [Fact]
    public void Should_leave_the_entry_reserved_for_tools_out_of_a_section_too()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Toolbar": { "$tools": { "crowdin.id": 1 }, "Save": "שמור" } }""");

        // Assert
        Assert.Equal([new LocaleFileEntry("Toolbar", "Save", "שמור")], file.GetEntries());
    }

    [Fact]
    public void Should_reject_a_key_shaped_like_a_reserved_entry_it_does_not_define()
    {
        // Act
        var thrown = Assert.Throws<JsonException>(() => Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$import": "x" }"""));

        // Assert
        Assert.Contains("'$import'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_key_shaped_like_a_reserved_entry_inside_a_section_too()
    {
        // Act
        var thrown = Assert.Throws<JsonException>(() => Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "Toolbar": { "$import": "x" } }"""));

        // Assert
        Assert.Contains("'$import'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_read_an_escaped_reserved_name_as_an_ordinary_translation()
    {
        // Arrange — the names are interpreted as written, so once escaped they are text like any other.
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$$localization": "a", "$$shared": "b", "$$tools": "c" }""");

        // Assert
        Assert.Equal(
            [new LocaleFileEntry(string.Empty, "$localization", "a"), new LocaleFileEntry(string.Empty, "$shared", "b"), new LocaleFileEntry(string.Empty, "$tools", "c")],
            file.GetEntries());
    }

    [Fact]
    public void Should_read_an_escaped_shared_name_as_an_ordinary_section()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$$shared": { "Bold": "x" } }""");

        // Assert
        Assert.Multiple(
            () => Assert.Equal("$shared", Assert.Single(file.Root.Sections).Key),
            () => Assert.Empty(file.Root.SharedEntries));
    }

    [Theory]
    [InlineData("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": { "Toolbar": { "Bold": "x" } } }""", "'Toolbar'")]
    [InlineData("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": { "$shared": { "Bold": "x" } } }""", "'$shared'")]
    public void Should_reject_a_section_nested_inside_the_shared_one(string content, string nested)
    {
        // Act — the shared section holds translations alone, so a section inside it would otherwise vanish without a word.
        var thrown = Assert.Throws<JsonException>(() => Read(content));

        // Assert
        Assert.Contains(nested, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_shared_entry_that_is_not_an_object()
    {
        // Act
        var thrown = Assert.Throws<JsonException>(() => Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": "x" }"""));

        // Assert
        Assert.Contains("'$shared'", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("$")]
    [InlineData("$USD")]
    [InlineData("$5 off")]
    [InlineData("$camelCase")]
    [InlineData("$ per month")]
    public void Should_read_any_other_key_starting_with_a_dollar_as_text(string key)
    {
        // Arrange
        var file = Read($$"""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "{{key}}": "x" }""");

        // Assert
        Assert.Equal([new LocaleFileEntry(string.Empty, key, "x")], file.GetEntries());
    }

    [Fact]
    public void Should_unescape_a_doubled_dollar_in_a_key()
    {
        // Arrange — at the top level and inside a section alike.
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$$price": "מחיר", "Toolbar": { "$$price": "מחיר" } }""");

        // Assert
        Assert.Equal(
            [new LocaleFileEntry(string.Empty, "$price", "מחיר"), new LocaleFileEntry("Toolbar", "$price", "מחיר")],
            file.GetEntries());
    }

    [Fact]
    public void Should_read_the_shared_section_as_a_section()
    {
        // Arrange
        var file = Read("""{ "$localization": "SchemaVersion: 1.0.0; Culture: he;", "$shared": { "Bold": "מודגש" }, "Toolbar": { "Save": "שמור" } }""");

        // Assert
        Assert.Multiple(
            () => Assert.Equal([new KeyValuePair<string, string?>("Bold", "מודגש")], file.Root.SharedEntries),
            () => Assert.Equal(
                [new LocaleFileEntry("$shared", "Bold", "מודגש"), new LocaleFileEntry("Toolbar", "Save", "שמור")],
                file.GetEntries()));
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
