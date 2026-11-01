namespace Forma.Framework.Culture.Blazor.WebView.Tests;

using Microsoft.AspNetCore.Components.Routing;

public sealed class LocationChangingContextExtensionsTests
{
    private static LocationChangingContext CreateContext(string? historyEntryState)
        => new() { TargetLocation = "https://0.0.0.1/he/about", HistoryEntryState = historyEntryState };

    [Fact]
    public void Should_tell_a_navigation_following_the_application_s_culture()
    {
        // Act
        var isFollowing = CreateContext("forma-culture").IsCultureFollowing();

        // Assert
        Assert.True(isFollowing);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Forma-Culture")]
    [InlineData("draft")]
    public void Should_tell_apart_a_navigation_of_another_state(string? historyEntryState)
    {
        // Act
        var isFollowing = CreateContext(historyEntryState).IsCultureFollowing();

        // Assert
        Assert.False(isFollowing);
    }
}
