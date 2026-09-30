namespace Forma.Framework.Localization.Tests;

public sealed class LocalizedFormatTests
{
    [Fact]
    public void Should_pair_values_with_the_holes_in_the_order_they_first_appear()
    {
        // Act
        var arguments = LocalizedFormat.Bind("Stored {stored} of {total}.", 3, 9);

        // Assert
        Assert.Equal([new LocalizedArgument("stored", 3), new LocalizedArgument("total", 9)], arguments);
    }

    [Fact]
    public void Should_count_a_repeated_hole_once()
    {
        // Act
        var arguments = LocalizedFormat.Bind("{count} of {count}", 4);

        // Assert
        Assert.Equal([new LocalizedArgument("count", 4)], arguments);
    }

    [Fact]
    public void Should_hold_a_lone_value_unnamed_when_the_format_names_no_hole()
    {
        // Act
        var arguments = LocalizedFormat.Bind("[$ plural; one: # file; other: # files; $]", 2);

        // Assert
        Assert.Equal([LocalizedArgument.Unnamed(2)], arguments);
    }

    [Fact]
    public void Should_pair_nothing_for_a_format_with_neither_holes_nor_values()
        => Assert.Empty(LocalizedFormat.Bind("Save"));

    [Theory]
    [InlineData("Stored {stored} of {total}.", 1)]
    [InlineData("Stored {stored} of {total}.", 3)]
    [InlineData("Save", 2)]
    public void Should_reject_a_value_count_the_format_has_no_holes_for(string format, int count)
        => Assert.Throws<ArgumentException>(() => LocalizedFormat.Bind(format, new object?[count]));

    [Fact]
    public void Should_reject_a_null_format()
        => Assert.Throws<ArgumentNullException>(() => LocalizedFormat.Bind(null!, 1));

    [Fact]
    public void Should_render_a_hole_naming_an_argument_that_was_not_supplied_as_its_own_text()
    {
        // Act
        var rendered = LocalizedFormat.Render("Stored {stored} of {total}.", [new LocalizedArgument("stored", 3)], CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("Stored 3 of {total}.", rendered);
    }

    [Fact]
    public void Should_render_a_hole_through_the_specifier_the_format_carries()
    {
        // Act
        var rendered = LocalizedFormat.Render("{rows:N0} rows", [new LocalizedArgument("rows", 5000)], CultureInfo.GetCultureInfo("en"));

        // Assert
        Assert.Equal("5,000 rows", rendered);
    }

    [Fact]
    public void Should_pad_a_hole_to_the_alignment_the_format_carries()
    {
        // Act
        var rendered = LocalizedFormat.Render("[{label,-4}]", [new LocalizedArgument("label", "x")], CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal("[x   ]", rendered);
    }
}
