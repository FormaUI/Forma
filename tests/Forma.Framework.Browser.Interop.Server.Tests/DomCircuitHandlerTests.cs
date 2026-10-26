namespace Forma.Framework.Browser.Interop.Server.Tests;

using System.Text.Json;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Tests.Browser;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

using Xunit;

public sealed class DomCircuitHandlerTests
{
    private static readonly ElementReference Viewport = new("viewport");

    private static FakeDomBrowser CreateBrowser()
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => new ScrollState(false, true, 0, 0),
            _ => null,
        });

    private static ServiceProvider CreateServices(FakeDomBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaBrowserCircuitIntegration()
            .BuildServiceProvider();

    private static Task<DomObservation<ScrollWatchOptions, ScrollState>> WatchAsync(DomWatcher watcher, ElementReference element)
        => watcher.GetElement(element).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());

    // The circuit is not read by the handler, and a test cannot make one.
    private static Task ConnectAsync(IServiceProvider services)
        => Assert.Single(services.GetServices<CircuitHandler>()).OnConnectionUpAsync(null!, TestContext.Current.CancellationToken);

    public sealed class Reconnecting
    {
        [Fact]
        public async Task Should_call_nothing_while_nothing_is_observed()
        {
            // Arrange
            var browser = CreateBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await ConnectAsync(scope.ServiceProvider);

            // Assert
            Assert.Empty(browser.Runtime.Recorder.Calls);
        }

        [Fact]
        public async Task Should_refresh_every_established_observation_of_the_circuit_in_one_call()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var bridge = scope.ServiceProvider.GetRequiredService<DomBridge>();
            var first = Guid.CreateVersion7();
            var second = Guid.CreateVersion7();
            await using var firstWatcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, first), bridge);
            await using var secondWatcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, second), bridge);
            await WatchAsync(firstWatcher, Viewport);
            await WatchAsync(firstWatcher, new ElementReference("other"));
            await WatchAsync(secondWatcher, Viewport);

            // Act
            await ConnectAsync(scope.ServiceProvider);

            // Assert
            var refresh = Assert.Single(browser.Watcher.Recorder.GetCalls("refreshAll"));
            Assert.Equal(
                $$"""[{"runtimeKey":"{{first}}","registrations":[{"token":"1","generation":1},{"token":"2","generation":1}]},{"runtimeKey":"{{second}}","registrations":[{"token":"1","generation":1}]}]""",
                JsonSerializer.Serialize(refresh.Arguments[0]));
        }

        [Fact]
        public async Task Should_name_each_observation_under_its_current_generation_and_leave_out_a_released_one()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var runtimeId = Guid.CreateVersion7();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, runtimeId), scope.ServiceProvider.GetRequiredService<DomBridge>());
            var replaced = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            var released = await watcher.GetElement(new ElementReference("other")).Scroll.WatchAsync(_ => Task.CompletedTask, new ScrollWatchOptions());
            await replaced.SyncAsync(Viewport, new ScrollWatchOptions { Axis = ScrollAxis.Block });
            await released.DisposeAsync();

            // Act
            await ConnectAsync(scope.ServiceProvider);

            // Assert
            var refresh = Assert.Single(browser.Watcher.Recorder.GetCalls("refreshAll"));
            Assert.Equal($$"""[{"runtimeKey":"{{runtimeId}}","registrations":[{"token":"1","generation":2}]}]""", JsonSerializer.Serialize(refresh.Arguments[0]));
        }

        [Fact]
        public async Task Should_call_nothing_for_an_owner_released_before_the_connection_returned()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory), scope.ServiceProvider.GetRequiredService<DomBridge>());
            await WatchAsync(watcher, Viewport);
            await watcher.DisposeAsync();

            // Act
            await ConnectAsync(scope.ServiceProvider);

            // Assert
            Assert.Empty(browser.Watcher.Recorder.GetCalls("refreshAll"));
        }

        [Fact]
        public async Task Should_leave_a_watcher_with_a_bridge_of_its_own_to_its_own_refresh()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory));
            await WatchAsync(watcher, Viewport);

            // Act
            await ConnectAsync(scope.ServiceProvider);

            // Assert
            Assert.Empty(browser.Watcher.Recorder.GetCalls("refreshAll"));
        }
    }
}
