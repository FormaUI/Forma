namespace Forma.Framework.Browser.Interop.Tests;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class DomWatcherTests
{
    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ScrollState Start = new(false, true, 0, 0, null);

    private static FakeDomBrowser CreateBrowser(Func<object?>? release = null)
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => Start,
            (FakeDomBrowser.EnginePath, "attach") => true,
            (_, "release") => release?.Invoke(),
            _ => null,
        });

    public sealed class Observing
    {
        [Fact]
        public async Task Should_load_only_the_watcher_to_observe()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.Equal(0, browser.CountImports(FakeDomBrowser.EnginePath)),
                () => Assert.Single(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_observe_its_element_but_refuse_to_act_on_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var element = (DomElement)watcher.GetElement(Viewport);

            // Act
            var stepping = await Assert.ThrowsAsync<InvalidOperationException>(() => element.Scroll.StepAsync(ScrollDirection.Forward, new ScrollStepOptions()).AsTask());
            var attaching = await Assert.ThrowsAsync<InvalidOperationException>(() => element.Scroll.AttachAsync(new ScrollInteractionOptions()));

            // Assert
            Assert.Multiple(
                () => Assert.Contains("observes alone", stepping.Message, StringComparison.Ordinal),
                () => Assert.Contains("observes alone", attaching.Message, StringComparison.Ordinal),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_keep_the_element_it_was_given()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var watcher = new DomWatcher(CreateBrowser().CreateEnvironment(loggerFactory));

            // Act
            var element = watcher.GetElement(Viewport);

            // Assert
            Assert.Equal(Viewport, element.Element);
        }
    }

    public sealed class Releasing
    {
        [Fact]
        public async Task Should_release_every_observation_in_one_call_under_its_owner()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var runtimeId = Guid.CreateVersion7();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, runtimeId));
            var first = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            var second = await watcher.GetElement(new ElementReference("other")).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            var release = Assert.Single(browser.Watcher.Recorder.GetCalls("release"));
            Assert.Multiple(
                () => Assert.Equal(runtimeId.ToString(), release.Arguments[0]),
                () => Assert.Empty(browser.Watcher.Recorder.GetCalls("detach")),
                () => Assert.True(first.IsDisposed),
                () => Assert.True(second.IsDisposed),
                () => Assert.True(watcher.IsDisposed));
        }

        [Fact]
        public async Task Should_release_through_the_engine_once_its_owner_has_acted()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(new ScrollInteractionOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Engine.Recorder.GetCalls("release")),
                () => Assert.Empty(browser.Watcher.Recorder.GetCalls("release")),
                () => Assert.True(interaction.IsDisposed),
                () => Assert.True(engine.IsDisposed));
        }

        [Fact]
        public async Task Should_make_no_call_to_release_an_owner_that_reached_nothing()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            watcher.GetElement(Viewport);

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Empty(browser.Runtime.Recorder.Calls);
        }

        [Fact]
        public async Task Should_release_the_bridge_it_made_for_itself()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Equal(1, browser.Watcher.Releases);
        }

        [Fact]
        public async Task Should_log_a_release_the_browser_failed_and_end_its_use_all_the_same()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser(() => throw new JSException("The owner's session is gone."));
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Warning, entry.Level),
                () => Assert.Contains("release failed", entry.Message, StringComparison.Ordinal),
                () => Assert.True(observation.IsDisposed),
                () => Assert.Equal(1, browser.Watcher.Releases));
        }

        [Fact]
        public async Task Should_log_a_release_the_runtime_canceled_at_debug_and_end_its_use_all_the_same()
        {
            // Arrange — as the runtime cancels the calls of a circuit that ends while its components are disposed
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser(() => throw new TaskCanceledException());
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Debug, entry.Level),
                () => Assert.Contains("was canceled", entry.Message, StringComparison.Ordinal),
                () => Assert.True(observation.IsDisposed),
                () => Assert.Equal(1, browser.Watcher.Releases));
        }

        [Fact]
        public async Task Should_make_nothing_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await watcher.DisposeAsync();

            // Act
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

            // Assert
            Assert.Multiple(
                () => Assert.True(observation.IsDisposed),
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_take_a_second_disposal_as_nothing()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            await watcher.DisposeAsync();

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Single(browser.Watcher.Recorder.GetCalls("release"));
        }

        [Fact]
        public async Task Should_release_an_attach_still_in_flight_and_take_nothing_it_returns()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Watcher.Recorder.Hold("attach");
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var handled = new List<ScrollState>();
            var watching = watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, new ScrollWatchOptions());
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));

            // Act
            await watcher.DisposeAsync();
            attach.Complete(Start);
            await using var observation = await watching;

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Watcher.Recorder.GetCalls("release")),
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(handled));
        }

        [Fact]
        public async Task Should_send_no_attach_once_released_while_its_entry_was_loading()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Runtime.Recorder.Hold("import");
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var watching = watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            var import = Assert.Single(browser.Runtime.Recorder.Calls);

            // Act - the watcher's own bridge releases what the import brings once it lands
            var disposing = watcher.DisposeAsync();
            import.Complete(browser.Watcher);
            await disposing;
            await using var observation = await watching;

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Watcher.Recorder.Calls),
                () => Assert.Equal(1, browser.Watcher.Releases),
                () => Assert.True(observation.IsDisposed));
        }
    }
}
