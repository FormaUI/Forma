namespace Forma.Features.Files.Tests;

using Xunit;

public sealed class SteppedZoomTests
{
    [Fact]
    public void Should_rest_on_the_first_step()
    {
        // Act
        var zoom = new SteppedZoom([100, 150, 200]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(100, zoom.Percent),
            () => Assert.False(zoom.IsZoomed),
            () => Assert.True(zoom.AtMinimum),
            () => Assert.False(zoom.AtMaximum));
    }

    [Fact]
    public void Should_step_in_and_out_along_the_run()
    {
        // Arrange
        var zoom = new SteppedZoom([100, 150, 200]);

        // Act & Assert
        Assert.True(zoom.In());
        Assert.Multiple(
            () => Assert.Equal(150, zoom.Percent),
            () => Assert.True(zoom.IsZoomed));

        Assert.True(zoom.Out());
        Assert.Equal(100, zoom.Percent);
    }

    [Fact]
    public void Should_stay_put_at_either_end()
    {
        // Arrange
        var zoom = new SteppedZoom([100, 150]);

        // Act & Assert
        Assert.False(zoom.Out());
        Assert.Equal(100, zoom.Percent);

        Assert.True(zoom.In());
        Assert.Multiple(
            () => Assert.True(zoom.AtMaximum),
            () => Assert.False(zoom.In()),
            () => Assert.Equal(150, zoom.Percent));
    }

    [Fact]
    public void Should_return_to_rest_from_anywhere()
    {
        // Arrange
        var zoom = new SteppedZoom([100, 150, 200]);
        zoom.In();
        zoom.In();

        // Act
        zoom.Reset();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(100, zoom.Percent),
            () => Assert.False(zoom.IsZoomed));
    }

    [Fact]
    public void Should_refuse_an_empty_run_of_steps()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SteppedZoom([]));
    }
}
