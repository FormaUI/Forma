namespace Forma.Framework.Localization.Abstractions.Tests.Parser;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

public sealed class LocaleFieldParserTests
{
    private const string Metadata = "SchemaVersion: 1.0.0; Culture: he;";

    [Fact]
    public void Should_read_every_field_in_order()
    {
        // Act
        var result = LocaleFieldParser.Parse(Metadata);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal(2, result.Fields.Count);
            Assert.Equal("SchemaVersion", Text(result.Fields[0].NameSpan, Metadata));
            Assert.Equal("1.0.0", Text(result.Fields[0].ValueSpan, Metadata));
            Assert.Equal("Culture", Text(result.Fields[1].NameSpan, Metadata));
            Assert.Equal("he", Text(result.Fields[1].ValueSpan, Metadata));
        });
    }

    // A file is hand-edited, so it must not fail on a character its author cannot see.
    [Theory]
    [InlineData("SchemaVersion: 1.0.0; Culture: he; ")]
    [InlineData("SchemaVersion: 1.0.0; Culture: he;\n")]
    [InlineData(" SchemaVersion: 1.0.0; Culture: he;")]
    [InlineData("\n  SchemaVersion: 1.0.0; Culture: he;\n  ")]
    public void Should_ignore_whitespace_around_the_list(string text)
    {
        // Act
        var result = LocaleFieldParser.Parse(text);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal("SchemaVersion", Text(result.Fields[0].NameSpan, text));
            Assert.Equal("he", Text(result.Fields[1].ValueSpan, text));
        });
    }

    [Theory]
    [InlineData("SchemaVersion: 1.0.0")]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_report_a_list_that_does_not_end_with_a_semicolon(string text)
    {
        // Act
        var result = LocaleFieldParser.Parse(text);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Fields);
            Assert.Equal(LocaleDiagnosticKind.MissingTrailingSemicolon, Assert.Single(result.Diagnostics).Kind);
        });
    }

    // Nothing constrains a standalone list's names, so an unknown one is carried rather than reported.
    [Fact]
    public void Should_accept_a_name_it_does_not_know()
    {
        // Arrange
        const string Extended = "SchemaVersion: 1.0.0; Culture: he; Author: Someone;";

        // Act
        var result = LocaleFieldParser.Parse(Extended);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal("Author", Text(result.Fields[2].NameSpan, Extended));
        });
    }

    [Fact]
    public void Should_report_a_name_written_twice()
    {
        // Act
        var result = LocaleFieldParser.Parse("SchemaVersion: 1.0.0; SchemaVersion: 2.0.0;");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.DuplicateFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_reject_null_text()
        => Assert.Throws<ArgumentNullException>(() => LocaleFieldParser.Parse(null!));

    private static string Text(LocaleTextSpan span, string text)
        => span.Slice(text).ToString();
}
