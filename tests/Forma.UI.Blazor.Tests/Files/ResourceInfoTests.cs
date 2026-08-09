namespace Forma.UI.Blazor.Tests.Files;

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
        var resource = new ResourceInfo(source);

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
    public void Should_default_to_an_unknown_kind()
    {
        var resource = new ResourceInfo("/media/whatever.bin");

        Assert.Multiple(() =>
        {
            Assert.Equal(ResourceKind.Unknown, resource.Kind);
            Assert.Null(resource.Size);
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

    [Theory]
    [InlineData(512L, "512 B")]
    [InlineData(2048L, "2 KB")]
    [InlineData(1536L, "1.5 KB")]
    [InlineData(5_242_880L, "5 MB")]
    [InlineData(3_221_225_472L, "3 GB")]
    public void Should_display_a_size_as_bytes_by_default(long size, string expected)
    {
        var resource = new ResourceInfo("/media/whatever.bin") { Size = size };

        Assert.Equal(expected, resource.DisplaySize);
    }

    [Fact]
    public void Should_omit_a_display_size_without_a_size()
    {
        var resource = new ResourceInfo("/media/whatever.bin");

        Assert.Null(resource.DisplaySize);
    }

    [Fact]
    public void Should_let_a_resource_measured_otherwise_display_its_own_size()
    {
        var resource = new PagedResourceInfo("/media/report.pdf") { Size = 12 };

        Assert.Equal("12 pages", resource.DisplaySize);
    }

    private sealed record PagedResourceInfo(string Source) : ResourceInfo(Source)
    {
        public override string? DisplaySize
            => Size is { } pages ? $"{pages} pages" : null;
    }

    [Theory]
    [InlineData("image/png", ResourceKind.Image)]
    [InlineData("video/mp4", ResourceKind.Video)]
    [InlineData("audio/mpeg", ResourceKind.Audio)]
    [InlineData("text/html", ResourceKind.Document)]
    [InlineData("application/pdf", ResourceKind.Document)]
    [InlineData("application/zip", ResourceKind.Unknown)]
    public void Should_take_its_kind_from_the_content_type(string contentType, ResourceKind expected)
    {
        var resource = new EmbeddedResourceInfo("/media/file", contentType);

        Assert.Multiple(() =>
        {
            Assert.Equal(expected, resource.Kind);
            Assert.Equal(contentType, resource.ContentType);
        });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_an_invalid_content_type(string? contentType)
    {
        Assert.ThrowsAny<ArgumentException>(() => new EmbeddedResourceInfo("/media/file", contentType!));
    }

    [Fact]
    public void Should_carry_converted_markup_as_a_document()
    {
        var resource = new MarkupResourceInfo("/media/brief.docx", "<p>Body.</p>");

        Assert.Multiple(() =>
        {
            Assert.Equal("<p>Body.</p>", resource.Html);
            Assert.Equal(ResourceKind.Document, resource.Kind);
            Assert.Equal("brief.docx", resource.Name);
        });
    }
}
