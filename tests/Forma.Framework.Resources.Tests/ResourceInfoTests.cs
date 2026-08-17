namespace Forma.Framework.Resources.Tests;

using Xunit;

public sealed class ResourceInfoTests
{
    [Theory]
    [InlineData("/media/cover.jpg", "cover.jpg")]
    [InlineData("cover.jpg", "cover.jpg")]
    [InlineData("https://cdn.example.com/media/cover.jpg", "cover.jpg")]
    [InlineData("C:\\media\\cover.jpg", "cover.jpg")]
    public void Should_take_its_name_from_the_source(string source, string expected)
    {
        var resource = new ResourceInfo(source);

        Assert.Equal(expected, resource.Name);
    }

    [Theory]
    [InlineData("/media/cover.jpg?v=2")]
    [InlineData("/media/cover.jpg#top")]
    [InlineData("/media/cover.jpg?v=2#top")]
    public void Should_drop_a_query_and_fragment_from_the_name(string source)
    {
        var resource = new FileResourceInfo(source);

        Assert.Multiple(() =>
        {
            Assert.Equal("cover.jpg", resource.Name);
            Assert.Equal(".jpg", resource.Extension);
        });
    }

    [Fact]
    public void Should_unescape_a_name_the_source_encoded()
    {
        var resource = new ResourceInfo("/media/stage%201.jpg");

        Assert.Equal("stage 1.jpg", resource.Name);
    }

    [Fact]
    public void Should_default_to_knowing_nothing_beyond_the_source()
    {
        var resource = new ResourceInfo("/media/whatever.bin");

        Assert.Multiple(() =>
        {
            Assert.Null(resource.Size);
            Assert.Null(resource.CreatedAt);
            Assert.Null(resource.ModifiedAt);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_an_invalid_source(string? source)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ResourceInfo(source!));
    }

    [Fact]
    public void Should_carry_converted_markup_as_a_document()
    {
        var resource = new MarkupResourceInfo("/media/brief.docx", "<p>Body.</p>");

        Assert.Multiple(() =>
        {
            Assert.Equal("<p>Body.</p>", resource.Html);
            Assert.IsAssignableFrom<DocumentResourceInfo>(resource);
            Assert.Equal("brief.docx", resource.Name);
        });
    }
}
