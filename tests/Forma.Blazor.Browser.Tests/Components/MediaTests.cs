namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The media components' main interactions, in every runtime.</summary>
public sealed class MediaTests
{
    public sealed class VideoPlayer(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_load_its_embedded_player_only_once_its_play_control_is_pressed(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "video-player");
            await using var _ = context;
            var play = page.GetByRole(AriaRole.Button, new() { Name = "Play Check video", Exact = true });
            var frame = page.Locator("#check iframe");
            await Assertions.Expect(play).ToHaveAccessibleDescriptionAsync("Loads from another site, which will see this visit.");
            await Assertions.Expect(frame).ToHaveCountAsync(0);

            // Act
            await play.ClickAsync();

            // Assert
            await Assertions.Expect(play).ToHaveCountAsync(0);
            await Assertions.Expect(frame).ToHaveAttributeAsync("title", "Check video");
            await Assertions.Expect(page.FrameLocator("#check iframe").Locator("body")).ToHaveTextAsync("ready");
        }
    }
}
