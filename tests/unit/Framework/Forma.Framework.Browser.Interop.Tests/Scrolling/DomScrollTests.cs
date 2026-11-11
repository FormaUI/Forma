namespace Forma.Framework.Browser.Interop.Tests.Scrolling;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;

using Xunit;

public sealed class DomScrollTests
{
    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ElementReference Pane = new("pane");

    private static FakeDomBrowser CreateBrowser(bool isMoving = true)
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new ScrollState(false, false, 0, 0),
            (FakeDomBrowser.EnginePath, "attach") => true,
            (FakeDomBrowser.EnginePath, "run") => isMoving,
            _ => null,
        });

    public sealed class Watching
    {
        [Fact]
        public async Task Should_ask_the_browser_for_the_axis_the_frozen_parts_and_the_viewport_extent()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            var options = new ScrollWatchOptions { Axis = ScrollAxis.Block, FrozenSelector = ".frozen", ReportsViewportExtent = true };

            // Act
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, options);

            // Assert
            var attach = Assert.Single(browser.Watcher.Recorder.GetCalls("attach"));
            Assert.Multiple(
                () => Assert.Equal("block", FakeDomBrowser.GetOption(attach, "axis")),
                () => Assert.Equal(".frozen", FakeDomBrowser.GetOption(attach, "frozenSelector")),
                () => Assert.Equal(true, FakeDomBrowser.GetOption(attach, "viewportExtent")));
        }
    }

    public sealed class Interacting
    {
        [Fact]
        public async Task Should_ask_the_browser_for_the_steps_the_buttons_and_the_wheel()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var options = new ScrollInteractionOptions
            {
                Steps = new ScrollStepOptions { StopSelector = ".stop", FrozenSelector = ".frozen" },
                Buttons = new ScrollButtons { Container = Pane, BackSelector = ".back", ForwardSelector = ".forward", BackAction = "pane.scroll-back", ForwardAction = "pane.scroll-forward" },
                WheelAction = "pane.scroll",
            };

            // Act
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(options);

            // Assert
            var attach = Assert.Single(browser.Engine.Recorder.GetCalls("attach"));
            var buttons = FakeDomBrowser.GetOption(attach, "buttons")!;
            object? GetButtonOption(string name)
            {
                return InteropPayload.GetValue(buttons, name);
            }

            Assert.Multiple(
                () => Assert.Equal("inline", FakeDomBrowser.GetOption(attach, "axis")),
                () => Assert.Equal(".stop", FakeDomBrowser.GetOption(attach, "stops")),
                () => Assert.Equal(".frozen", FakeDomBrowser.GetOption(attach, "frozenSelector")),
                () => Assert.Equal("pane.scroll", FakeDomBrowser.GetOption(attach, "wheel")),
                () => Assert.Equal(Pane, GetButtonOption("container")),
                () => Assert.Equal(".back", GetButtonOption("backButtonSelector")),
                () => Assert.Equal(".forward", GetButtonOption("forwardButtonSelector")),
                () => Assert.Equal("pane.scroll-back", GetButtonOption("backAction")),
                () => Assert.Equal("pane.scroll-forward", GetButtonOption("forwardAction")));
        }

        [Fact]
        public async Task Should_refuse_a_wheel_for_a_container_stepping_along_the_block_axis()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var options = new ScrollInteractionOptions { Steps = new ScrollStepOptions { Axis = ScrollAxis.Block }, WheelAction = "pane.scroll" };

            // Act
            var exception = await Assert.ThrowsAsync<ArgumentException>(() => engine.GetElement(Viewport).Scroll.AttachAsync(options));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("options", exception.ParamName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }

    public sealed class Stepping
    {
        [Fact]
        public async Task Should_step_through_the_engine_with_no_interaction_attached_first()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var isMoving = await engine.GetElement(Viewport).Scroll.StepAsync(ScrollDirection.Back, new ScrollStepOptions { Axis = ScrollAxis.Block, StopSelector = ".stop" });

            // Assert
            var run = Assert.Single(browser.Engine.Recorder.GetCalls("run"));
            var options = run.Arguments[4]!;
            object? GetOption(string name)
            {
                return InteropPayload.GetValue(options, name);
            }

            Assert.Multiple(
                () => Assert.True(isMoving),
                () => Assert.Equal(new object?[] { "scroll", "step", Viewport, -1 }, run.Arguments.Take(4)),
                () => Assert.Equal("block", GetOption("axis")),
                () => Assert.Equal(".stop", GetOption("stops")),
                () => Assert.Equal(0, browser.CountImports(FakeDomBrowser.WatcherPath)));
        }

        [Fact]
        public async Task Should_report_a_step_that_did_not_move()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser(isMoving: false);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            var isMoving = await engine.GetElement(Viewport).Scroll.StepAsync(ScrollDirection.Forward, new ScrollStepOptions());

            // Assert
            Assert.False(isMoving);
        }

        [Fact]
        public async Task Should_release_no_owner_state_for_a_step_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await engine.GetElement(Viewport).Scroll.StepAsync(ScrollDirection.Forward, new ScrollStepOptions());

            // Act
            await watcher.DisposeAsync();

            // Assert
            Assert.Empty(browser.Engine.Recorder.GetCalls("release"));
        }
    }
}
