namespace Forma.UI.Features.Sanitization.Tests;

using Forma.Framework.Sanitization;

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
        [InlineData("https://example.com/photo.jpg")]
        [InlineData("http://example.com")]
        [InlineData("mailto:editor@epica.cc")]
        [InlineData("tel:+573001234567")]
        [InlineData("/media/podium.png")]
        [InlineData("./relative.png")]
        [InlineData("../up.png")]
        [InlineData("#section-2")]
        [InlineData("data:image/png;base64,iVBORw0KGgo=")]
        public void Should_permit_an_address_the_document_may_carry(string address)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, address, default, out _);

            // Assert
            Assert.True(allowed);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("ftp://files.example.com")]
        [InlineData("data:text/html,<script>alert(1)</script>")]
        [InlineData("data:image/svg+xml,<svg onload=alert(1)>")]
        [InlineData("just some text")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("http://")]
        [InlineData("https://")]
        [InlineData("mailto:")]
        [InlineData("tel:")]
        [InlineData("#")]
        [InlineData("data:image/png")]
        public void Should_refuse_an_address_the_document_may_not_carry(string address)
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, address, default, out _);

            // Assert
            Assert.False(allowed);
        }

        [Fact]
        public void Should_judge_an_address_after_trimming_it()
        {
            // Act
            var allowed = _sut.Allows(HtmlSubject.Url, "  https://example.com  ", default, out _);

            // Assert
            Assert.True(allowed);
        }
    }

    public sealed class AgainstTheSanitizer
    {
        // A host widens or narrows the rules by wrapping the default rather than restating it.
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
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new AllowlistHtmlSanitizer(null!));
        }
    }
}
