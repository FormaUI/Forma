namespace Forma.Framework.Localization.Abstractions.Tests;

public sealed class LocalizedPatternTests
{
    [Fact]
    public void Should_split_its_format_into_segments()
    {
        // Arrange
        var pattern = new LocalizedPattern("Stored {0} of {1}.", "נשמרו {0} מתוך {1}.", [3, 4]);

        // Act
        var texts = new List<string>();
        var indexes = new List<int>();

        foreach (var segment in pattern.Segments())
        {
            texts.Add(segment.Text.ToString());
            indexes.Add(segment.ArgumentIndex);
        }

        // Assert
        Assert.Equal(["נשמרו ", "", " מתוך ", "", "."], texts);
        Assert.Equal([-1, 0, -1, 1, -1], indexes);
    }

    [Fact]
    public void Should_expose_the_arguments_the_holes_name()
    {
        // Arrange
        var pattern = new LocalizedPattern("Stored {0}.", "נשמרו {0}.", [7]);

        // Act
        object? held = null;

        foreach (var segment in pattern.Segments())
        {
            if (segment.IsHole)
            {
                held = pattern.Arguments[segment.ArgumentIndex];
            }
        }

        // Assert
        Assert.Equal(7, held);
    }

    [Fact]
    public void Should_keep_the_order_a_translation_reverses()
    {
        // Arrange
        var pattern = new LocalizedPattern("{0} before {1}", "{1} לפני {0}", ["a", "b"]);

        // Act
        var values = new List<object?>();

        foreach (var segment in pattern.Segments())
        {
            if (segment.IsHole)
            {
                values.Add(pattern.Arguments[segment.ArgumentIndex]);
            }
        }

        // Assert
        Assert.Equal(["b", "a"], values);
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
