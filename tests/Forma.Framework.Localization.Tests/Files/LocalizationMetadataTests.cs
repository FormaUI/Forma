namespace Forma.Framework.Localization.Tests.Files;

public sealed class LocalizationMetadataTests
{
    [Fact]
    public void Should_read_the_root_namespace_a_file_declares()
    {
        // Act
        var metadata = LocalizationMetadata.Read("SchemaVersion: 1.0.0; Culture: he; RootNamespace: Contoso.Widgets;", "file", "he");

        // Assert
        Assert.Equal("Contoso.Widgets", metadata.RootNamespace);
    }

    [Fact]
    public void Should_read_no_root_namespace_when_a_file_declares_none()
    {
        // Act
        var metadata = LocalizationMetadata.Read("SchemaVersion: 1.0.0; Culture: he;", "file", "he");

        // Assert
        Assert.Null(metadata.RootNamespace);
    }

    [Fact]
    public void Should_reject_an_absent_entry()
    {
        // Act
        var thrown = Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Read(null, "file", "he"));

        // Assert
        Assert.Contains("file", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Should_reject_a_culture_the_file_name_contradicts()
    {
        // Assert
        Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Read("SchemaVersion: 1.0.0; Culture: es;", "file", "he"));
    }

    [Fact]
    public void Should_reject_a_schema_major_this_build_does_not_read()
    {
        // Assert
        Assert.Throws<LocalizationMetadataException>(() => LocalizationMetadata.Read("SchemaVersion: 2.0.0; Culture: he;", "file", "he"));
    }

    [Fact]
    public void Should_read_a_later_minor_schema()
    {
        // Act
        var metadata = LocalizationMetadata.Read("SchemaVersion: 1.4.0; Culture: he; Future: whatever;", "file", "he");

        // Assert
        Assert.Null(metadata.RootNamespace);
    }
}
