namespace Forma.Framework.Localization.Tests.Files;

public sealed class LocalizationMetadataTests
{
    [Fact]
    public void Should_accept_an_entry_declaring_the_required_fields()
        => Assert.Null(Record.Exception(() => LocalizationMetadata.Validate("SchemaVersion: 1.0.0; Culture: he;", "file", "he")));

    [Fact]
    public void Should_pass_over_a_root_namespace_the_entry_still_declares()
        => Assert.Null(Record.Exception(() => LocalizationMetadata.Validate("SchemaVersion: 1.0.0; Culture: he; RootNamespace: Contoso.Widgets;", "file", "he")));

    [Fact]
    public void Should_reject_an_absent_entry()
    {
        // Act
        var thrown = Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Validate(null, "file", "he"));

        // Assert
        Assert.Contains("file", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_culture_the_file_name_contradicts()
    {
        // Assert
        Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Validate("SchemaVersion: 1.0.0; Culture: es;", "file", "he"));
    }

    [Fact]
    public void Should_reject_a_schema_major_this_build_does_not_read()
    {
        // Assert
        Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Validate("SchemaVersion: 2.0.0; Culture: he;", "file", "he"));
    }

    [Fact]
    public void Should_accept_a_later_minor_schema()
        => Assert.Null(Record.Exception(() => LocalizationMetadata.Validate("SchemaVersion: 1.4.0; Culture: he; Future: whatever;", "file", "he")));
}
