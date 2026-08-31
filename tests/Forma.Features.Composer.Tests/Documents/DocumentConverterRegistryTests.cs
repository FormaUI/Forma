namespace Forma.Features.Composer.Tests.Documents;

using Forma.Features.Composer.Documents;

using Xunit;

public sealed class DocumentConverterRegistryTests
{
    private sealed class FakeMarkdownToHtmlConverter : IComposerDocumentConverter
    {
        public ComposerDocumentFormat Source
            => ComposerDocumentFormat.Markdown;

        public ComposerDocumentFormat Target
            => ComposerDocumentFormat.Html;

        public ComposerConversionResult Convert(string content)
            => ComposerConversionResult.Success($"<p>{content}</p>");
    }

    [Fact]
    public void Should_find_a_registered_converter()
    {
        // Arrange
        var converter = new FakeMarkdownToHtmlConverter();
        var registry = new DocumentConverterRegistry([converter]);

        // Act
        var found = registry.Find(ComposerDocumentFormat.Markdown, ComposerDocumentFormat.Html);

        // Assert
        Assert.Multiple(
            () => Assert.Same(converter, found),
            () => Assert.True(registry.CanConvert(ComposerDocumentFormat.Markdown, ComposerDocumentFormat.Html)));
    }

    [Fact]
    public void Should_carry_a_document_already_in_the_target_format()
    {
        // Arrange
        var registry = new DocumentConverterRegistry();
        var document = new ComposerDocument(ComposerDocumentFormat.Html, "<p>Hello</p>");

        // Act
        var result = registry.Convert(document, ComposerDocumentFormat.Html);

        // Assert
        Assert.Multiple(
            () => Assert.True(result.Succeeded),
            () => Assert.Equal("<p>Hello</p>", result.Content));
    }

    [Fact]
    public void Should_refuse_when_no_converter_is_registered()
    {
        // Arrange
        var registry = new DocumentConverterRegistry();
        var document = new ComposerDocument(ComposerDocumentFormat.Html, "<p>Hello</p>");

        // Act
        var result = registry.Convert(document, ComposerDocumentFormat.Markdown);

        // Assert
        Assert.Multiple(
            () => Assert.False(result.Succeeded),
            () => Assert.Null(result.Content),
            () => Assert.NotEmpty(result.UnsupportedFeatures));
    }

    [Fact]
    public void Should_run_the_registered_converter()
    {
        // Arrange
        var registry = new DocumentConverterRegistry([new FakeMarkdownToHtmlConverter()]);
        var document = new ComposerDocument(ComposerDocumentFormat.Markdown, "Hello");

        // Act
        var result = registry.Convert(document, ComposerDocumentFormat.Html);

        // Assert
        Assert.Multiple(
            () => Assert.True(result.Succeeded),
            () => Assert.Equal("<p>Hello</p>", result.Content));
    }

    [Fact]
    public void Should_replace_a_converter_registered_for_the_same_pair()
    {
        // Arrange
        var registry = new DocumentConverterRegistry([new FakeMarkdownToHtmlConverter()]);
        var replacement = new FakeMarkdownToHtmlConverter();

        // Act
        registry.Register(replacement);

        // Assert
        Assert.Same(replacement, registry.Find(ComposerDocumentFormat.Markdown, ComposerDocumentFormat.Html));
    }
}
