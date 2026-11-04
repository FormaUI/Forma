namespace Forma.Framework.Browser.Interop.Tests.Text;

using Forma.Framework.Browser.Interop.Text;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomTextTests
{
    private static readonly ElementReference Field = new("field");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new TextSelection(2, 5),
            _ => null,
        });

    public sealed class Selecting
    {
        [Fact]
        public async Task Should_observe_the_selection_through_the_watcher_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Field).Text.WatchSelectionAsync(_ => Task.CompletedTask);

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("selection", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(0, browser.CountImports(FakeDomBrowser.EnginePath)));
        }

        [Fact]
        public async Task Should_hand_the_first_range_to_its_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var handled = new List<TextSelection>();

            // Act
            await using var observation = await watcher.GetElement(Field).Text.WatchSelectionAsync(selection =>
            {
                handled.Add(selection);
                return Task.CompletedTask;
            });

            // Assert
            Assert.Equal([new TextSelection(2, 5)], handled);
        }

        [Fact]
        public async Task Should_make_no_call_while_it_should_not_observe()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));

            // Act
            await using var observation = await watcher.GetElement(Field).Text.WatchSelectionAsync(_ => Task.CompletedTask, shouldObserve: false);

            // Assert
            Assert.Empty(browser.Watcher.Recorder.GetCalls("attach"));
        }
    }
}
