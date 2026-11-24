namespace Forma.Framework.Resources.Tests;

using Xunit;

public sealed class BrowserContentTypesTests
{
    [Theory]
    [InlineData(".jpg", "image/jpeg")]
    [InlineData(".JPEG", "image/jpeg")]
    [InlineData(".svg", "image/svg+xml")]
    [InlineData(".webm", "video/webm")]
    [InlineData(".pdf", "application/pdf")]
    public void Should_name_the_type_a_browser_renders_an_extension_as(string extension, string expected)
    {
        // Act
        var isKnown = BrowserContentTypes.TryGetByExtension(extension, out var contentType);

        // Assert
        Assert.Multiple(
            () => Assert.True(isKnown),
            () => Assert.Equal(expected, contentType));
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData("jpg")]
    [InlineData("")]
    public void Should_name_no_type_for_an_extension_a_browser_doesnt_render(string extension)
    {
        // Act
        var isKnown = BrowserContentTypes.TryGetByExtension(extension, out var contentType);

        // Assert
        Assert.Multiple(
            () => Assert.False(isKnown),
            () => Assert.Null(contentType));
    }
}
