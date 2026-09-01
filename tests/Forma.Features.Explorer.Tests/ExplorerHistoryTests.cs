namespace Forma.Features.Explorer.Tests;

using Xunit;

public sealed class ExplorerHistoryTests
{
    [Fact]
    public void Should_start_at_all_media_with_nowhere_to_go()
    {
        // Act
        var history = new ExplorerHistory();

        // Assert
        Assert.Multiple(
            () => Assert.Null(history.Current),
            () => Assert.False(history.CanGoBack),
            () => Assert.False(history.CanGoForward));
    }

    [Fact]
    public void Should_walk_back_and_forward_along_the_trail()
    {
        // Arrange
        var history = new ExplorerHistory();
        history.Visit("/photos");
        history.Visit("/photos/races");

        // Act & Assert
        history.GoBack();
        Assert.Equal("/photos", history.Current);

        history.GoBack();
        Assert.Multiple(
            () => Assert.Null(history.Current),
            () => Assert.False(history.CanGoBack),
            () => Assert.True(history.CanGoForward));

        history.GoForward();
        Assert.Equal("/photos", history.Current);
    }

    [Fact]
    public void Should_stay_put_at_either_end_of_the_trail()
    {
        // Arrange
        var history = new ExplorerHistory();
        history.Visit("/photos");

        // Act
        history.GoForward();
        history.GoBack();
        history.GoBack();

        // Assert
        Assert.Null(history.Current);
    }

    [Fact]
    public void Should_drop_what_lay_ahead_when_a_new_folder_is_visited()
    {
        // Arrange
        var history = new ExplorerHistory();
        history.Visit("/photos");
        history.Visit("/photos/races");
        history.GoBack();

        // Act
        history.Visit("/documents");

        // Assert
        Assert.Multiple(
            () => Assert.Equal("/documents", history.Current),
            () => Assert.False(history.CanGoForward));

        history.GoBack();
        Assert.Equal("/photos", history.Current);
    }
}
