namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocalizedPatternTests
{
    [Fact]
    public void Should_split_its_format_into_segments()
    {
        // Arrange
        var pattern = new LocalizedPattern(
            "Stored {stored} of {total}.",
            "נשמרו {stored} מתוך {total}.",
            [new LocalizedArgument("stored", 3), new LocalizedArgument("total", 4)]);

        // Act
        var texts = new List<string>();
        var names = new List<string>();

        foreach (var segment in pattern.Segments())
        {
            texts.Add(segment.Text.ToString());
            names.Add(segment.Name.ToString());
        }

        // Assert
        Assert.Equal(["נשמרו ", "", " מתוך ", "", "."], texts);
        Assert.Equal(["", "stored", "", "total", ""], names);
    }

    [Fact]
    public void Should_expose_the_arguments_the_holes_name()
    {
        // Arrange
        var pattern = new LocalizedPattern("Stored {stored}.", "נשמרו {stored}.", [new LocalizedArgument("stored", 7)]);

        // Act
        object? held = null;

        foreach (var segment in pattern.Segments())
        {
            if (segment.IsHole)
            {
                held = Find(pattern, segment.Name);
            }
        }

        // Assert
        Assert.Equal(7, held);
    }

    [Fact]
    public void Should_keep_the_order_a_translation_reverses()
    {
        // Arrange
        var pattern = new LocalizedPattern(
            "{first} before {second}",
            "{second} לפני {first}",
            [new LocalizedArgument("first", "a"), new LocalizedArgument("second", "b")]);

        // Act
        var values = new List<object?>();

        foreach (var segment in pattern.Segments())
        {
            if (segment.IsHole)
            {
                values.Add(Find(pattern, segment.Name));
            }
        }

        // Assert
        Assert.Equal(["b", "a"], values);
    }

    private static object? Find(LocalizedPattern pattern, ReadOnlySpan<char> name)
    {
        foreach (var argument in pattern.Arguments)
        {
            if (name.SequenceEqual(argument.Name))
            {
                return argument.Value;
            }
        }

        return null;
    }

    [Fact]
    public void Should_yield_nothing_for_a_default_pattern()
    {
        // Arrange
        var pattern = default(LocalizedPattern);

        // Act
        var count = 0;

        foreach (var segment in pattern.Segments())
        {
            count += segment.Text.Length + 1;
        }

        // Assert
        Assert.Equal(0, count);
    }
}
