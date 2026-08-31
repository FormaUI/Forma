namespace Forma.Features.Explorer.Tests;

using Forma.Framework.Resources;

using Xunit;

public sealed class ExplorerResourceFilterTests
{
    [Fact]
    public void Should_read_nothing_from_a_blank_pattern()
    {
        // Assert
        Assert.Multiple(
            () => Assert.Null(ExplorerResourceFilter.Parse(null)),
            () => Assert.Null(ExplorerResourceFilter.Parse(string.Empty)),
            () => Assert.Null(ExplorerResourceFilter.Parse("   ")));
    }

    [Fact]
    public void Should_refuse_a_pattern_naming_neither_globs_nor_content_types()
    {
        // Assert
        Assert.Throws<ArgumentException>(() => ExplorerResourceFilter.Parse("*.jpg"));
    }

    [Fact]
    public void Should_keep_the_pattern_as_written()
    {
        // Act
        var filter = ExplorerResourceFilter.Parse("globs:*.jpg;*.pdf");

        // Assert
        Assert.Equal("globs:*.jpg;*.pdf", filter!.Pattern);
    }

    [Theory]
    [InlineData("photo.jpg", true)]
    [InlineData("report.pdf", true)]
    [InlineData("clip.mp4", false)]
    public void Should_accept_by_glob(string name, bool accepted)
    {
        // Arrange
        var filter = ExplorerResourceFilter.Parse("globs:*.jpg;*.pdf");

        // Assert
        Assert.Equal(accepted, filter!.Accepts(name, contentType: null));
    }

    [Theory]
    [InlineData("image/png", true)]
    [InlineData("video/mp4", true)]
    [InlineData("audio/mpeg", false)]
    public void Should_accept_by_content_type(string contentType, bool accepted)
    {
        // Arrange
        var filter = ExplorerResourceFilter.Parse("contentType:image;video");

        // Assert
        Assert.Equal(accepted, filter!.Accepts("anything", contentType));
    }

    [Fact]
    public void Should_fall_back_to_the_kind_a_resource_type_stands_for()
    {
        // Arrange
        var filter = ExplorerResourceFilter.Parse("contentType:image");
        var image = new ImageResourceInfo("/media/poster.svg") { Name = "poster.svg" };

        // Assert
        Assert.True(filter!.Accepts(image));
    }

    [Fact]
    public void Should_refuse_a_resource_whose_kind_is_not_named()
    {
        // Arrange
        var filter = ExplorerResourceFilter.Parse("contentType:image");
        var video = new VideoResourceInfo("/media/clip.mp4") { Name = "clip.mp4" };

        // Assert
        Assert.False(filter!.Accepts(video));
    }

    [Fact]
    public void Should_match_a_glob_against_a_resource_name()
    {
        // Arrange
        var filter = ExplorerResourceFilter.Parse("globs:*.svg");
        var image = new ImageResourceInfo("/media/poster.svg") { Name = "poster.svg" };
        var clip = new VideoResourceInfo("/media/clip.mp4") { Name = "clip.mp4" };

        // Assert
        Assert.Multiple(
            () => Assert.True(filter!.Accepts(image)),
            () => Assert.False(filter!.Accepts(clip)));
    }
}
