namespace Forma.Framework.Browser.Interop.Tests.Scrolling;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomIntersectionTests
{
    private static readonly ElementReference Sentinel = new("sentinel");
    private static readonly ElementReference Panel = new("panel");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new IntersectionState(IsIntersecting: true),
            _ => null,
        });

    public sealed class Watching
    {
        [Fact]
        public async Task Should_ask_the_browser_to_measure_the_element_against_its_root_and_margin()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var handled = new List<IntersectionState>();

            // Act
            await using var observation = await watcher.GetElement(Sentinel).Intersection.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, new IntersectionWatchOptions { Root = Panel, RootMarginPixels = 200 });

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("intersection", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(Panel, FakeDomBrowser.GetOption(attach, "root")),
                () => Assert.Equal(200d, FakeDomBrowser.GetOption(attach, "rootMargin")),
                () => Assert.Equal([new IntersectionState(true)], handled));
        }

        [Fact]
        public async Task Should_load_the_watcher_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Sentinel).Intersection.WatchAsync(_ => Task.CompletedTask, new IntersectionWatchOptions());

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
            await using var observation = await watcher.GetElement(Sentinel).Intersection.WatchAsync(_ => Task.CompletedTask, new IntersectionWatchOptions(), shouldObserve: false);

            // Assert
            Assert.Multiple(
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_refuse_a_margin_below_zero()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => watcher.GetElement(Sentinel).Intersection.WatchAsync(_ => Task.CompletedTask, new IntersectionWatchOptions { RootMarginPixels = -1 }));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }
}
