namespace Forma.Blazor.Tests.Models;

using Forma.Blazor.Models;

public sealed class SizeTests
{
    [Fact]
    public void Should_hold_a_width_and_a_height()
    {
        // Act
        var size = new Size(320, 200);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(320, size.Width),
            () => Assert.Equal(200, size.Height));
    }

    [Fact]
    public void Should_read_from_a_tuple()
    {
        // Act
        Size size = (320, 200);

        // Assert
        Assert.Equal(new Size(320, 200), size);
    }

    [Fact]
    public void Should_compare_by_value()
    {
        // Act & Assert
        Assert.Multiple(
            () => Assert.Equal(new Size(1, 2), new Size(1, 2)),
            () => Assert.NotEqual(new Size(1, 2), new Size(2, 1)));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Should_reject_a_negative_side(double width, double height)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new Size(width, height));
    }
}
