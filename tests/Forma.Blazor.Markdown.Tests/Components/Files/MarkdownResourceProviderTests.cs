namespace Forma.Blazor.Markdown.Tests.Components.Files;

using Forma.Blazor.Components.Files;
using Forma.Blazor.Markdown.Tests.Support.Components.Files;

public sealed class MarkdownResourceProviderTests
{
    [Theory]
    [InlineData("/media/report.docx")]
    [InlineData("/media/photo.png")]
    [InlineData("/media/notes.txt")]
    [InlineData("/media/notes")]
    public async Task Should_leave_an_address_it_does_not_claim_to_another_provider(string source)
    {
        // Arrange
        var provider = new MarkdownResourceProvider(new FakeResourceContentReader());

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(resource);
    }

    [Theory]
    [InlineData("/media/notes.md")]
    [InlineData("/media/NOTES.MD")]
    [InlineData("/media/notes.markdown")]
    public async Task Should_convert_a_document_to_markup(string source)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(source, "# Release notes\n\nShipping **today**.");
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync(source, TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Equal(ResourceKind.Document, markup.Kind);
            Assert.Contains("Release notes</h1>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<strong>today</strong>", markup.Html, StringComparison.Ordinal);
        });
    }

    // Tables come from the advanced extensions, which is what a reader expects of a Markdown file.
    [Fact]
    public async Task Should_convert_a_pipe_table()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add(
            "/media/notes.md",
            "| Region | Change |\n|---|---|\n| South | +11.7% |");
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.Contains("<table>", markup.Html, StringComparison.Ordinal);
            Assert.Contains("<td>South</td>", markup.Html, StringComparison.Ordinal);
        });
    }

    // The viewer renders this markup as trusted HTML, so an embedded tag must never survive as one.
    [Theory]
    [InlineData("<script>alert(1)</script>", "<script")]
    [InlineData("<img src=x onerror=alert(1)>", "<img")]
    [InlineData("<div onclick=\"alert(1)\">text</div>", "<div")]
    [InlineData("<iframe src=\"https://example.com\"></iframe>", "<iframe")]
    public async Task Should_drop_raw_html_a_document_embeds(string markdown, string forbidden)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/notes.md", markdown);
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Multiple(() =>
        {
            Assert.DoesNotContain(forbidden, markup.Html, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("&lt;", markup.Html, StringComparison.Ordinal);
        });
    }

    [Theory]
    [InlineData("[Click](javascript:alert(1))", "javascript:")]
    [InlineData("[Click](vbscript:msgbox(1))", "vbscript:")]
    [InlineData("![Shot](data:text/html;base64,PHNjcmlwdD4=)", "data:text/html")]
    public async Task Should_strip_a_link_carrying_a_script_scheme(string markdown, string forbidden)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/notes.md", markdown);
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.DoesNotContain(forbidden, markup.Html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Should_keep_an_ordinary_link_intact()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/notes.md", "[Docs](https://example.com/docs)");
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains("https://example.com/docs", markup.Html, StringComparison.Ordinal);
    }

    // A document routinely addresses a mailbox or a sibling file, neither of which is a scripting risk.
    [Theory]
    [InlineData("[Mail](mailto:team@example.com)", "mailto:team@example.com")]
    [InlineData("[Sibling](./other.md)", "./other.md")]
    [InlineData("[Anchor](#what-survives)", "#what-survives")]
    public async Task Should_keep_a_link_that_carries_no_script(string markdown, string expected)
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/notes.md", markdown);
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains(expected, markup.Html, StringComparison.Ordinal);
    }

    // A host with no reader for the address must degrade to the viewer's download link.
    [Fact]
    public async Task Should_return_nothing_when_the_reader_does_not_have_the_content()
    {
        // Arrange
        var provider = new MarkdownResourceProvider(new FakeResourceContentReader());

        // Act
        var resource = await provider.GetResourceAsync("/media/missing.md", TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(resource);
    }

    [Fact]
    public async Task Should_report_the_size_of_the_document_it_read()
    {
        // Arrange
        const string Markdown = "# Sized \u00e9";
        var reader = new FakeResourceContentReader().Add("/media/notes.md", Markdown);
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(System.Text.Encoding.UTF8.GetByteCount(Markdown), resource?.Size);
    }

    // A file saved from an editor carries a byte order mark, which must not survive into the heading.
    [Fact]
    public async Task Should_read_a_document_that_begins_with_a_byte_order_mark()
    {
        // Arrange
        var bytes = new List<byte>([0xEF, 0xBB, 0xBF]);

        bytes.AddRange(System.Text.Encoding.UTF8.GetBytes("# Heading"));

        var reader = new FakeResourceContentReader().Add("/media/notes.md", [.. bytes]);
        var provider = new MarkdownResourceProvider(reader);

        // Act
        var resource = await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        var markup = Assert.IsType<MarkupResourceInfo>(resource);

        Assert.Contains(">Heading</h1>", markup.Html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_close_the_stream_the_reader_handed_it()
    {
        // Arrange
        var reader = new FakeResourceContentReader().Add("/media/notes.md", "# Closed");
        var provider = new MarkdownResourceProvider(reader);

        // Act
        await provider.GetResourceAsync("/media/notes.md", TestContext.Current.CancellationToken);

        // Assert
        Assert.False(reader.HasOpenStream);
    }

    [Fact]
    public void Should_claim_both_markdown_extensions()
    {
        // Act & Assert
        Assert.Equal([".markdown", ".md"], MarkdownResourceProvider.ClaimedExtensions.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task Should_reject_an_address_that_is_missing()
    {
        // Arrange
        var provider = new MarkdownResourceProvider(new FakeResourceContentReader());

        // Act
        var thrown = await Record.ExceptionAsync(
            () => provider.GetResourceAsync(" ", TestContext.Current.CancellationToken).AsTask());

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Should_reject_construction_without_a_reader()
    {
        // Act
        var thrown = Record.Exception(() => new MarkdownResourceProvider(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }
}
