namespace Forma.Framework.Localization.Linter.Tests.Rules.Sources;

public sealed class CallSiteRuleTests
{
    [Fact]
    public void Should_report_fewer_values_than_the_key_names_holes()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["Stored {stored} of {total}.", 1];""");

        // Assert
        Assert.Equal([LintCodes.ValueCountMismatch], diagnostics.Codes());
    }

    [Fact]
    public void Should_report_more_values_than_a_key_naming_no_hole_takes()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint("""public string Read() => locale["Saved.", 1, 2];""");

        // Assert
        Assert.Equal([LintCodes.ValueCountMismatch], diagnostics.Codes());
    }

    [Fact]
    public void Should_accept_one_value_for_each_hole_the_key_names()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["Stored {stored} of {total}.", 1, 2];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_count_a_repeated_hole_once()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["{name} and {name}", 1];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_an_interpolated_hole_that_is_not_a_plain_identifier()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { var items = new[] { 1 }; return locale[$"Stored {items.Length}"]; }""");

        // Assert
        Assert.Equal([LintCodes.HoleNotIdentifier], diagnostics.Codes());
    }

    [Fact]
    public void Should_accept_an_interpolated_hole_that_is_a_plain_identifier()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { var count = 1; return locale[$"Stored {count}"]; }""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_an_expression_reading_a_name_the_key_names_no_hole_for()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["[$ [count] plural; one: # file; other: # files; $]"];""");

        // Assert
        Assert.Equal([LintCodes.UnsuppliedPluralArgument], diagnostics.Codes());
    }

    [Fact]
    public void Should_accept_an_expression_reading_a_hole_the_key_names()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["{count} [$ [count] plural; one: file; other: files; $]", 1];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_an_expression_naming_no_argument_when_no_value_is_supplied()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["[$ plural; one: # file; other: # files; $]"];""");

        // Assert
        Assert.Equal([LintCodes.SoleArgumentExpected], diagnostics.Codes());
    }

    [Fact]
    public void Should_accept_an_expression_naming_no_argument_when_one_value_is_supplied()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["[$ plural; one: # file; other: # files; $]", 1];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_report_a_plural_expression_that_omits_the_fallback_category()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["{count} [$ [count] plural; one: file; $]", 1];""");

        // Assert
        Assert.Equal([LintCodes.MissingPluralOther], diagnostics.Codes());
    }

    [Fact]
    public void Should_report_a_malformed_expression_in_an_interpolated_key()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { var count = 1; return locale[$"{count} [$ [count] plural one: file; $]"]; }""");

        // Assert
        Assert.Equal([LintCodes.MalformedSourceExpression], diagnostics.Codes());
    }

    [Fact]
    public void Should_read_an_interpolated_key_as_the_text_the_handler_rebuilds()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { var count = 1; return locale[$"{count} [$ [count] plural; one: file; $]"]; }""");

        // Assert
        Assert.Equal([LintCodes.MissingPluralOther], diagnostics.Codes());
    }

    [Fact]
    public void Should_treat_doubled_braces_in_an_interpolated_key_as_literal_text()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint("""public string Read() => locale[$"Use {{curly}} braces"];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_leave_a_lookup_that_names_only_a_key_alone()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint("""public string Read() => locale["Saved."];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_not_count_values_that_carry_their_own_names()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() => locale["Stored {stored} of {total}.", ("stored", (object?)1)];""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_not_count_a_values_collection_passed_whole_as_one_value()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { object?[] values = [1, 2]; return locale["Stored {stored} of {total}.", values]; }""");

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Should_still_count_a_value_that_is_an_array_of_another_element_type()
    {
        // Act
        var diagnostics = SourceLinterTestFactory.Lint(
            """public string Read() { int[] values = [1, 2]; return locale["Stored {stored} of {total}.", values]; }""");

        // Assert
        Assert.Equal([LintCodes.ValueCountMismatch], diagnostics.Codes());
    }
}
