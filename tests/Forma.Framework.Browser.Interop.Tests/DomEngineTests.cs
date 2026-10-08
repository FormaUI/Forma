namespace Forma.Framework.Browser.Interop.Tests;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomEngineTests
{
    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ScrollState Start = new(false, true, 0, 0, null);

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => Start,
            (FakeDomBrowser.EnginePath, "attach") => true,
            _ => null,
        });

    public sealed class Releasing
    {
        [Fact]
        public async Task Should_release_its_interactions_in_one_call_and_leave_the_watchers_observations()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var engine = new DomEngine(watcher);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(new ScrollInteractionOptions());

            // Act
            await engine.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Engine.Recorder.GetCalls("releaseEngine")),
                () => Assert.Empty(browser.Engine.Recorder.GetCalls("detach")),
                () => Assert.True(interaction.IsDisposed),
                () => Assert.True(observation.IsActive),
                () => Assert.False(watcher.IsDisposed));
        }

        [Fact]
        public async Task Should_release_what_is_left_through_the_watcher_once_the_engine_has_gone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var engine = new DomEngine(watcher);
            await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            await engine.GetElement(Viewport).Scroll.AttachAsync(new ScrollInteractionOptions());
            await engine.DisposeAsync();

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Watcher.Recorder.GetCalls("release")),
                () => Assert.Empty(browser.Engine.Recorder.GetCalls("release")));
        }

        [Fact]
        public async Task Should_attach_nothing_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var engine = new DomEngine(watcher);
            await engine.DisposeAsync();

            // Act
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(new ScrollInteractionOptions());
            var isMoving = await engine.GetElement(Viewport).Scroll.StepAsync(ScrollDirection.Forward, new ScrollStepOptions());

            // Assert
            Assert.Multiple(
                () => Assert.True(interaction.IsDisposed),
                () => Assert.False(isMoving),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_end_with_the_watcher_it_borrows()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var watcher = new DomWatcher(CreateBrowser().CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.True(engine.IsDisposed);
        }
    }
}
