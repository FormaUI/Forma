namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocalizedFormatHandlerTests
{
    [Fact]
    public void Should_rebuild_the_key_with_named_holes()
    {
        // Arrange
        var stored = 3;
        var total = 4;

        // Act
        var (key, arguments) = Capture($"Stored {stored} of {total}.");

        // Assert
        Assert.Equal("Stored {stored} of {total}.", key);
        Assert.Equal([new LocalizedArgument("stored", 3), new LocalizedArgument("total", 4)], arguments);
    }

    [Fact]
    public void Should_reject_a_hole_that_is_not_a_plain_identifier()
    {
        // Arrange
        var model = new { Total = 5 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Capture($"Stored {model.Total}."));
    }

    [Fact]
    public void Should_keep_a_literal_without_holes_as_the_key()
    {
        // Act
        var (key, arguments) = Capture($"Save");

        // Assert
        Assert.Equal("Save", key);
        Assert.Empty(arguments);
    }

    [Fact]
    public void Should_double_a_brace_so_the_key_survives_formatting()
    {
        // Arrange
        var count = 3;

        // Act
        var (key, _) = Capture($"Use {{curly}} braces {count}.");

        // Assert
        Assert.Equal("Use {{curly}} braces {count}.", key);
        Assert.Equal(
            "Use {curly} braces 3.",
            LocalizedFormat.Render(key, [new LocalizedArgument("count", 3)], CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Should_keep_the_specifier_in_the_key_and_the_value_unrendered()
    {
        // Arrange
        var share = 0.4567;

        // Act
        var (key, arguments) = Capture($"Ratio {share:P1}.");

        // Assert
        Assert.Equal("Ratio {share:P1}.", key);
        Assert.Equal(0.4567, arguments[0].Value);
    }

    [Fact]
    public void Should_keep_the_alignment_in_the_key_and_the_value_unpadded()
    {
        // Arrange
        var name = "x";

        // Act
        var (key, arguments) = Capture($"{name,-3}|");

        // Assert
        Assert.Equal("{name,-3}|", key);
        Assert.Equal("x", arguments[0].Value);
    }

    [Fact]
    public void Should_repeat_the_same_name_for_a_value_used_more_than_once()
    {
        // Arrange
        var value = 1;

        // Act
        var (key, arguments) = Capture($"{value}{value}{value}");

        // Assert
        Assert.Equal("{value}{value}{value}", key);
        Assert.Equal(3, arguments.Length);
    }

    [Fact]
    public void Should_capture_a_null_hole_value()
    {
        // Arrange
        string? missing = null;

        // Act
        var (key, arguments) = Capture($"Name {missing}.");

        // Assert
        Assert.Equal("Name {missing}.", key);
        Assert.Null(arguments[0].Value);
    }

    [Fact]
    public void Should_grow_its_buffer_when_doubling_braces_outruns_the_estimate()
    {
        // Arrange
        var handler = new LocalizedFormatHandler(literalLength: 100, formattedCount: 0);

        // Act
        handler.AppendLiteral(new string('{', 100));
        var key = handler.Key.ToString();
        handler.Clear();

        // Assert
        Assert.Equal(new string('{', 200), key);
    }

    [Fact]
    public void Should_leave_the_key_empty_once_cleared()
    {
        // Arrange
        var handler = new LocalizedFormatHandler(literalLength: 8, formattedCount: 0);
        handler.AppendLiteral("Save");

        // Act
        handler.Clear();

        // Assert
        Assert.True(handler.Key.IsEmpty);
    }

    [Fact]
    public void Should_report_no_arguments_once_cleared()
    {
        // Arrange
        var stored = 3;
        var handler = new LocalizedFormatHandler(literalLength: 8, formattedCount: 1);
        handler.AppendFormatted(stored);

        // Act
        handler.Clear();

        // Assert
        Assert.Empty(handler.GetArguments());
    }

    [Fact]
    public void Should_keep_arguments_already_read_valid_after_clearing()
    {
        // Arrange
        var stored = 3;
        var total = 4;
        var handler = new LocalizedFormatHandler(literalLength: 8, formattedCount: 2);
        handler.AppendFormatted(stored);
        handler.AppendFormatted(total);

        // Act
        var arguments = handler.GetArguments();
        handler.Clear();

        // Assert
        Assert.Equal([new LocalizedArgument("stored", 3), new LocalizedArgument("total", 4)], arguments);
    }

    private static (string Key, LocalizedArgument[] Arguments) Capture(LocalizedFormatHandler handler)
    {
        var key = handler.Key.ToString();
        var arguments = handler.GetArguments();
        handler.Clear();

        return (key, arguments);
    }
}
