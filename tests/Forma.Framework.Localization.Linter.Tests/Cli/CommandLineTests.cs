namespace Forma.Framework.Localization.Linter.Tests.Cli;

public sealed class CommandLineTests
{
    [Fact]
    public void Should_reject_a_command_line_without_a_path()
    {
        // Act
        var parsed = CommandLine.TryParse([], out _, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(parsed);
            Assert.Equal("'--path' is required.", error);
        });
    }

    [Fact]
    public void Should_reject_a_path_switch_with_no_value()
    {
        // Act
        var parsed = CommandLine.TryParse(["--path", "--report"], out _, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(parsed);
            Assert.Equal("'--path' needs a file or directory.", error);
        });
    }

    [Fact]
    public void Should_reject_an_argument_the_linter_does_not_take()
    {
        // Act
        var parsed = CommandLine.TryParse(["--path", "x", "--verbose"], out _, out var error);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(parsed);
            Assert.Equal("'--verbose' is not an argument the linter takes.", error);
        });
    }

    [Fact]
    public void Should_ask_for_no_report_when_the_switch_is_absent()
    {
        // Act
        _ = CommandLine.TryParse(["--path", "x"], out var options, out _);

        // Assert
        Assert.Null(options!.ReportPath);
    }

    [Fact]
    public void Should_default_the_report_to_the_current_directory()
    {
        // Act
        _ = CommandLine.TryParse(["--path", "x", "--report"], out var options, out _);

        // Assert
        Assert.Equal(Path.GetFullPath("localization-report.json"), options!.ReportPath);
    }

    [Fact]
    public void Should_treat_a_json_report_value_as_the_file_to_write()
    {
        // Act
        _ = CommandLine.TryParse(["--path", "x", "--report", "out/found.json"], out var options, out _);

        // Assert
        Assert.Equal(Path.GetFullPath(Path.Combine("out", "found.json")), options!.ReportPath);
    }

    [Fact]
    public void Should_treat_any_other_report_value_as_the_directory_to_write_into()
    {
        // Act
        _ = CommandLine.TryParse(["--path", "x", "--report", "out"], out var options, out _);

        // Assert
        Assert.Equal(Path.GetFullPath(Path.Combine("out", "localization-report.json")), options!.ReportPath);
    }

    [Fact]
    public void Should_resolve_the_path_against_the_current_directory()
    {
        // Act
        _ = CommandLine.TryParse(["--path", "some/where"], out var options, out _);

        // Assert
        Assert.Equal(Path.GetFullPath(Path.Combine("some", "where")), options!.Path);
    }
}
