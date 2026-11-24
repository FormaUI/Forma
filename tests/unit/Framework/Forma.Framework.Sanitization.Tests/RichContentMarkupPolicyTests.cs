namespace Forma.Framework.Sanitization.Tests;

using Xunit;

public sealed class RichContentMarkupPolicyTests
{
    public sealed class Allows
    {
        private readonly RichContentMarkupPolicy _sut = RichContentMarkupPolicy.Default;

        [Theory]
        [InlineData("p")]
        [InlineData("strong")]
        [InlineData("figcaption")]
        public void Should_permit_an_element_the_plugins_produce(string element)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Element, element, default, out _);

            // Assert
            Assert.True(allowed);
        }

        [Theory]
        [InlineData("script")]
        [InlineData("marquee")]
        public void Should_refuse_an_element_the_plugins_never_produce(string element)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Element, element, default, out _);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Should_write_an_element_with_the_spelling_the_policy_holds()
        {
            // Act
            _sut.Allows(HtmlSubject.Element, "BlockQuote", default, out var canonical);

            // Assert
            Assert.Equal("blockquote", canonical);
        }

        [Theory]
        [InlineData("script")]
        [InlineData("style")]
        [InlineData("iframe")]
        public void Should_refuse_the_content_of_an_element_that_would_survive_as_markup(string element)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.ElementContent, element, default, out _);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Should_permit_the_content_of_an_ordinary_element()
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.ElementContent, "p", default, out _);

            // Assert
            Assert.True(allowed);
        }

        [Fact]
        public void Should_permit_a_global_attribute_on_any_element()
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Attribute, "class", "div", out var canonical);

            // Assert
            Assert.True(allowed);
            Assert.Equal("class", canonical);
        }

        [Fact]
        public void Should_permit_an_attribute_only_on_the_element_owning_it()
        {
            // Act
            var onAnchor = _sut.Allows(HtmlSubject.Attribute, "href", "a", out _);
            var onParagraph = _sut.Allows(HtmlSubject.Attribute, "href", "p", out _);

            // Assert
            Assert.True(onAnchor);
            Assert.False(onParagraph);
        }

        [Fact]
        public void Should_permit_a_data_attribute_and_keep_the_spelling_it_was_written_with()
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Attribute, "data-Video-Url", "div", out var canonical);

            // Assert
            Assert.True(allowed);
            Assert.Null(canonical);
        }

        [Fact]
        public void Should_permit_a_style_property_and_a_forma_custom_property()
        {
            // Act
            var known = _sut.Allows(HtmlSubject.StyleProperty, "Color", default, out var canonical);
            var custom = _sut.Allows(HtmlSubject.StyleProperty, "--forma-composer-color", default, out var customCanonical);

            // Assert
            Assert.True(known);
            Assert.Equal("color", canonical);
            Assert.True(custom);
            Assert.Null(customCanonical);
        }

        [Theory]
        [InlineData("position")]
        [InlineData("--other-color")]
        public void Should_refuse_a_style_property_outside_the_policy(string property)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.StyleProperty, property, default, out _);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("background: url(evil.png)")]
        [InlineData("color: expression(alert(1))")]
        [InlineData("behavior: url(#default)")]
        public void Should_refuse_a_style_declaration_however_it_is_spelled(string declaration)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.StyleDeclaration, declaration, default, out _);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("a", "https://example.com/photo.jpg")]
        [InlineData("a", "http://example.com")]
        [InlineData("a", "mailto:editor@example.com")]
        [InlineData("a", "tel:+573001234567")]
        [InlineData("img", "/media/podium.png")]
        [InlineData("img", "./relative.png")]
        [InlineData("img", "../up.png")]
        [InlineData("img", "images/podium.png")]
        [InlineData("img", "podium.png")]
        [InlineData("a", "files/report.pdf?page=2")]
        [InlineData("a", "#section-2")]
        [InlineData("img", "data:image/png;base64,iVBORw0KGgo=")]
        [InlineData("img", "blob:https://example.com/8f4b0c2e")]
        [InlineData("video", "data:video/mp4;base64,AAAA")]
        [InlineData("video", "data:image/png;base64,iVBORw0KGgo=")]
        [InlineData("audio", "data:audio/mpeg;base64,AAAA")]
        [InlineData("source", "data:audio/ogg;base64,AAAA")]
        [InlineData("source", "data:video/webm;base64,AAAA")]
        public void Should_permit_an_address_the_document_may_carry(string element, string address)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, address, element, out _);

            // Assert
            Assert.True(allowed);
        }

        [Theory]
        [InlineData("a", "javascript:alert(1)")]
        [InlineData("a", "JaVaScRiPt:alert(1)")]
        [InlineData("a", " \u0001javascript:alert(1)")]
        [InlineData("img", "javascript:alert(1)")]
        [InlineData("a", "ftp://files.example.com")]
        [InlineData("a", "data:text/html,<script>alert(1)</script>")]
        [InlineData("img", "data:image/svg+xml,<svg onload=alert(1)>")]
        [InlineData("a", "just some text")]
        [InlineData("img", "images/a podium.png")]
        [InlineData("a", "vbscript:msgbox(1)")]
        [InlineData("a", "java\tscript:alert(1)")]
        [InlineData("a", "")]
        [InlineData("a", "   ")]
        [InlineData("a", "http://")]
        [InlineData("a", "https://")]
        [InlineData("a", "mailto:")]
        [InlineData("a", "tel:")]
        [InlineData("a", "#")]
        [InlineData("img", "data:image/png")]
        public void Should_refuse_an_address_the_document_may_not_carry(string element, string address)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, address, element, out _);

            // Assert
            Assert.False(allowed);
        }

        [Theory]
        [InlineData("a", "data:image/png;base64,iVBORw0KGgo=")]
        [InlineData("a", "blob:https://example.com/8f4b0c2e")]
        [InlineData("img", "mailto:editor@example.com")]
        [InlineData("img", "data:video/mp4;base64,AAAA")]
        [InlineData("audio", "data:video/mp4;base64,AAAA")]
        [InlineData("source", "data:image/png;base64,iVBORw0KGgo=")]
        public void Should_refuse_an_address_its_element_does_not_write(string element, string address)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, address, element, out _);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Should_judge_an_address_of_no_element_as_a_link()
        {
            // Act
            var isLinkAllowed = _sut.Allows(HtmlSubject.Url, "mailto:editor@example.com", default, out _);
            var isImageAllowed = _sut.Allows(HtmlSubject.Url, "data:image/png;base64,iVBORw0KGgo=", default, out _);

            // Assert
            Assert.True(isLinkAllowed);
            Assert.False(isImageAllowed);
        }

        [Fact]
        public void Should_judge_an_address_after_trimming_it()
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, "  https://example.com  ", "a", out _);

            // Assert
            Assert.True(allowed);
        }
    }

    public sealed class AgainstTheSanitizer
    {
        // A policy that answers for one element or attribute itself and defers every other question to the policy it wraps.
        private sealed class WrappingPolicy(IMarkupPolicy inner, HtmlSubject about, string name, bool answer) : IMarkupPolicy
        {
            public bool Allows(HtmlSubject subject, ReadOnlySpan<char> value, ReadOnlySpan<char> owner, out string? canonical)
            {
                if (subject == about && value.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    canonical = null;

                    return answer;
                }

                return inner.Allows(subject, value, owner, out canonical);
            }
        }

        [Fact]
        public void Should_keep_an_element_a_widened_policy_permits()
        {
            // Arrange
            var policy = new WrappingPolicy(RichContentMarkupPolicy.Default, HtmlSubject.Element, "marquee", answer: true);
            var sut = new AllowlistHtmlSanitizer(policy);

            // Act
            var result = sut.Sanitize("<marquee>Stage 12</marquee>");

            // Assert
            Assert.Contains("marquee", result, StringComparison.Ordinal);
        }

        [Fact]
        public void Should_drop_an_element_a_narrowed_policy_refuses()
        {
            // Arrange
            var policy = new WrappingPolicy(RichContentMarkupPolicy.Default, HtmlSubject.Element, "strong", answer: false);
            var sut = new AllowlistHtmlSanitizer(policy);

            // Act
            var result = sut.Sanitize("<p>Hello <strong>bold</strong></p>");

            // Assert
            Assert.Equal("<p>Hello bold</p>", result);
        }

        [Fact]
        public void Should_reject_an_event_handler_whatever_the_policy_says()
        {
            // Arrange
            var policy = new WrappingPolicy(RichContentMarkupPolicy.Default, HtmlSubject.Attribute, "onclick", answer: true);
            var sut = new AllowlistHtmlSanitizer(policy);

            // Act
            var result = sut.Sanitize("<p onclick=\"alert(1)\">Hello</p>");

            // Assert
            Assert.Equal("<p>Hello</p>", result);
        }

        [Fact]
        public void Should_refuse_a_sanitizer_built_without_a_policy()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AllowlistHtmlSanitizer(null!));
        }
    }
}
