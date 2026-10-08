namespace Forma.Blazor.Tests.Components;

using Forma.Blazor.Components;
using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.Localization;
using Forma.Tests.Browser;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class ComponentContextTests
{
    [Fact]
    public async Task Should_create_each_service_once_and_only_when_first_read()
    {
        // Arrange
        var localeCreations = 0;
        var loggerCreations = 0;
        var runtimeCreations = 0;
        await using var context = new ComponentContext(
            () => { runtimeCreations++; return new FakeJSRuntime((_, _) => null); },
            () => { localeCreations++; return CreateLocale(); },
            () => { loggerCreations++; return NullLogger.Instance; });

        // Act
        var before = (localeCreations, loggerCreations, runtimeCreations);
        _ = context.Locale;
        _ = context.Locale;
        _ = context.JS;
        _ = context.JS;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal((0, 0, 0), before);
            Assert.Equal(1, localeCreations);
            Assert.Equal(1, loggerCreations);
            Assert.Equal(1, runtimeCreations);
        });
    }

    [Fact]
    public async Task Should_give_its_environment_its_own_identity_and_log()
    {
        // Arrange
        var logger = NullLogger.Instance;
        await using var context = new ComponentContext(() => new FakeJSRuntime((_, _) => null), CreateLocale, () => logger);

        // Act
        var environment = context.JS;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.NotEqual(Guid.Empty, context.RuntimeId);
            Assert.Equal(7, context.RuntimeId.Version);
            Assert.Equal(context.RuntimeId, environment.RuntimeId);
            Assert.Same(logger, environment.Logger);
            Assert.Same(context.Logger, environment.Logger);
        });
    }

    [Fact]
    public async Task Should_mark_itself_disposed_and_take_another_disposal_safely()
    {
        // Arrange
        var context = new ComponentContext(CreateRuntime, CreateLocale, CreateLogger);
        var before = context.IsDisposed;

        // Act
        await context.DisposeAsync();
        await context.DisposeAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(before);
            Assert.True(context.IsDisposed);
        });
    }

    public sealed class WatcherAndEngine
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

        private static ComponentContext CreateContext(FakeDomBrowser browser, FakeLoggerFactory loggerFactory, DomBridge? bridge = null)
            => new(() => browser.Runtime, CreateLocale, () => loggerFactory.CreateLogger("Owner"), () => bridge);

        private static Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(ComponentContext context, Func<ScrollState, Task>? handler = null)
            => context.DomWatcher.GetElement(Viewport).Scroll.WatchAsync(handler ?? (_ => Task.CompletedTask), new ScrollWatchOptions());

        [Fact]
        public async Task Should_make_its_watcher_and_engine_once_each_and_only_when_first_read()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var runtimeCreations = 0;
            await using var context = new ComponentContext(() => { runtimeCreations++; return browser.Runtime; }, CreateLocale, () => loggerFactory.CreateLogger("Owner"));

            // Act
            var before = runtimeCreations;
            var engine = context.DomEngine;
            var watcher = context.DomWatcher;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(0, before),
                () => Assert.Equal(1, runtimeCreations),
                () => Assert.Same(engine, context.DomEngine),
                () => Assert.Same(watcher, context.DomWatcher),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_share_its_imports_through_the_bridge_it_is_given()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            await using var first = CreateContext(browser, loggerFactory, bridge);
            await using var second = CreateContext(browser, loggerFactory, bridge);

            // Act
            await WatchAsync(first);
            await WatchAsync(second);

            // Assert
            Assert.Equal(1, browser.CountImports(FakeDomBrowser.WatcherPath));
        }

        [Fact]
        public async Task Should_release_what_its_watcher_and_engine_hold_in_one_call()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var context = CreateContext(browser, loggerFactory);
            var observation = await WatchAsync(context);
            var interaction = await context.DomEngine.GetElement(Viewport).Scroll.AttachAsync(new ScrollInteractionOptions());
            var calls = browser.Watcher.Recorder.Calls.Count + browser.Engine.Recorder.Calls.Count;

            // Act
            await context.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Engine.Recorder.GetCalls("release")),
                () => Assert.Equal(calls + 1, browser.Watcher.Recorder.Calls.Count + browser.Engine.Recorder.Calls.Count),
                () => Assert.True(observation.IsDisposed),
                () => Assert.True(interaction.IsDisposed));
        }

        [Fact]
        public async Task Should_make_no_watcher_or_engine_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var context = CreateContext(browser, loggerFactory);
            await context.DisposeAsync();

            // Act
            var watcher = Assert.Throws<ObjectDisposedException>(() => context.DomWatcher);
            var engine = Assert.Throws<ObjectDisposedException>(() => context.DomEngine);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(typeof(ComponentContext).FullName, watcher.ObjectName),
                () => Assert.Equal(typeof(ComponentContext).FullName, engine.ObjectName),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }

        [Fact]
        public async Task Should_leave_no_session_in_the_browser_when_disposed_while_its_entry_loads()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Runtime.Recorder.Hold("import");
            await using var bridge = new DomBridge(browser.Runtime, NullLogger<DomBridge>.Instance);
            var context = CreateContext(browser, loggerFactory, bridge);
            var watching = WatchAsync(context);

            // Act
            await context.DisposeAsync();
            Assert.Single(browser.Runtime.Recorder.Calls).Complete(browser.Watcher);
            await using var observation = await watching;

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Watcher.Recorder.Calls),
                () => Assert.True(observation.IsDisposed));
        }

        [Fact]
        public async Task Should_release_an_observation_still_attaching_and_take_nothing_it_returns()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Watcher.Recorder.Hold("attach");
            var context = CreateContext(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var watching = WatchAsync(context, state => { handled.Add(state); return Task.CompletedTask; });

            // Act
            await context.DisposeAsync();
            Assert.Single(browser.Watcher.Recorder.GetCalls("attach")).Complete(Start);
            await using var observation = await watching;

            // Assert
            Assert.Multiple(
                () => Assert.Single(browser.Watcher.Recorder.GetCalls("release")),
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(handled));
        }

        [Fact]
        public async Task Should_end_its_release_when_the_browser_fails_it_and_log_the_failure()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser(() => throw new JSException("The owner's session is gone."));
            var context = CreateContext(browser, loggerFactory);
            var observation = await WatchAsync(context);

            // Act
            await context.DisposeAsync();

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.True(context.IsDisposed),
                () => Assert.True(observation.IsDisposed),
                () => Assert.Equal(LogLevel.Warning, entry.Level));
        }
    }

    [Fact]
    public void Should_refuse_a_missing_function()
    {
        // Act
        var withoutRuntime = Assert.Throws<ArgumentNullException>(() => new ComponentContext(null!, CreateLocale, CreateLogger));
        var withoutLocale = Assert.Throws<ArgumentNullException>(() => new ComponentContext(CreateRuntime, null!, CreateLogger));
        var withoutLogger = Assert.Throws<ArgumentNullException>(() => new ComponentContext(CreateRuntime, CreateLocale, null!));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("jsRuntime", withoutRuntime.ParamName);
            Assert.Equal("locale", withoutLocale.ParamName);
            Assert.Equal("logger", withoutLogger.ParamName);
        });
    }

    private static FakeJSRuntime CreateRuntime()
        => new((_, _) => null);

    private static IStringLocale CreateLocale()
        => NullStringLocaleFactory.Instance.Create(typeof(ComponentContextTests));

    private static NullLogger CreateLogger()
        => NullLogger.Instance;
}
