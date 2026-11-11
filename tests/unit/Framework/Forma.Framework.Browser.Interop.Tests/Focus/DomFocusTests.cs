namespace Forma.Framework.Browser.Interop.Tests.Focus;

using Forma.Framework.Browser.Interop.Focus;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomFocusTests
{
    private static readonly ElementReference Surface = new("surface");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new FocusState(IsWithin: true),
            (FakeDomBrowser.EnginePath, "attach") => true,
            _ => null,
        });

    public sealed class Watching
    {
        [Fact]
        public async Task Should_observe_the_focus_through_the_watcher_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Surface).Focus.WatchAsync(_ => Task.CompletedTask);

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("focus", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath)),
                () => Assert.Equal(0, browser.CountImports(FakeDomBrowser.EnginePath)));
        }

        [Fact]
        public async Task Should_hand_the_first_state_to_its_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var handled = new List<FocusState>();

            // Act
            await using var observation = await watcher.GetElement(Surface).Focus.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; });

            // Assert
            Assert.Equal([new FocusState(IsWithin: true)], handled);
        }

        [Fact]
        public async Task Should_ask_the_browser_to_name_the_item_holding_the_focus_by_the_attribute_items_carry()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Surface).Focus.WatchAsync(_ => Task.CompletedTask, new FocusWatchOptions { ItemAttribute = "data-node" });

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Equal("data-node", FakeDomBrowser.GetOption(attach, "itemAttribute"));
        }

        [Fact]
        public async Task Should_refuse_an_empty_item_attribute()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => watcher.GetElement(Surface).Focus.WatchAsync(_ => Task.CompletedTask, new FocusWatchOptions { ItemAttribute = " " }));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }

    public sealed class Interacting
    {
        [Fact]
        public async Task Should_ask_the_browser_for_each_part_of_the_interaction()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var options = new FocusInteractionOptions { Wraps = true, RegionSelectors = [".canvas"], RovingOrientation = FocusOrientation.Vertical, HeirAttribute = "data-heir" };

            // Act
            await using var interaction = await engine.GetElement(Surface).Focus.AttachAsync(options);

            // Assert
            var attach = Assert.Single(browser.Engine.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("focus", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(true, FakeDomBrowser.GetOption(attach, "wrap")),
                () => Assert.Equal([".canvas"], (IEnumerable<string>)FakeDomBrowser.GetOption(attach, "order")!),
                () => Assert.Equal("vertical", FakeDomBrowser.GetOption(attach, "rovingOrientation")),
                () => Assert.Equal("data-heir", FakeDomBrowser.GetOption(attach, "heir")));
        }

        [Fact]
        public async Task Should_make_no_call_for_options_naming_the_same_regions_again()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var interaction = await engine.GetElement(Surface).Focus.AttachAsync(new FocusInteractionOptions { RegionSelectors = [".canvas"] });

            // Act
            await interaction.SyncAsync(Surface, new FocusInteractionOptions { RegionSelectors = [".canvas"] });

            // Assert
            Assert.Single(browser.Engine.Recorder.GetCalls("attach"));
        }

        [Fact]
        public async Task Should_refuse_an_empty_heir_attribute()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => engine.GetElement(Surface).Focus.AttachAsync(new FocusInteractionOptions { HeirAttribute = " " }));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }
}
