namespace Forma.Framework.Localization.Tests.Parser;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

public sealed class LocaleExpressionParserTests
{
    private const string Files = "[$ [count] plural; one: # file; other: # files; $]";

    private static readonly LocaleKeywords Keywords =
        new(new LocaleKeyword("plural", "zero", "one", "two", "few", "many", "other"));

    private static string GetText(LocaleTextSpan span, string text)
        => span.Slice(text).ToString();

    [Fact]
    public void Should_return_nothing_for_text_without_a_expression()
    {
        // Act
        var result = LocaleExpressionParser.Parse("Stored {0} of {1}.", Keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void Should_read_the_keyword_and_the_argument_name()
    {
        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Files, Keywords).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("plural", GetText(expression.KeywordSpan, Files));
            Assert.Equal("[count]", GetText(expression.ArgumentSpan, Files));
            Assert.Equal("count", GetText(expression.NameSpan, Files));
            Assert.Equal(Files, GetText(expression.Span, Files));
        });
    }

    [Fact]
    public void Should_read_an_expression_that_names_no_argument()
    {
        // Arrange
        const string Bare = "[$ plural; one: # file; other: # files; $]";

        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Bare, Keywords).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("plural", GetText(expression.KeywordSpan, Bare));
            Assert.True(expression.NameSpan.IsEmpty);
            Assert.Equal(2, expression.Fields.Count);
        });
    }

    [Fact]
    public void Should_read_every_field_in_order()
    {
        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Files, Keywords).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, expression.Fields.Count);
            Assert.Equal("one", GetText(expression.Fields[0].NameSpan, Files));
            Assert.Equal("# file", GetText(expression.Fields[0].ValueSpan, Files));
            Assert.Equal("other", GetText(expression.Fields[1].NameSpan, Files));
            Assert.Equal("# files", GetText(expression.Fields[1].ValueSpan, Files));
        });
    }

    [Fact]
    public void Should_read_every_expression_in_one_run_of_text()
    {
        // Arrange
        const string Pair = "[$ [files] plural; one: # file; other: # files; $] in [$ [folders] plural; one: # folder; other: # folders; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Pair, Keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, result.Expressions.Count);
            Assert.Equal("files", GetText(result.Expressions[0].NameSpan, Pair));
            Assert.Equal("folders", GetText(result.Expressions[1].NameSpan, Pair));
            Assert.True(result.IsValid);
        });
    }

    [Fact]
    public void Should_span_only_the_expression_within_surrounding_prose()
    {
        // Arrange
        const string Prose = "Moved to [$ [count] plural; one: # place; other: # places; $] today";

        // Act
        var expression = Assert.Single(LocaleExpressionParser.Parse(Prose, Keywords).Expressions);

        // Assert
        Assert.Equal("[$ [count] plural; one: # place; other: # places; $]", GetText(expression.Span, Prose));
    }

    // A semicolon ends a field only where a category follows it.
    [Fact]
    public void Should_keep_a_semicolon_that_no_category_follows_inside_the_field()
    {
        // Arrange
        const string Listed = "[$ [count] plural; other: # files; see the list; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Listed, Keywords);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal("# files; see the list", GetText(Assert.Single(expression.Fields).ValueSpan, Listed));
            Assert.True(result.IsValid);
        });
    }

    [Theory]
    [InlineData("[$ [count] plural; one: # file; other: # files;")]
    [InlineData("[$ [count] plural; one: # file")]
    public void Should_report_a_expression_that_is_never_closed(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

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
        var result = LocaleExpressionParser.Parse("[$ [count] plural; one: # file; other: # files $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingTrailingSemicolon, Assert.Single(result.Diagnostics).Kind);
    }

    [Theory]
    [InlineData("[$ [0] plural; one: # file; other: # files; $]")]
    [InlineData("[$ [a b] plural; one: # file; other: # files; $]")]
    [InlineData("[$ [-1] plural; one: # file; other: # files; $]")]
    [InlineData("[$ [] plural; one: # file; other: # files; $]")]
    public void Should_report_a_bracketed_name_that_is_not_an_identifier(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Empty(result.Expressions);
            Assert.Equal(LocaleDiagnosticKind.InvalidArgumentName, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_report_a_bracket_that_is_never_closed()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count plural; one: # file; other: # files; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingArgumentName, Assert.Single(result.Diagnostics).Kind);
    }

    [Theory]
    [InlineData("[$ [count]; one: # file; other: # files; $]")]
    [InlineData("[$ [count] ; one: # file; other: # files; $]")]
    public void Should_report_a_header_that_carries_the_name_alone(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

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
        const string Unknown = "[$ [count] plurals; one: # file; other: # files; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Unknown, Keywords);

        // Assert
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, diagnostic.Kind);
            Assert.Equal("plurals", GetText(diagnostic.Span, Unknown));
        });
    }

    [Fact]
    public void Should_not_check_field_names_against_categories_for_an_unknown_keyword()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] select; male: his; female: her; $]", Keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, Assert.Single(result.Expressions).Fields.Count);
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_read_a_keyword_the_caller_defines()
    {
        // Arrange
        var keywords = new LocaleKeywords(new LocaleKeyword("gender", "male", "female", "other"));

        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] gender; male: his; female: her; $]", keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, Assert.Single(result.Expressions).Fields.Count);
            Assert.Empty(result.Diagnostics);
        });
    }

    [Fact]
    public void Should_report_a_field_name_the_caller_s_keyword_does_not_define()
    {
        // Arrange
        var keywords = new LocaleKeywords(new LocaleKeyword("gender", "male", "female", "other"));

        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] gender; neuter: theirs; $]", keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.UnknownFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_accept_any_field_name_for_a_keyword_that_defines_none()
    {
        // Arrange
        var keywords = new LocaleKeywords(new LocaleKeyword("select"));

        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] select; anything: here; $]", keywords);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Single(Assert.Single(result.Expressions).Fields);
            Assert.Empty(result.Diagnostics);
        });
    }

    [Fact]
    public void Should_reject_a_field_name_repeated_within_a_keyword()
        => Assert.Throws<ArgumentException>(() => new LocaleKeyword("gender", "male", "male"));

    [Fact]
    public void Should_reject_a_blank_field_name()
        => Assert.Throws<ArgumentException>(() => new LocaleKeyword("gender", "male", " "));

    [Fact]
    public void Should_reject_a_keyword_name_repeated_within_a_set()
        => Assert.Throws<ArgumentException>(() => new LocaleKeywords(new LocaleKeyword("plural"), new LocaleKeyword("plural")));

    [Fact]
    public void Should_reject_a_null_keyword_in_a_set()
        => Assert.Throws<ArgumentNullException>(() => new LocaleKeywords(null!));

    [Fact]
    public void Should_report_a_expression_with_no_fields()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] plural; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingFields, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_the_expression_does_not_recognise()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] plural; single: # file; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.UnknownFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_written_twice()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] plural; one: # file; one: # files; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.DuplicateFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_field_with_no_colon()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] plural; other # files; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingNameSeparator, Assert.Single(result.Diagnostics).Kind);
    }

    // A category after the semicolon marks it as a separator.
    [Theory]
    [InlineData("[$ [count] plural;one: # file; other: # files; $]")]
    [InlineData("[$ [count] plural; one: # file;other: # files; $]")]
    public void Should_recover_a_field_whose_semicolon_carries_no_space(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.MalformedFieldSeparator, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, expression.Fields.Count);
            Assert.Equal("# file", GetText(expression.Fields[0].ValueSpan, text));
            Assert.Equal("other", GetText(expression.Fields[1].NameSpan, text));
        });
    }

    [Theory]
    [InlineData("[$ [count] plural; one : # file; other : # files; $]")]
    [InlineData("[$ [count] plural; one:# file; other:# files; $]")]
    public void Should_recover_a_field_not_written_against_its_colon(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(2, result.Diagnostics.Count);
            Assert.All(result.Diagnostics, d => Assert.Equal(LocaleDiagnosticKind.MalformedNameSeparator, d.Kind));
            Assert.Equal("one", GetText(expression.Fields[0].NameSpan, text));
            Assert.Equal("# file", GetText(expression.Fields[0].ValueSpan, text));
            Assert.Equal("# files", GetText(expression.Fields[1].ValueSpan, text));
        });
    }

    [Theory]
    [InlineData("[$ [count] plural; one: ; other: # files; $]")]
    [InlineData("[$ [count] plural; one:; other: # files; $]")]
    [InlineData("[$ [count] plural; one:    ; other: # files; $]")]
    public void Should_report_a_field_that_carries_no_value(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text, Keywords);

        // Assert
        var expression = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, expression.Fields.Count);
            Assert.True(expression.Fields[0].ValueSpan.IsEmpty);
            Assert.Equal("# files", GetText(expression.Fields[1].ValueSpan, text));
        });
    }

    [Fact]
    public void Should_report_the_last_field_carrying_no_value()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ [count] plural; one: # file; other: ; $]", Keywords);

        // Assert
        Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_not_recover_a_separator_for_a_keyword_that_brings_no_names()
    {
        // Arrange
        const string Unspaced = "[$ [count] select; male: his;female: her; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Unspaced, Keywords);

        // Assert
        var field = Assert.Single(Assert.Single(result.Expressions).Fields);
        Assert.Multiple(() =>
        {
            Assert.Equal("male", GetText(field.NameSpan, Unspaced));
            Assert.Equal("his;female: her", GetText(field.ValueSpan, Unspaced));
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_reject_null_text()
        => Assert.Throws<ArgumentNullException>(() => LocaleExpressionParser.Parse(null!, Keywords));

    [Fact]
    public void Should_rewrite_an_interpolated_name_into_a_plain_one()
    {
        // Act
        var rewritten = LocaleExpressionParser.TryNormalize("[$ [{count}] plural; one: # file; other: # files; $]", out var normalized);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(rewritten);
            Assert.Equal(Files, normalized);
        });
    }

    // Only an expression's own header is rewritten.
    [Theory]
    [InlineData("See [{count}] for details")]
    [InlineData("[$ [{count} plural; one: # file; other: # files; $]")]
    [InlineData("[$ [{count} plural; one: a}]")]
    [InlineData("[$ [{} plural; one: a; $]")]
    [InlineData("[$ [{0}] plural; one: a; $]")]
    [InlineData("[$ [{count}")]
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
        LocaleExpressionParser.TryNormalize("See [{files}] and [$ [{count}] plural; one: a; other: b; $] now", out var normalized);

        // Assert
        Assert.Equal("See [{files}] and [$ [count] plural; one: a; other: b; $] now", normalized);
    }

    [Theory]
    [InlineData("[$ [count] plural; one: a; other: b; $]")]
    [InlineData("Stored {stored} of {total}.")]
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
