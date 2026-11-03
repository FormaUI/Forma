namespace Forma.Blazor.Tests.Features.Emojis;

using Forma.Blazor.Features.Emojis;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class EmojiSpriteLoaderTests
{
    private const string Module = "./_content/forma/browser/scripts/operations/Asset.js";

    [Fact]
    public async Task Should_share_a_load_still_in_flight()
    {
        // Arrange
        await using var module = CreateModule(loaded: true);
        module.Recorder.Hold("loadSprite");
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = loader.LoadAsync("smileys-0.svg");
        var second = loader.LoadAsync("smileys-0.svg");
        module.Recorder.GetCalls("loadSprite")[0].Complete(true);

        // Assert
        Assert.True(await first);
        Assert.True(await second);
        Assert.Single(module.Recorder.GetCalls("loadSprite"));
    }

    [Fact]
    public async Task Should_not_refetch_a_sheet_that_arrived()
    {
        // Arrange
        await using var module = CreateModule(loaded: true);
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = await loader.LoadAsync("smileys-0.svg");
        var second = await loader.LoadAsync("smileys-0.svg");

        // Assert
        Assert.Multiple(
            () => Assert.True(first),
            () => Assert.True(second),
            () => Assert.Single(module.Recorder.GetCalls("loadSprite")));
    }

    [Fact]
    public async Task Should_retry_a_sheet_that_did_not_arrive()
    {
        // Arrange
        await using var module = CreateModule(loaded: false);
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = await loader.LoadAsync("smileys-0.svg");
        var second = await loader.LoadAsync("smileys-0.svg");

        // Assert
        Assert.Multiple(
            () => Assert.False(first),
            () => Assert.False(second),
            () => Assert.Equal(2, module.Recorder.GetCalls("loadSprite").Count));
    }

    [Fact]
    public async Task Should_ask_for_a_sheet_again_after_a_navigation()
    {
        // Arrange
        await using var module = CreateModule(loaded: true);
        var navigation = new RecordedNavigationManager();
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), NullLogger<EmojiSpriteLoader>.Instance, navigation);
        await loader.LoadAsync("smileys-0.svg");

        // Act
        navigation.NavigateTo("/composer");
        await loader.LoadAsync("smileys-0.svg");

        // Assert
        Assert.Equal(2, module.Recorder.GetCalls("loadSprite").Count);
    }

    [Fact]
    public async Task Should_log_each_attempt_at_a_sheet_that_did_not_load()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var module = CreateModule(loaded: false);
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), new Logger<EmojiSpriteLoader>(loggerFactory));

        // Act
        await loader.LoadAsync("smileys-0.svg");
        await loader.LoadAsync("smileys-0.svg");

        // Assert
        Assert.Equal(2, loggerFactory.Entries.Count);
        var entry = loggerFactory.Entries[0];
        Assert.Multiple(() =>
        {
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Contains("smileys-0.svg", entry.Message, StringComparison.Ordinal);
            Assert.Contains("did not load", entry.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Should_refuse_a_nameless_sheet()
    {
        // Arrange
        await using var module = CreateModule(loaded: true);
        await using var loader = new EmojiSpriteLoader(CreateRuntime(module), NullLogger<EmojiSpriteLoader>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => loader.LoadAsync(" "));
        Assert.Empty(module.Recorder.Calls);
    }

    private static RecordingJSObjectReference CreateModule(bool loaded)
        => new((identifier, _) => identifier == "loadSprite" ? loaded : null);

    private static RecordingJSRuntime CreateRuntime(RecordingJSObjectReference module)
        => new((identifier, args) => identifier == "import" && (string?)args[0] == Module ? module : null);

    private sealed class RecordedNavigationManager : NavigationManager
    {
        public RecordedNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/");
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = ToAbsoluteUri(uri).ToString();
            NotifyLocationChanged(isInterceptedLink: false);
        }

        protected override void EnsureInitialized()
        {
        }
    }
}
