namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class MessageSegmentTests
{
    [Fact]
    public void Should_split_a_format_into_literals_and_holes()
    {
        // Act
        var segments = Collect("Stored {0} of {1}.");

        // Assert
        Assert.Equal([("Stored ", -1), ("", 0), (" of ", -1), ("", 1), (".", -1)], segments);
    }

    [Fact]
    public void Should_treat_a_format_without_holes_as_one_literal()
    {
        // Act
        var segments = Collect("Save");

        // Assert
        Assert.Equal([("Save", -1)], segments);
    }

    [Fact]
    public void Should_yield_a_doubled_brace_as_a_single_literal_brace()
    {
        // Act
        var segments = Collect("Use {{curly}} braces {0}.");

        // Assert
        Assert.Equal(
            [("Use ", -1), ("{", -1), ("curly", -1), ("}", -1), (" braces ", -1), ("", 0), (".", -1)],
            segments);
    }

    [Fact]
    public void Should_read_a_hole_wrapped_in_escaped_braces()
    {
        // Act
        var segments = Collect("{{{0:D}}}");

        // Assert
        Assert.Equal([("{", -1), ("", 0), ("}", -1)], segments);
    }

    [Fact]
    public void Should_report_the_same_index_each_time_a_format_repeats_it()
    {
        // Act
        var segments = Collect("0x{0:X} {0:E} {0:N}");

        // Assert
        Assert.Equal([("0x", -1), ("", 0), (" ", -1), ("", 0), (" ", -1), ("", 0)], segments);
    }

    [Fact]
    public void Should_keep_a_hole_that_carries_a_format_specifier()
    {
        // Act
        var segments = Collect("Ratio {0:P1}.");

        // Assert
        Assert.Equal([("Ratio ", -1), ("", 0), (".", -1)], segments);
    }

    [Fact]
    public void Should_keep_a_hole_that_carries_an_alignment()
    {
        // Act
        var segments = Collect("{0,-8}|");

        // Assert
        Assert.Equal([("", 0), ("|", -1)], segments);
    }

    [Fact]
    public void Should_treat_a_non_numeric_brace_group_as_literal_text()
    {
        // Act
        var segments = Collect("A {name} here");

        // Assert
        Assert.Equal([("A ", -1), ("{name}", -1), (" here", -1)], segments);
    }

    [Fact]
    public void Should_treat_an_unclosed_brace_as_literal_text()
    {
        // Act
        var segments = Collect("Broken {0 here");

        // Assert
        Assert.Equal([("Broken ", -1), ("{0 here", -1)], segments);
    }

    [Fact]
    public void Should_treat_a_stray_closing_brace_as_literal_text()
    {
        // Act
        var segments = Collect("Closed } here");

        // Assert
        Assert.Equal([("Closed ", -1), ("}", -1), (" here", -1)], segments);
    }

    [Fact]
    public void Should_report_holes_in_the_order_the_translation_puts_them()
    {
        // Act
        var segments = Collect("{1} לפני {0}");

        // Assert
        Assert.Equal([("", 1), (" לפני ", -1), ("", 0)], segments);
    }

    [Fact]
    public void Should_yield_nothing_for_an_empty_format()
        => Assert.Empty(Collect(string.Empty));

    [Fact]
    public void Should_walk_a_format_without_allocating()
    {
        // Arrange
        const string Format = "Stored {0} of {1}.";
        _ = Walk(Format);

        // Act
        var before = GC.GetAllocatedBytesForCurrentThread();
        var literalLength = Walk(Format);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        // Assert
        Assert.Equal(12, literalLength);
        Assert.Equal(0, allocated);
    }

    private static int Walk(string format)
    {
        var literalLength = 0;

        foreach (var segment in MessageSegment.Parse(format))
        {
            literalLength += segment.Text.Length;
        }

        return literalLength;
    }

    private static List<(string Text, int ArgumentIndex)> Collect(string format)
    {
        var segments = new List<(string, int)>();

        foreach (var segment in MessageSegment.Parse(format))
        {
            segments.Add((segment.Text.ToString(), segment.ArgumentIndex));
        }

        return segments;
    }
}
