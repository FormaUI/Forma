namespace Forma.Blazor.Tests.Models;

using Forma.Blazor.Models;

public sealed class ZoomScaleTests
{
    [Fact]
    public void Should_rest_on_the_first_step()
    {
        // Act
        var zoom = new ZoomScale([100, 150, 200]);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(100, zoom.Percent),
            () => Assert.False(zoom.IsZoomedIn),
            () => Assert.True(zoom.IsAtMinimum),
            () => Assert.False(zoom.IsAtMaximum));
    }

    [Fact]
    public void Should_step_in_and_out_along_the_run()
    {
        // Arrange
        var zoom = new ZoomScale([100, 150, 200]);

        // Act & Assert
        Assert.True(zoom.TryZoomIn());
        Assert.Multiple(
            () => Assert.Equal(150, zoom.Percent),
            () => Assert.True(zoom.IsZoomedIn));

        Assert.True(zoom.TryZoomOut());
        Assert.Equal(100, zoom.Percent);
    }

    [Fact]
    public void Should_stay_put_at_either_end()
    {
        // Arrange
        var zoom = new ZoomScale([100, 150]);

        // Act & Assert
        Assert.False(zoom.TryZoomOut());
        Assert.Equal(100, zoom.Percent);

        Assert.True(zoom.TryZoomIn());
        Assert.Multiple(
            () => Assert.True(zoom.IsAtMaximum),
            () => Assert.False(zoom.TryZoomIn()),
            () => Assert.Equal(150, zoom.Percent));
    }

    [Fact]
    public void Should_return_to_rest_from_anywhere()
    {
        // Arrange
        var zoom = new ZoomScale([100, 150, 200]);
        zoom.TryZoomIn();
        zoom.TryZoomIn();

        // Act
        zoom.Reset();

        // Assert
        Assert.Multiple(
            () => Assert.Equal(100, zoom.Percent),
            () => Assert.False(zoom.IsZoomedIn));
    }

    [Fact]
    public void Should_refuse_an_empty_run_of_steps()
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new ZoomScale([]));
    }
}
