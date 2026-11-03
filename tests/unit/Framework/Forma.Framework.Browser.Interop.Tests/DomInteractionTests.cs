namespace Forma.Framework.Browser.Interop.Tests;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Input;
using Forma.Framework.Input.Interop;
using Forma.Tests.Browser;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

public sealed class DomInteractionTests
{
    private const string InputRouterPath = "./_content/forma/input/scripts/InputRouter.js";

    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ScrollInteractionOptions Wheel = new() { WheelAction = "pane.scroll" };
    private static readonly ScrollInteractionOptions Steps = new() { Steps = new ScrollStepOptions { StopSelector = ".stop" } };

    private static FakeDomBrowser CreateBrowser(Func<bool>? attach = null)
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.EnginePath, "attach") => attach?.Invoke() ?? true,
            _ => null,
        });

    public sealed class Attaching
    {
        [Fact]
        public async Task Should_carry_the_input_policy_to_the_browser_before_declaring_routes()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var router = new RecordingJSObjectReference((_, _) => null);
            var browser = CreateBrowser(() => router.Recorder.GetCalls("configure").Count == 1);
            browser.Serve(InputRouterPath, router);
            await using var inputRouter = new InputRouter(browser.Runtime, NullLogger<InputRouter>.Instance, InputRouterOptions.Default);
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance, inputRouter);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            await using var engine = new DomEngine(watcher);

            // Act
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Wheel);

            // Assert
            Assert.True(interaction.IsActive);
        }

        [Fact]
        public async Task Should_carry_no_policy_for_an_interaction_that_declares_no_route()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            await using var router = new RecordingJSObjectReference((_, _) => null);
            var browser = CreateBrowser();
            browser.Serve(InputRouterPath, router);
            await using var inputRouter = new InputRouter(browser.Runtime, NullLogger<InputRouter>.Instance, InputRouterOptions.Default);
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance, inputRouter);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), bridge);
            await using var engine = new DomEngine(watcher);

            // Act
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps);

            // Assert
            Assert.Multiple(
                () => Assert.True(interaction.IsActive),
                () => Assert.Empty(router.Recorder.Calls));
        }

        [Fact]
        public async Task Should_make_no_call_while_it_is_not_to_be_enabled()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);

            // Act
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Wheel, shouldEnable: false);

            // Assert
            Assert.Multiple(
                () => Assert.False(interaction.IsActive),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_stay_inactive_where_the_browser_attached_nothing_and_try_again_on_the_next_sync()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var isAttached = false;
            var browser = CreateBrowser(() => isAttached);
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps);
            var wasActive = interaction.IsActive;

            // Act
            isAttached = true;
            await interaction.SyncAsync(Viewport, Steps);

            // Assert
            Assert.Multiple(
                () => Assert.False(wasActive),
                () => Assert.True(interaction.IsActive),
                () => Assert.Equal(2, browser.Engine.Recorder.GetCalls("attach").Count));
        }
    }

    public sealed class Settling
    {
        [Fact]
        public async Task Should_make_no_call_for_a_request_equal_to_the_one_attached()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps);

            // Act
            await interaction.SyncAsync(Viewport, new ScrollInteractionOptions { Steps = new ScrollStepOptions { StopSelector = ".stop" } });

            // Assert
            Assert.Single(browser.Engine.Recorder.Calls);
        }

        [Fact]
        public async Task Should_attach_again_in_one_call_when_its_options_change()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps);

            // Act
            await interaction.SyncAsync(Viewport, Steps with { WheelAction = "pane.scroll" });

            // Assert
            var attaches = browser.Engine.Recorder.GetCalls("attach");
            Assert.Multiple(
                () => Assert.Equal(2, browser.Engine.Recorder.Calls.Count),
                () => Assert.Equal("pane.scroll", FakeDomBrowser.GetOption(attaches[^1], "wheel")),
                () => Assert.Equal(2, FakeDomBrowser.GetRequestField(attaches[^1], "generation")));
        }

        [Fact]
        public async Task Should_detach_once_it_is_no_longer_to_be_enabled()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            await using var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps);

            // Act
            await interaction.SyncAsync(Viewport, Steps, shouldEnable: false);

            // Assert
            var detach = Assert.Single(browser.Engine.Recorder.GetCalls("detach"));
            Assert.Multiple(
                () => Assert.False(interaction.IsActive),
                () => Assert.Equal(2, detach.Arguments[2]));
        }

        [Fact]
        public async Task Should_detach_what_an_attach_still_in_flight_leaves_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Engine.Recorder.Hold("attach");
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await using var engine = new DomEngine(watcher);
            var interaction = await engine.GetElement(Viewport).Scroll.AttachAsync(Steps, shouldEnable: false);
            var enabling = interaction.SyncAsync(Viewport, Steps);
            var attach = Assert.Single(browser.Engine.Recorder.GetCalls("attach"));

            // Act
            await interaction.DisposeAsync();
            attach.Complete(true);
            await enabling;

            // Assert
            var detach = Assert.Single(browser.Engine.Recorder.GetCalls("detach"));
            Assert.Multiple(
                () => Assert.False(interaction.IsActive),
                () => Assert.Equal(2, detach.Arguments[2]));
        }
    }
}
