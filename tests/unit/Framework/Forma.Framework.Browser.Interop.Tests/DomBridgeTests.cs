namespace Forma.Framework.Browser.Interop.Tests;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Xunit;

public sealed class DomBridgeTests
{
    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ScrollState Start = new(false, true, 0, 0, null);

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => Start,
            _ => null,
        });

    private static Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(DomWatcher watcher)
        => watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

    public sealed class Importing
    {
        [Fact]
        public async Task Should_import_each_entry_once_for_every_owner_sharing_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var first = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            await using var second = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);

            // Act
            await using var firstObservation = await WatchAsync(first);
            await using var secondObservation = await WatchAsync(second);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.True(firstObservation.IsActive),
                () => Assert.True(secondObservation.IsActive));
        }

        [Fact]
        public async Task Should_share_an_import_in_flight_between_owners()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Runtime.Recorder.Hold("import");
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var first = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            await using var second = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            var firstWatching = WatchAsync(first);
            var secondWatching = WatchAsync(second);

            // Act
            Assert.Single(browser.Runtime.Recorder.Calls).Complete(browser.Watcher);
            await using var firstObservation = await firstWatching;
            await using var secondObservation = await secondWatching;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.Equal(2, browser.Watcher.Recorder.GetCalls("attach").Count));
        }

        [Fact]
        public async Task Should_import_an_entry_again_after_an_import_failed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Runtime.Recorder.Hold("import");
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            var watching = WatchAsync(watcher);
            Assert.Single(browser.Runtime.Recorder.Calls).Fail(new JSException("Failed to fetch dynamically imported module"));
            await using var observation = await watching;
            browser.Runtime.Recorder.Release("import");

            // Act
            await observation.SyncAsync(Viewport, new ScrollWatchOptions());

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.True(observation.IsActive),
                () => Assert.Equal(LogLevel.Warning, entry.Level),
                () => Assert.Contains("DomWatcher could not be imported for attach scroll 1.1", entry.Message, StringComparison.Ordinal));
        }
    }

    public sealed class Disposing
    {
        [Fact]
        public async Task Should_release_each_entry_it_imported()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            await using var observation = await WatchAsync(watcher);

            // Act
            await bridge.DisposeAsync();

            // Assert
            Assert.Equal(1, browser.Watcher.Releases);
        }

        [Fact]
        public async Task Should_release_an_entry_whose_import_lands_after_disposal_began()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Runtime.Recorder.Hold("import");
            var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            var watching = WatchAsync(watcher);

            // Act
            var disposing = bridge.DisposeAsync();
            Assert.Single(browser.Runtime.Recorder.Calls).Complete(browser.Watcher);
            await disposing;
            await using var observation = await watching;

            // The release follows the import's landing, which the disposal doesn't wait for.
            await browser.Watcher.Released.WaitAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(1, browser.Watcher.Releases);
        }

        [Fact]
        public async Task Should_import_nothing_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await bridge.DisposeAsync();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);

            // Act
            await using var observation = await WatchAsync(watcher);

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Empty(browser.Runtime.Recorder.Calls),
                () => Assert.False(observation.IsActive),
                () => Assert.Contains("was released before", entry.Message, StringComparison.Ordinal));
        }
    }
}
