namespace Forma.Framework.Sanitization.Tests;

using Xunit;

public sealed class AllowlistHtmlSanitizerTests
{
    public sealed class Sanitize
    {
        private readonly AllowlistHtmlSanitizer _sut = new();

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Should_return_empty_for_blank_input(string? input)
        {
            // Act
            var result = _sut.Sanitize(input!);

            // Assert
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public void Should_preserve_safe_formatting_tags()
        {
            // Arrange
            var input = "<p>Hello <strong>bold</strong> <em>italic</em> <a href=\"https://example.com\">link</a></p>";

            // Act
            var result = _sut.Sanitize(input);

            // Assert
            Assert.Equal(input, result);
        }

        [Fact]
        public void Should_strip_script_tags_and_content()
        {
            // Act
            var result = _sut.Sanitize("<p>Safe</p><script>alert('xss')</script>");

            // Assert
            Assert.Equal("<p>Safe</p>", result);
        }

        [Fact]
        public void Should_strip_event_handler_attributes()
        {
            // Act
            var result = _sut.Sanitize("<p onclick=\"steal()\" onmouseover=\"bad()\">Text</p>");

            // Assert
            Assert.Equal("<p>Text</p>", result);
        }

        [Fact]
        public void Should_strip_javascript_urls()
        {
            // Act
            var result = _sut.Sanitize("<a href=\"javascript:alert(1)\">Click</a>");

            // Assert
            Assert.Equal("<a>Click</a>", result);
        }

        [Fact]
        public void Should_strip_iframes_and_objects()
        {
            // Act
            var result = _sut.Sanitize("<div>Safe<iframe src=\"https://evil.com\"></iframe><object data=\"bad\"></object></div>");

            // Assert
            Assert.Equal("<div>Safe</div>", result);
        }

        [Fact]
        public void Should_not_split_a_tag_on_a_quoted_angle_bracket()
        {
            // Arrange
            var input = "<a href=\"https://example.com\" title=\"a > b\">click</a>";

            // Act
            var result = _sut.Sanitize(input);

            // Assert
            Assert.Equal("<a href=\"https://example.com\" title=\"a &gt; b\">click</a>", result);
        }

        [Fact]
        public void Should_keep_an_unquoted_attribute_value()
        {
            // Act
            var result = _sut.Sanitize("<img src=/media/8842/card.jpg alt=Rider>");

            // Assert
            Assert.Equal("<img src=\"/media/8842/card.jpg\" alt=\"Rider\" />", result);
        }

        [Fact]
        public void Should_keep_the_name_attribute_on_an_anchor_because_it_is_how_a_named_anchor_is_addressed()
        {
            // Act
            var result = _sut.Sanitize("<a id=\"section-1\" name=\"section-1\"></a>");

            // Assert
            Assert.Equal("<a id=\"section-1\" name=\"section-1\"></a>", result);
        }

        [Fact]
        public void Should_drop_the_closing_tag_of_a_void_element()
        {
            // Act
            var result = _sut.Sanitize("<p>a<br></br>b</p>");

            // Assert
            Assert.Equal("<p>a<br />b</p>", result);
        }

        [Fact]
        public void Should_reject_svg_data_urls()
        {
            // Act
            var result = _sut.Sanitize("<img src=\"data:image/svg+xml;base64,PHN2Zz48L3N2Zz4=\" alt=\"x\" />");

            // Assert
            Assert.DoesNotContain("data:image/svg", result, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Should_allow_raster_data_urls()
        {
            // Act
            var result = _sut.Sanitize("<img src=\"data:image/png;base64,iVBORw0KGgo=\" alt=\"x\" />");

            // Assert
            Assert.Contains("data:image/png", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_allow_safe_inline_styles_and_drop_dangerous_css()
        {
            // Arrange
            var input = "<span style=\"color: red; background-image: url('bad.jpg'); font-size: 1rem;\">Styled</span>";

            // Act
            var result = _sut.Sanitize(input);

            // Assert
            Assert.Multiple(
                () => Assert.DoesNotContain("url(", result, StringComparison.Ordinal),
                () => Assert.Contains("color: red", result, StringComparison.Ordinal),
                () => Assert.Contains("font-size: 1rem", result, StringComparison.Ordinal));
        }

        [Fact]
        public void Should_keep_a_data_attribute_so_a_host_can_resolve_media()
        {
            // Act
            var result = _sut.Sanitize("<img src=\"/media/8842/card.jpg\" data-media-id=\"8842\" alt=\"x\" />");

            // Assert
            Assert.Contains("data-media-id=\"8842\"", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_leave_a_lone_angle_bracket_as_text()
        {
            // Act
            var result = _sut.Sanitize("<p>2 < 3</p>");

            // Assert
            Assert.Equal("<p>2 < 3</p>", result);
        }

        [Fact]
        public void Should_normalize_the_case_of_a_tag_and_attribute()
        {
            // Act
            var result = _sut.Sanitize("<P CLASS=\"lead\">Text</P>");

            // Assert
            Assert.Equal("<p class=\"lead\">Text</p>", result);
        }

        [Fact]
        public void Should_drop_a_style_attribute_whose_declarations_are_all_disallowed()
        {
            // Act
            var result = _sut.Sanitize("<span style=\"position: fixed; z-index: 99\">Text</span>");

            // Assert
            Assert.Equal("<span>Text</span>", result);
        }

        [Fact]
        public void Should_keep_a_forma_custom_property()
        {
            // Act
            var result = _sut.Sanitize("<span style=\"--forma-accent-color: red\">Text</span>");

            // Assert
            Assert.Contains("--forma-accent-color: red", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_discard_the_remainder_when_a_script_is_never_closed()
        {
            // Act
            var result = _sut.Sanitize("<p>Safe</p><script>alert(1)");

            // Assert
            Assert.Equal("<p>Safe</p>", result);
        }

        [Fact]
        public void Should_discard_a_comment_and_everything_inside_it()
        {
            // Act
            var result = _sut.Sanitize("<p>a<!-- <script>evil()</script> -->b</p>");

            // Assert
            Assert.Equal("<p>ab</p>", result);
        }

        [Fact]
        public void Should_discard_a_doctype()
        {
            // Act
            var result = _sut.Sanitize("<!DOCTYPE html><p>a</p>");

            // Assert
            Assert.Equal("<p>a</p>", result);
        }

        [Fact]
        public void Should_escape_markup_carried_inside_an_attribute_value()
        {
            // Act
            var result = _sut.Sanitize("<p title=\"<script>evil()</script>\">Text</p>");

            // Assert
            Assert.Equal("<p title=\"&lt;script&gt;evil()&lt;/script&gt;\">Text</p>", result);
        }

        [Fact]
        public void Should_keep_a_valueless_boolean_attribute()
        {
            // Act
            var result = _sut.Sanitize("<video src=\"/v.mp4\" controls></video>");

            // Assert
            Assert.Equal("<video src=\"/v.mp4\" controls=\"\"></video>", result);
        }
    }
}
