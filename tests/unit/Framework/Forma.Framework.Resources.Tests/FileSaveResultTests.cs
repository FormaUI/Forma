namespace Forma.Framework.Resources.Tests;

using Xunit;

public sealed class FileSaveResultTests
{
    [Fact]
    public void Should_hold_the_resource_a_saved_file_became()
    {
        // Arrange
        var resource = new FileResourceInfo("/media/cover.jpg");

        // Act
        var result = FileSaveResult.Saved(resource);

        // Assert
        Assert.Multiple(
            () => Assert.True(result.IsSaved),
            () => Assert.Same(resource, result.Resource),
            () => Assert.Null(result.Refusal));
    }

    [Fact]
    public void Should_hold_the_refusal_of_a_refused_file()
    {
        // Arrange
        var refusal = new FileRefusal(FileRefusalReason.TooWide, 4000);

        // Act
        var result = FileSaveResult.Refused(refusal);

        // Assert
        Assert.Multiple(
            () => Assert.False(result.IsSaved),
            () => Assert.Same(refusal, result.Refusal),
            () => Assert.Null(result.Resource));
    }

    [Fact]
    public void Should_reject_a_missing_resource_or_refusal()
        => Assert.Multiple(
            () => Assert.Throws<ArgumentNullException>(() => FileSaveResult.Saved(null!)),
            () => Assert.Throws<ArgumentNullException>(() => FileSaveResult.Refused(null!)));
}
