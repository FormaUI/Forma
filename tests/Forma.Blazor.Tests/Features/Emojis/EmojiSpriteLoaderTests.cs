namespace Forma.Blazor.Tests.Features.Emojis;

using Forma.Blazor.Features.Emojis;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class EmojiSpriteLoaderTests
{
    private const string Module = "./_content/forma/common/scripts/operations/Asset.js";

    [Fact]
    public async Task Should_share_a_load_still_in_flight()
    {
        // Arrange
        var js = new BunitJSInterop { Mode = JSRuntimeMode.Loose };
        var handler = js.SetupModule(Module).Setup<bool>("loadSprite", _ => true);
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = loader.LoadAsync("smileys-0.svg");
        var second = loader.LoadAsync("smileys-0.svg");
        handler.SetResult(true);

        // Assert
        // Racers share the one load, so a surface drawing hundreds of emoji costs one fetch per sheet.
        Assert.True(await first);
        Assert.True(await second);
        Assert.Single(js.Invocations["loadSprite"]);
    }

    [Fact]
    public async Task Should_not_refetch_a_sheet_that_arrived()
    {
        // Arrange
        var js = new BunitJSInterop { Mode = JSRuntimeMode.Loose };
        js.SetupModule(Module).Setup<bool>("loadSprite", _ => true).SetResult(true);
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = await loader.LoadAsync("smileys-0.svg");
        var second = await loader.LoadAsync("smileys-0.svg");

        // Assert
        Assert.Multiple(
            () => Assert.True(first),
            () => Assert.True(second),
            () => Assert.Single(js.Invocations["loadSprite"]));
    }

    [Fact]
    public async Task Should_retry_a_sheet_that_did_not_arrive()
    {
        // Arrange
        var js = new BunitJSInterop { Mode = JSRuntimeMode.Loose };
        js.SetupModule(Module).Setup<bool>("loadSprite", _ => true).SetResult(false);
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, NullLogger<EmojiSpriteLoader>.Instance);

        // Act
        var first = await loader.LoadAsync("smileys-0.svg");
        var second = await loader.LoadAsync("smileys-0.svg");

        // Assert
        // A failed sheet is forgotten rather than remembered as failed, so a later surface retries it
        // instead of leaving its emoji blank for the rest of the circuit.
        Assert.Multiple(
            () => Assert.False(first),
            () => Assert.False(second),
            () => Assert.Equal(2, js.Invocations["loadSprite"].Count));
    }

    [Fact]
    public async Task Should_ask_for_a_sheet_again_after_a_navigation()
    {
        // Arrange
        var js = new BunitJSInterop { Mode = JSRuntimeMode.Loose };
        js.SetupModule(Module).Setup<bool>("loadSprite", _ => true).SetResult(true);
        var navigation = new RecordedNavigationManager();
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, NullLogger<EmojiSpriteLoader>.Instance, navigation);
        await loader.LoadAsync("smileys-0.svg");

        // Act
        navigation.NavigateTo("/composer");
        await loader.LoadAsync("smileys-0.svg");

        // Assert
        // A navigation replaces the document's body, symbols included, so what arrived before it is gone.
        Assert.Equal(2, js.Invocations["loadSprite"].Count);
    }

    [Fact]
    public async Task Should_log_each_attempt_at_a_sheet_that_did_not_load()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        var js = new BunitJSInterop { Mode = JSRuntimeMode.Loose };
        js.SetupModule(Module).Setup<bool>("loadSprite", _ => true).SetResult(false);
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, new Logger<EmojiSpriteLoader>(loggerFactory));

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
        var js = new BunitJSInterop();
        await using var loader = new EmojiSpriteLoader(js.JSRuntime, NullLogger<EmojiSpriteLoader>.Instance);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => loader.LoadAsync(" "));
    }

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
