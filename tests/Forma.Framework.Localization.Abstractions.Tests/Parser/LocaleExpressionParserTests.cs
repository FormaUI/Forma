namespace Forma.Framework.Localization.Abstractions.Tests.Parser;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

public sealed class LocaleExpressionParserTests
{
    private const string Files = "[$ [0] plural; one: # file; other: # files; $]";

    private static string Text(LocaleTextSpan span, string text)
        => span.Slice(text).ToString();

    [Fact]
    public void Should_return_nothing_for_text_without_a_expression()
    {
        // Act
        var result = LocaleExpressionParser.Parse("Stored {0} of {1}.");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void Should_read_the_keyword_and_the_argument_index()
    {
        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Files).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("plural", Text(expression.KeywordSpan, Files));
            Assert.Equal("[0]", Text(expression.ArgumentSpan, Files));
            Assert.Equal(0, expression.ArgumentIndex);
            Assert.Equal(Files, Text(expression.Span, Files));
        });
    }

    [Fact]
    public void Should_read_every_field_in_order()
    {
        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Files).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, expression.Fields.Count);
            Assert.Equal("one", Text(expression.Fields[0].NameSpan, Files));
            Assert.Equal("# file", Text(expression.Fields[0].ValueSpan, Files));
            Assert.Equal("other", Text(expression.Fields[1].NameSpan, Files));
            Assert.Equal("# files", Text(expression.Fields[1].ValueSpan, Files));
        });
    }

    [Fact]
    public void Should_read_every_expression_in_one_run_of_text()
    {
        // Arrange
        const string Pair = "[$ [0] plural; one: # file; other: # files; $] in [$ [1] plural; one: # folder; other: # folders; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Pair);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, result.Expressions.Count);
            Assert.Equal(0, result.Expressions[0].ArgumentIndex);
            Assert.Equal(1, result.Expressions[1].ArgumentIndex);
            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void Should_span_only_the_expression_within_surrounding_prose()
    {
        // Arrange
        const string Prose = "Moved to [$ [0] plural; one: # place; other: # places; $] today";

        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Prose).Expressions);

        // Assert
        Assert.Equal("[$ [0] plural; one: # place; other: # places; $]", Text(expression.Span, Prose));
    }

    // A semicolon only ends a field when a category follows, so prose keeps its own punctuation.
    [Fact]
    public void Should_keep_a_semicolon_that_no_category_follows_inside_the_field()
    {
        // Arrange
        const string Listed = "[$ [0] plural; other: # files; see the list; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Listed);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal("# files; see the list", Text(Assert.Single(expression.Fields).ValueSpan, Listed));
            Assert.True(result.IsValid);
        });
    }

    [Theory]
    [InlineData("[$ [0] plural; one: # file; other: # files;")]
    [InlineData("[$ [0] plural; one: # file")]
    public void Should_report_a_expression_that_is_never_closed(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.Equal(LocaleDiagnosticKind.UnclosedExpression, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_report_a_body_that_does_not_end_with_a_semicolon()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; one: # file; other: # files $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingTrailingSemicolon, Assert.Single(result.Diagnostics).Kind);
    }

    [Theory]
    [InlineData("[$ plural; one: # file; other: # files; $]", LocaleDiagnosticKind.MissingArgumentIndex)]
    [InlineData("[$ 0 plural; one: # file; other: # files; $]", LocaleDiagnosticKind.MissingArgumentIndex)]
    [InlineData("[$ [a] plural; one: # file; other: # files; $]", LocaleDiagnosticKind.InvalidArgumentIndex)]
    [InlineData("[$ [-1] plural; one: # file; other: # files; $]", LocaleDiagnosticKind.InvalidArgumentIndex)]
    [InlineData("[$ [] plural; one: # file; other: # files; $]", LocaleDiagnosticKind.InvalidArgumentIndex)]
    public void Should_report_a_header_that_does_not_carry_an_argument_index(string text, LocaleDiagnosticKind expected)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.Equal(expected, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Theory]
    [InlineData("[$ [0]; one: # file; other: # files; $]")]
    [InlineData("[$ [0] ; one: # file; other: # files; $]")]
    public void Should_report_a_header_that_carries_the_index_alone(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.Equal(LocaleDiagnosticKind.MissingKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_report_a_keyword_no_expression_defines()
    {
        // Arrange
        const string Unknown = "[$ [0] plurals; one: # file; other: # files; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Unknown);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, diagnostic.Kind);
            Assert.Equal("plurals", Text(diagnostic.Span, Unknown));
        });
    }

    // An unrecognised keyword brings no categories with it, so its field names go unchecked.
    [Fact]
    public void Should_not_check_field_names_against_categories_for_an_unknown_keyword()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] select; male: his; female: her; $]");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, Assert.Single(result.Expressions).Fields.Count);
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_report_a_expression_with_no_fields()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingFields, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_the_expression_does_not_recognise()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; single: # file; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.UnknownFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_written_twice()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; one: # file; one: # files; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.DuplicateFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_field_with_no_colon()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; other # files; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingNameSeparator, Assert.Single(result.Diagnostics).Kind);
    }

    // A category following the semicolon marks a separator the author meant, so the field is recovered rather than lost.
    [Theory]
    [InlineData("[$ [0] plural;one: # file; other: # files; $]")]
    [InlineData("[$ [0] plural; one: # file;other: # files; $]")]
    public void Should_recover_a_field_whose_semicolon_carries_no_space(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.MalformedFieldSeparator, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, expression.Fields.Count);
            Assert.Equal("# file", Text(expression.Fields[0].ValueSpan, text));
            Assert.Equal("other", Text(expression.Fields[1].NameSpan, text));
        });
    }

    [Theory]
    [InlineData("[$ [0] plural; one : # file; other : # files; $]")]
    [InlineData("[$ [0] plural; one:# file; other:# files; $]")]
    public void Should_recover_a_field_not_written_against_its_colon(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(2, result.Diagnostics.Count);
            Assert.All(result.Diagnostics, d => Assert.Equal(LocaleDiagnosticKind.MalformedNameSeparator, d.Kind));
            Assert.Equal("one", Text(expression.Fields[0].NameSpan, text));
            Assert.Equal("# file", Text(expression.Fields[0].ValueSpan, text));
            Assert.Equal("# files", Text(expression.Fields[1].ValueSpan, text));
        });
    }

    // Spacing cannot make an absent value present, so the shape of the separator is not what gets reported.
    [Theory]
    [InlineData("[$ [0] plural; one: ; other: # files; $]")]
    [InlineData("[$ [0] plural; one:; other: # files; $]")]
    [InlineData("[$ [0] plural; one:    ; other: # files; $]")]
    public void Should_report_a_field_that_carries_no_value(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, expression.Fields.Count);
            Assert.True(expression.Fields[0].ValueSpan.IsEmpty);
            Assert.Equal("# files", Text(expression.Fields[1].ValueSpan, text));
        });
    }

    [Fact]
    public void Should_report_the_last_field_carrying_no_value()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [0] plural; one: # file; other: ; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
    }

    // Without categories to check a name against, a typo cannot be told from prose, so the exact separator stays required.
    [Fact]
    public void Should_not_recover_a_separator_for_a_keyword_that_brings_no_names()
    {
        // Arrange
        const string Unspaced = "[$ [0] select; male: his;female: her; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Unspaced);

        // Assert
        var field = Assert.Single(Assert.Single(result.Expressions).Fields);
        Assert.Multiple(() =>
        {
            Assert.Equal("male", Text(field.NameSpan, Unspaced));
            Assert.Equal("his;female: her", Text(field.ValueSpan, Unspaced));
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_reject_null_text()
        => Assert.Throws<ArgumentNullException>(() => LocaleExpressionParser.Parse(null!));

    [Fact]
    public void Should_rewrite_an_interpolated_index_into_a_plain_one()
    {
        // Act
        var rewritten = LocaleExpressionParser.TryNormalize("[$ [{0}] plural; one: # file; other: # files; $]", out var normalized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(rewritten);
            Assert.Equal(Files, normalized);
        });
    }

    // Only an expression's own header is rewritten, so a hole the caller means to fill survives.
    [Theory]
    [InlineData("See [{0}] for details")]
    [InlineData("[$ [{0} plural; one: # file; other: # files; $]")]
    [InlineData("[$ [{0} plural; one: a}]")]
    [InlineData("[$ [{} plural; one: a; $]")]
    [InlineData("[$ [{x}] plural; one: a; $]")]
    [InlineData("[$ [{0}")]
    public void Should_leave_brackets_that_do_not_open_a_header_alone(string text)
    {
        // Act
        var rewritten = LocaleExpressionParser.TryNormalize(text, out var normalized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(rewritten);
            Assert.Equal(text, normalized);
        });
    }

    [Fact]
    public void Should_rewrite_the_header_while_prose_around_it_keeps_its_holes()
    {
        // Act
        LocaleExpressionParser.TryNormalize("See [{0}] and [$ [{1}] plural; one: a; other: b; $] now", out var normalized);

        // Assert
        Assert.Equal("See [{0}] and [$ [1] plural; one: a; other: b; $] now", normalized);
    }

    // Canonical text needs no rewriting, so nothing is allocated for it.
    [Theory]
    [InlineData("[$ [0] plural; one: a; other: b; $]")]
    [InlineData("Stored {0} of {1}.")]
    [InlineData("")]
    public void Should_report_canonical_text_as_needing_no_rewrite(string text)
    {
        // Act
        var rewritten = LocaleExpressionParser.TryNormalize(text, out var normalized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(rewritten);
            Assert.Null(normalized);
        });
    }
}
