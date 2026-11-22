namespace Forma.Framework.Browser.Interop.Tests.Geometry;

using Forma.Framework.Browser;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomGeometryTests
{
    private static readonly ElementReference Panel = new("panel");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new SizeState(InlineSize: 320, BlockSize: 48),
            _ => null,
        });

    public sealed class WatchingTheSize
    {
        [Fact]
        public async Task Should_ask_the_browser_to_measure_the_element_s_size_and_hand_the_first_size_to_the_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var handled = new List<SizeState>();

            // Act
            await using var observation = await watcher.GetElement(Panel).Geometry.WatchSizeAsync(state => { handled.Add(state); return Task.CompletedTask; });

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("size", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(Panel, FakeDomBrowser.GetRequestField(attach, "target")),
                () => Assert.Equal(new SizeState(320, 48), observation.Current),
                () => Assert.Equal([new SizeState(320, 48)], handled));
        }

        [Fact]
        public async Task Should_load_the_watcher_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Panel).Geometry.WatchSizeAsync(_ => Task.CompletedTask);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.Equal(0, browser.CountImports(FakeDomBrowser.EnginePath)));
        }

        [Fact]
        public async Task Should_make_no_call_while_it_is_not_to_observe()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Panel).Geometry.WatchSizeAsync(_ => Task.CompletedTask, shouldObserve: false);

            // Assert
            Assert.Multiple(
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }
}
