namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocalizedSegmentTests
{
    [Fact]
    public void Should_split_a_format_into_literals_and_holes()
    {
        // Act
        var segments = Collect("Stored {stored} of {total}.");

        // Assert
        Assert.Equal([("Stored ", ""), ("", "stored"), (" of ", ""), ("", "total"), (".", "")], segments);
    }

    [Fact]
    public void Should_treat_a_format_without_holes_as_one_literal()
    {
        // Act
        var segments = Collect("Save");

        // Assert
        Assert.Equal([("Save", "")], segments);
    }

    [Fact]
    public void Should_yield_a_doubled_brace_as_a_single_literal_brace()
    {
        // Act
        var segments = Collect("Use {{curly}} braces {count}.");

        // Assert
        Assert.Equal(
            [("Use ", ""), ("{", ""), ("curly", ""), ("}", ""), (" braces ", ""), ("", "count"), (".", "")],
            segments);
    }

    [Fact]
    public void Should_read_a_hole_wrapped_in_escaped_braces()
    {
        // Act
        var segments = Collect("{{{count:D}}}");

        // Assert
        Assert.Equal([("{", ""), ("", "count"), ("}", "")], segments);
    }

    [Fact]
    public void Should_report_the_same_name_each_time_a_format_repeats_it()
    {
        // Act
        var segments = Collect("0x{value:X} {value:E} {value:N}");

        // Assert
        Assert.Equal([("0x", ""), ("", "value"), (" ", ""), ("", "value"), (" ", ""), ("", "value")], segments);
    }

    [Fact]
    public void Should_keep_a_hole_that_carries_a_format_specifier()
    {
        // Act
        var segments = Collect("Ratio {share:P1}.");

        // Assert
        Assert.Equal([("Ratio ", ""), ("", "share"), (".", "")], segments);
    }

    [Fact]
    public void Should_expose_the_specifier_a_hole_carries()
    {
        // Act
        var (name, alignment, format) = Single("Ratio {share:P1}.");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("share", name);
            Assert.Equal("P1", format);
            Assert.Equal(0, alignment);
        });
    }

    [Fact]
    public void Should_keep_a_hole_that_carries_an_alignment()
    {
        // Act
        var segments = Collect("{label,-8}|");

        // Assert
        Assert.Equal([("", "label"), ("|", "")], segments);
    }

    [Fact]
    public void Should_expose_the_alignment_a_hole_carries()
    {
        // Act
        var (name, alignment, format) = Single("{label,-8}|");

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("label", name);
            Assert.Equal(-8, alignment);
            Assert.Equal(string.Empty, format);
        });
    }

    [Fact]
    public void Should_treat_a_brace_group_that_is_not_an_identifier_as_literal_text()
    {
        // Act
        var segments = Collect("A {0} here");

        // Assert
        Assert.Equal([("A ", ""), ("{0}", ""), (" here", "")], segments);
    }

    [Fact]
    public void Should_treat_an_unclosed_brace_as_literal_text()
    {
        // Act
        var segments = Collect("Broken {count here");

        // Assert
        Assert.Equal([("Broken ", ""), ("{count here", "")], segments);
    }

    [Fact]
    public void Should_treat_a_stray_closing_brace_as_literal_text()
    {
        // Act
        var segments = Collect("Closed } here");

        // Assert
        Assert.Equal([("Closed ", ""), ("}", ""), (" here", "")], segments);
    }

    [Fact]
    public void Should_report_holes_in_the_order_the_translation_puts_them()
    {
        // Act
        var segments = Collect("{total} \u05dc\u05e4\u05e0\u05d9 {stored}");

        // Assert
        Assert.Equal([("", "total"), (" \u05dc\u05e4\u05e0\u05d9 ", ""), ("", "stored")], segments);
    }

    [Fact]
    public void Should_yield_nothing_for_an_empty_format()
        => Assert.Empty(Collect(string.Empty));

    [Fact]
    public void Should_walk_a_format_without_allocating()
    {
        // Arrange
        const string Format = "Stored {stored} of {total}.";
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

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            literalLength += segment.Text.Length;
        }

        return literalLength;
    }

    private static (string Name, int Alignment, string Format) Single(string format)
    {
        foreach (var segment in LocalizedSegment.Parse(format))
        {
            if (segment.IsHole)
            {
                return (segment.Name.ToString(), segment.Alignment, segment.Format.ToString());
            }
        }

        throw new InvalidOperationException($"'{format}' carries no hole.");
    }

    private static List<(string Text, string Name)> Collect(string format)
    {
        var segments = new List<(string, string)>();

        foreach (var segment in LocalizedSegment.Parse(format))
        {
            segments.Add((segment.Text.ToString(), segment.Name.ToString()));
        }

        return segments;
    }
}
