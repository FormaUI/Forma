namespace Forma.Framework.Localization.Abstractions.Tests.Parser;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

public sealed class LocaleExpressionParserTests
{
    private const string Files = "[$ plural {0}; one: # file; other: # files; $]";

    private static string Text(LocaleTextSpan span, string text)
        => span.Slice(text).ToString();

    [Fact]
    public void Should_return_nothing_for_text_without_a_construct()
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
    public void Should_read_the_keyword_and_the_argument_hole()
    {
        // Act
        var construct = Assert.Single(LocaleExpressionParser.Parse(Files).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("plural", Text(construct.KeywordSpan, Files));
            Assert.Equal("{0}", Text(construct.ArgumentHoleSpan, Files));
            Assert.Equal(0, construct.ArgumentIndex);
            Assert.Equal(Files, Text(construct.Span, Files));
        });
    }

    [Fact]
    public void Should_read_every_branch_in_order()
    {
        // Act
        var construct = Assert.Single(LocaleExpressionParser.Parse(Files).Expressions);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, construct.Fields.Count);
            Assert.Equal("one", Text(construct.Fields[0].NameSpan, Files));
            Assert.Equal("# file", Text(construct.Fields[0].ValueSpan, Files));
            Assert.Equal("other", Text(construct.Fields[1].NameSpan, Files));
            Assert.Equal("# files", Text(construct.Fields[1].ValueSpan, Files));
        });
    }

    [Fact]
    public void Should_read_every_construct_in_one_run_of_text()
    {
        // Arrange
        const string Pair = "[$ plural {0}; one: # file; other: # files; $] in [$ plural {1}; one: # folder; other: # folders; $]";

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
    public void Should_span_only_the_construct_within_surrounding_prose()
    {
        // Arrange
        const string Prose = "Moved to [$ plural {0}; one: # place; other: # places; $] today";

        // Act
        var construct = Assert.Single(LocaleExpressionParser.Parse(Prose).Expressions);

        // Assert
        Assert.Equal("[$ plural {0}; one: # place; other: # places; $]", Text(construct.Span, Prose));
    }

    // A semicolon only ends a branch when a category follows, so prose keeps its own punctuation.
    [Fact]
    public void Should_keep_a_semicolon_that_no_category_follows_inside_the_branch()
    {
        // Arrange
        const string Listed = "[$ plural {0}; other: # files; see the list; $]";

        // Act
        var result = LocaleExpressionParser.Parse(Listed);

        // Assert
        var construct = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal("# files; see the list", Text(Assert.Single(construct.Fields).ValueSpan, Listed));
            Assert.True(result.IsValid);
        });
    }

    [Theory]
    [InlineData("[$ plural {0}; one: # file; other: # files;")]
    [InlineData("[$ plural {0}; one: # file")]
    public void Should_report_a_construct_that_is_never_closed(string text)
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
        var result = LocaleExpressionParser.Parse("[$ plural {0}; one: # file; other: # files $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingTrailingSemicolon, Assert.Single(result.Diagnostics).Kind);
    }

    [Theory]
    [InlineData("[$ plural; one: # file; other: # files; $]", LocaleDiagnosticKind.MissingArgumentHole)]
    [InlineData("[$ plural 0; one: # file; other: # files; $]", LocaleDiagnosticKind.MissingArgumentHole)]
    [InlineData("[$ plural {a}; one: # file; other: # files; $]", LocaleDiagnosticKind.InvalidArgumentHole)]
    [InlineData("[$ plural {-1}; one: # file; other: # files; $]", LocaleDiagnosticKind.InvalidArgumentHole)]
    public void Should_report_a_header_that_does_not_carry_an_argument_hole(string text, LocaleDiagnosticKind expected)
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
    [InlineData("[$ {0}; one: # file; other: # files; $]")]
    [InlineData("[$  {0}; one: # file; other: # files; $]")]
    public void Should_report_a_header_that_carries_the_hole_alone(string text)
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
    public void Should_report_a_keyword_no_construct_defines()
    {
        // Arrange
        const string Unknown = "[$ plurals {0}; one: # file; other: # files; $]";

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

    // An unrecognised keyword brings no categories with it, so its branch names go unchecked.
    [Fact]
    public void Should_not_check_branch_names_against_categories_for_an_unknown_keyword()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ select {0}; male: his; female: her; $]");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(2, Assert.Single(result.Expressions).Fields.Count);
            Assert.Equal(LocaleDiagnosticKind.UnknownKeyword, Assert.Single(result.Diagnostics).Kind);
        });
    }

    [Fact]
    public void Should_report_a_construct_with_no_branches()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ plural {0}; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingBranches, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_the_construct_does_not_recognise()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ plural {0}; single: # file; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.UnknownFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_category_written_twice()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ plural {0}; one: # file; one: # files; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.DuplicateFieldName, Assert.Single(result.Diagnostics).Kind);
    }

    [Fact]
    public void Should_report_a_branch_with_no_colon()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ plural {0}; other # files; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.MissingNameSeparator, Assert.Single(result.Diagnostics).Kind);
    }

    // A category following the semicolon marks a separator the author meant, so the branch is recovered rather than lost.
    [Theory]
    [InlineData("[$ plural {0};one: # file; other: # files; $]")]
    [InlineData("[$ plural {0}; one: # file;other: # files; $]")]
    public void Should_recover_a_branch_whose_semicolon_carries_no_space(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var construct = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.MalformedFieldSeparator, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, construct.Fields.Count);
            Assert.Equal("# file", Text(construct.Fields[0].ValueSpan, text));
            Assert.Equal("other", Text(construct.Fields[1].NameSpan, text));
        });
    }

    [Theory]
    [InlineData("[$ plural {0}; one : # file; other : # files; $]")]
    [InlineData("[$ plural {0}; one:# file; other:# files; $]")]
    public void Should_recover_a_branch_not_written_against_its_colon(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var construct = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(2, result.Diagnostics.Count);
            Assert.All(result.Diagnostics, d => Assert.Equal(LocaleDiagnosticKind.MalformedNameSeparator, d.Kind));
            Assert.Equal("one", Text(construct.Fields[0].NameSpan, text));
            Assert.Equal("# file", Text(construct.Fields[0].ValueSpan, text));
            Assert.Equal("# files", Text(construct.Fields[1].ValueSpan, text));
        });
    }

    // Spacing cannot make an absent value present, so the shape of the separator is not what gets reported.
    [Theory]
    [InlineData("[$ plural {0}; one: ; other: # files; $]")]
    [InlineData("[$ plural {0}; one:; other: # files; $]")]
    [InlineData("[$ plural {0}; one:    ; other: # files; $]")]
    public void Should_report_a_branch_that_carries_no_value(string text)
    {
        // Act
        var result = LocaleExpressionParser.Parse(text);

        // Assert
        var construct = Assert.Single(result.Expressions);
        Assert.Multiple(() =>
        {
            Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
            Assert.Equal(2, construct.Fields.Count);
            Assert.True(construct.Fields[0].ValueSpan.IsEmpty);
            Assert.Equal("# files", Text(construct.Fields[1].ValueSpan, text));
        });
    }

    [Fact]
    public void Should_report_the_last_branch_carrying_no_value()
    {
        // Act
        var result = LocaleExpressionParser.Parse("[$ plural {0}; one: # file; other: ; $]");

        // Assert
        Assert.Equal(LocaleDiagnosticKind.EmptyFieldValue, Assert.Single(result.Diagnostics).Kind);
    }

    // Without categories to check a name against, a typo cannot be told from prose, so the exact separator stays required.
    [Fact]
    public void Should_not_recover_a_separator_for_a_keyword_that_brings_no_names()
    {
        // Arrange
        const string Unspaced = "[$ select {0}; male: his;female: her; $]";

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
}
