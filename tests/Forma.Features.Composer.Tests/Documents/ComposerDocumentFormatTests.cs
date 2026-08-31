namespace Forma.Features.Composer.Tests.Documents;

using Forma.Features.Composer.Documents;

using Xunit;

public sealed class ComposerDocumentFormatTests
{
    [Fact]
    public void Should_lowercase_the_id()
    {
        // Act
        var format = new ComposerDocumentFormat("HTML");

        // Assert
        Assert.Equal("html", format.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_a_blank_id(string? id)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => new ComposerDocumentFormat(id!));
    }

    [Fact]
    public void Should_equal_a_format_differing_only_in_case()
    {
        // Arrange
        var lower = new ComposerDocumentFormat("latex");

        // Act
        var upper = new ComposerDocumentFormat("LATEX");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(lower, upper),
            () => Assert.Equal(lower.GetHashCode(), upper.GetHashCode()));
    }

    [Fact]
    public void Should_render_as_its_id()
    {
        // Arrange
        var format = ComposerDocumentFormat.WidgetTree;

        // Act
        var rendered = format.ToString();

        // Assert
        Assert.Equal("widget-tree", rendered);
    }
}
