namespace Forma.Framework.Localization.Tests.Parser;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

public sealed class LocalePathParserTests
{
    [Fact]
    public void Should_read_literal_runs_and_tokens_in_order()
    {
        // Arrange
        const string Path = "$RootDir/$AnchorName/.locales/$Culture$Extension";

        // Act
        var result = LocalePathParser.Parse(Path);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal(
                [
                    new LocalePathPart(LocalePathToken.RootDir, string.Empty, new LocaleTextSpan(0, 8)),
                    new LocalePathPart(null, "/", new LocaleTextSpan(8, 1)),
                    new LocalePathPart(LocalePathToken.AnchorName, string.Empty, new LocaleTextSpan(9, 11)),
                    new LocalePathPart(null, "/.locales/", new LocaleTextSpan(20, 10)),
                    new LocalePathPart(LocalePathToken.Culture, string.Empty, new LocaleTextSpan(30, 8)),
                    new LocalePathPart(LocalePathToken.Extension, string.Empty, new LocaleTextSpan(38, 10)),
                ],
                result.Parts);
        });
    }

    [Fact]
    public void Should_read_a_doubled_marker_as_a_literal_dollar()
    {
        // Act
        var result = LocalePathParser.Parse("$$shared/$AnchorName.$Culture$Extension");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal(new LocalePathPart(null, "$shared/", new LocaleTextSpan(0, 9)), result.Parts[0]);
        });
    }

    [Fact]
    public void Should_read_every_token_a_path_defines()
    {
        // Act
        var result = LocalePathParser.Parse("$AssemblyName/$RootNamespace/$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.True(result.IsValid);
            Assert.Equal(
                [
                    LocalePathToken.AssemblyName,
                    LocalePathToken.RootNamespace,
                    LocalePathToken.RootDir,
                    LocalePathToken.ParentName,
                    LocalePathToken.LocalesDir,
                    LocalePathToken.CurrentName,
                    LocalePathToken.Culture,
                    LocalePathToken.Extension,
                ],
                result.Parts.Where(part => part.IsToken).Select(part => part.Token!.Value));
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_report_an_empty_path(string path)
    {
        // Act
        var result = LocalePathParser.Parse(path);

        // Assert
        Assert.Equal([LocaleDiagnosticKind.EmptyPath], result.Diagnostics.Select(diagnostic => diagnostic.Kind));
    }

    [Theory]
    [InlineData("$AnchorName/$Folder.$Culture$Extension", 12, 7)]
    [InlineData("$AnchorName/$anchorname.$Culture$Extension", 12, 11)]
    [InlineData("$AnchorName/$.$Culture$Extension", 12, 1)]
    [InlineData("$AnchorName/$1.$Culture$Extension", 12, 1)]
    public void Should_report_a_token_no_path_defines_where_it_is_written(string path, int start, int length)
    {
        // Act
        var result = LocalePathParser.Parse(path);

        // Assert
        Assert.Equal(new LocaleDiagnostic(LocaleDiagnosticKind.UnknownPathToken, new LocaleTextSpan(start, length)), Assert.Single(result.Diagnostics));
    }

    [Fact]
    public void Should_report_a_token_named_twice_where_it_is_named_again()
    {
        // Act
        var result = LocalePathParser.Parse("$AnchorName/$Culture/$AnchorName.$Culture$Extension");

        // Assert
        Assert.Equal(
            [
                new LocaleDiagnostic(LocaleDiagnosticKind.DuplicatePathToken, new LocaleTextSpan(21, 11)),
                new LocaleDiagnostic(LocaleDiagnosticKind.DuplicatePathToken, new LocaleTextSpan(33, 8)),
            ],
            result.Diagnostics);
    }

    [Theory]
    [InlineData("$RootDir/$AnchorName.json")]
    [InlineData("$RootDir/$AnchorName.$Culture.json")]
    [InlineData("$RootDir/$AnchorName$Extension")]
    public void Should_report_a_path_that_does_not_name_its_culture_and_extension(string path)
    {
        // Act
        var result = LocalePathParser.Parse(path);

        // Assert
        Assert.Equal(
            new LocaleDiagnostic(LocaleDiagnosticKind.MissingPathToken, new LocaleTextSpan(0, path.Length)),
            Assert.Single(result.Diagnostics));
    }

    [Theory]
    [InlineData("$RootDir/$CurrentName.$Culture$Extension")]
    [InlineData("$RootDir/$ParentName/$Culture$Extension")]
    public void Should_report_a_path_that_spells_part_of_an_anchor(string path)
    {
        // Act
        var result = LocalePathParser.Parse(path);

        // Assert
        Assert.Equal(
            new LocaleDiagnostic(LocaleDiagnosticKind.IncompletePathAnchor, new LocaleTextSpan(0, path.Length)),
            Assert.Single(result.Diagnostics));
    }

    [Fact]
    public void Should_report_a_path_that_spells_its_anchor_twice()
    {
        // Arrange
        const string Path = "$RootDir/$ParentName/$AnchorName.$Culture$Extension";

        // Act
        var result = LocalePathParser.Parse(Path);

        // Assert
        Assert.Equal(
            new LocaleDiagnostic(LocaleDiagnosticKind.ConflictingPathAnchor, new LocaleTextSpan(0, Path.Length)),
            Assert.Single(result.Diagnostics));
    }

    [Theory]
    [InlineData("$RootDir/$AnchorName.$Culture$Extension")]
    [InlineData("$RootDir/$AnchorName/$CurrentName.$Culture$Extension")]
    [InlineData("$RootDir/$AssemblyName.$Culture$Extension")]
    [InlineData("$RootDir/$ParentName/$LocalesDir/$CurrentName.$Culture$Extension")]
    [InlineData("$RootDir/$LocalesDir/$Culture$Extension")]
    [InlineData("$CurrentName/$AnchorName.$Culture$Extension")]
    public void Should_accept_a_path_that_spells_its_whole_anchor_or_none_of_it(string path)
        => Assert.True(LocalePathParser.Parse(path).IsValid);
}
