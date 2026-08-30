namespace Forma.UI.Features.Composer.Tests.Documents;

using Forma.UI.Features.Composer.Documents;

using Xunit;

public sealed class HtmlSourceFormatterTests
{
    [Fact]
    public void Should_put_each_block_on_a_line_of_its_own()
    {
        // Act
        var formatted = HtmlSourceFormatter.Format("<p>One</p><p>Two</p>");

        // Assert
        Assert.Equal("<p>One</p>\n<p>Two</p>", formatted);
    }

    [Fact]
    public void Should_indent_nested_blocks_and_keep_inline_markup_on_its_line()
    {
        // Act
        var formatted = HtmlSourceFormatter.Format("<ul><li>A <strong>bold</strong> one</li><li>Two</li></ul>");

        // Assert
        Assert.Equal("<ul>\n  <li>A <strong>bold</strong> one</li>\n  <li>Two</li>\n</ul>", formatted);
    }

    [Fact]
    public void Should_keep_preformatted_content_exactly_as_written()
    {
        // Act
        var formatted = HtmlSourceFormatter.Format("<div><pre>line one\n  line two</pre></div>");

        // Assert
        Assert.Equal("<div>\n  <pre>line one\n  line two</pre>\n</div>", formatted);
    }

    [Fact]
    public void Should_give_a_void_block_a_line_without_indenting_what_follows()
    {
        // Act
        var formatted = HtmlSourceFormatter.Format("<p>Before</p><hr><p>After</p>");

        // Assert
        Assert.Equal("<p>Before</p>\n<hr>\n<p>After</p>", formatted);
    }

    [Fact]
    public void Should_leave_plain_text_alone()
    {
        // Act
        var formatted = HtmlSourceFormatter.Format("just words");

        // Assert
        Assert.Equal("just words", formatted);
    }
}
