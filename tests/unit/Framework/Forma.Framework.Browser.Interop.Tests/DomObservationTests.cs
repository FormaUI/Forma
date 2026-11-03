namespace Forma.Framework.Browser.Interop.Tests;

using System.Text.Json;

using Forma.Framework.Browser.Interop.Scrolling;
using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Browser;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class DomObservationTests
{
    private static readonly ElementReference Viewport = new("viewport");
    private static readonly ScrollWatchOptions Inline = new();
    private static readonly ScrollWatchOptions Block = new() { Axis = ScrollAxis.Block };
    private static readonly ScrollState Start = new(false, true, 0, 0, null);
    private static readonly ScrollState Middle = new(true, true, 0, 0, null);
    private static readonly ScrollState End = new(true, false, 0, 0, null);

    // The browser answers each attach with the start of the content unless a test says otherwise.
    private static FakeDomBrowser CreateBrowser(Func<object?>? attach = null)
        => new((path, identifier, _) => (path, identifier) switch
        {
            (FakeDomBrowser.WatcherPath, "attach") => attach is null ? Start : attach(),
            (FakeDomBrowser.WatcherPath, "refresh") => true,
            _ => null,
        });

    private static DomWatcher CreateWatcher(FakeDomBrowser browser, FakeLoggerFactory loggerFactory)
        => new(browser.CreateEnvironment(loggerFactory));

    private static IReadOnlyList<RecordedCall> GetAttaches(FakeDomBrowser browser)
        => browser.Watcher.Recorder.GetCalls("attach");

    public sealed class Establishing
    {
        [Fact]
        public async Task Should_take_the_first_state_from_the_attach_and_hand_it_to_the_handler()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();

            // Act
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);

            // Assert
            var attach = Assert.Single(GetAttaches(browser));
            Assert.Multiple(
                () => Assert.True(observation.IsActive),
                () => Assert.Equal(Start, observation.Current),
                () => Assert.Equal([Start], handled),
                () => Assert.Equal("scroll", FakeDomBrowser.GetRequestField(attach, "capability")),
                () => Assert.Equal(1, FakeDomBrowser.GetRequestField(attach, "generation")),
                () => Assert.Equal(Viewport, FakeDomBrowser.GetRequestField(attach, "target")));
        }

        [Fact]
        public async Task Should_hand_over_a_first_state_in_which_nothing_was_measured()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var fits = new ScrollState(false, false, 0, 0);
            var browser = CreateBrowser(() => fits);
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();

            // Act
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);

            // Assert
            Assert.Equal([fits], handled);
        }

        [Fact]
        public async Task Should_hand_over_the_first_state_before_its_owner_holds_the_observation()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            DomObservation<ScrollWatchOptions, ScrollState>? observation = null;
            bool? wasHeld = null;

            // Act
            observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { wasHeld = observation is not null; return Task.CompletedTask; }, Inline);
            await using var _ = observation;

            // Assert
            Assert.False(wasHeld);
        }

        [Fact]
        public async Task Should_stay_inactive_while_its_target_has_gone_and_try_again_on_the_next_sync()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            ScrollState? answer = null;
            var browser = CreateBrowser(() => answer);
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var wasActive = observation.IsActive;

            // Act
            answer = Start;
            await observation.SyncAsync(Viewport, Inline);

            // Assert
            Assert.Multiple(
                () => Assert.False(wasActive),
                () => Assert.True(observation.IsActive),
                () => Assert.Equal(2, GetAttaches(browser).Count));
        }

        [Fact]
        public async Task Should_log_an_attach_the_browser_failed_under_its_owner_and_try_it_again_on_the_next_sync()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var fails = true;
            var browser = CreateBrowser(() => fails ? throw new JSException("querySelectorAll: not a valid selector") : Start);
            var runtimeId = Guid.CreateVersion7();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, runtimeId));
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Act
            fails = false;
            await observation.SyncAsync(Viewport, Inline);

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Warning, entry.Level),
                () => Assert.Contains("attach scroll 1.1", entry.Message, StringComparison.Ordinal),
                () => Assert.Contains(runtimeId.ToString(), entry.Message, StringComparison.Ordinal),
                () => Assert.True(observation.IsActive));
        }

        [Fact]
        public async Task Should_leave_an_unrendered_element_alone()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);

            // Act
            await using var observation = await watcher.GetElement(default).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Assert
            Assert.Multiple(
                () => Assert.False(observation.IsActive),
                () => Assert.Empty(browser.Runtime.Recorder.Calls));
        }
    }

    public sealed class Barrier
    {
        [Fact]
        public async Task Should_hold_a_state_arriving_before_the_attach_result_until_the_first_is_handed_over()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Watcher.Recorder.Hold("attach");
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var watching = watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            var attach = Assert.Single(GetAttaches(browser));

            // Act
            var early = FakeDomBrowser.DeliverAsync(attach, Middle);
            var wasHeld = !early.IsCompleted;
            attach.Complete(Start);
            await using var observation = await watching;
            await early;

            // Assert
            Assert.Multiple(
                () => Assert.True(wasHeld),
                () => Assert.Equal([Start, Middle], handled),
                () => Assert.Equal(Middle, observation.Current));
        }

        [Fact]
        public async Task Should_keep_only_the_latest_state_held()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Watcher.Recorder.Hold("attach");
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var watching = watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            var attach = Assert.Single(GetAttaches(browser));

            // Act
            var earlier = FakeDomBrowser.DeliverAsync(attach, Middle);
            var latest = FakeDomBrowser.DeliverAsync(attach, End);
            var wasEarlierDropped = earlier.IsCompleted;
            attach.Complete(Start);
            await using var observation = await watching;
            await latest;

            // Assert
            Assert.Multiple(
                () => Assert.True(wasEarlierDropped),
                () => Assert.Equal([Start, End], handled));
        }

        [Fact]
        public async Task Should_release_a_held_state_when_the_attach_fails_and_deliver_after_a_retry()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            browser.Watcher.Recorder.Hold("attach");
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var watching = watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            var failed = Assert.Single(GetAttaches(browser));
            var early = FakeDomBrowser.DeliverAsync(failed, Middle);

            // Act
            failed.Complete(null);
            await using var observation = await watching;
            await early;
            browser.Watcher.Recorder.Release("attach");
            await observation.SyncAsync(Viewport, Inline);
            await FakeDomBrowser.DeliverAsync(GetAttaches(browser)[^1], End);

            // Assert
            Assert.Equal([Start, End], handled);
        }

        [Fact]
        public async Task Should_release_a_held_state_when_disposed_during_an_attach_and_detach_what_lands()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            browser.Watcher.Recorder.Hold("attach");
            var syncing = observation.SyncAsync(Viewport, Block);
            var attach = GetAttaches(browser)[^1];
            var early = FakeDomBrowser.DeliverAsync(attach, Middle);

            // Act
            await observation.DisposeAsync();
            await early;
            attach.Complete(End);
            await syncing;

            // Assert
            var detach = Assert.Single(browser.Watcher.Recorder.GetCalls("detach"));
            Assert.Multiple(
                () => Assert.Equal([Start], handled),
                () => Assert.False(observation.IsActive),
                () => Assert.Null(observation.Current),
                () => Assert.Equal(3, detach.Arguments[2]));
        }
    }

    public sealed class Settling
    {
        [Fact]
        public async Task Should_make_no_call_for_a_request_equal_to_the_one_established()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Act
            await observation.SyncAsync(new ElementReference("viewport"), new ScrollWatchOptions());

            // Assert
            Assert.Single(browser.Watcher.Recorder.Calls);
        }

        [Fact]
        public async Task Should_attach_again_under_a_new_generation_when_its_options_change()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);

            // Act
            await observation.SyncAsync(Viewport, Block);

            // Assert
            var second = GetAttaches(browser)[^1];
            Assert.Multiple(
                () => Assert.Equal(2, GetAttaches(browser).Count),
                () => Assert.Equal(2, FakeDomBrowser.GetRequestField(second, "generation")),
                () => Assert.Equal("block", FakeDomBrowser.GetOption(second, "axis")),
                () => Assert.Equal([Start, Start], handled));
        }

        [Fact]
        public async Task Should_join_one_settle_and_skip_a_request_superseded_while_another_was_applied()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            browser.Watcher.Recorder.Hold("attach");
            var first = observation.SyncAsync(Viewport, Block);
            var held = GetAttaches(browser)[^1];
            browser.Watcher.Recorder.Release("attach");

            // Act
            var second = observation.SyncAsync(Viewport, new ScrollWatchOptions { FrozenSelector = ".pinned" });
            var latest = observation.SyncAsync(Viewport, new ScrollWatchOptions { FrozenSelector = ".frozen" });
            held.Complete(Middle);
            await Task.WhenAll(first, second, latest);

            // Assert
            var attaches = GetAttaches(browser);
            Assert.Multiple(
                () => Assert.Equal(3, attaches.Count),
                () => Assert.Equal(".frozen", FakeDomBrowser.GetOption(attaches[^1], "frozen")),
                () => Assert.Same(first, second),
                () => Assert.Same(first, latest),
                () => Assert.True(observation.IsActive));
        }

        [Fact]
        public async Task Should_release_and_clear_its_state_once_it_is_no_longer_to_observe()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Act
            await observation.SyncAsync(Viewport, Inline, shouldObserve: false);

            // Assert
            var detach = Assert.Single(browser.Watcher.Recorder.GetCalls("detach"));
            Assert.Multiple(
                () => Assert.False(observation.IsActive),
                () => Assert.Null(observation.Current),
                () => Assert.Equal(2, detach.Arguments[2]));
        }

        [Fact]
        public async Task Should_take_a_fresh_first_state_when_it_is_to_observe_again()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            await observation.SyncAsync(Viewport, Inline, shouldObserve: false);

            // Act
            await observation.SyncAsync(Viewport, Inline);

            // Assert
            Assert.Multiple(
                () => Assert.True(observation.IsActive),
                () => Assert.Equal(3, FakeDomBrowser.GetRequestField(GetAttaches(browser)[^1], "generation")),
                () => Assert.Equal([Start, Start], handled));
        }

        [Fact]
        public async Task Should_attach_afresh_to_a_new_element()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var replacement = new ElementReference("replacement");

            // Act
            await observation.SyncAsync(replacement, Inline);

            // Assert
            Assert.Equal(replacement, FakeDomBrowser.GetRequestField(GetAttaches(browser)[^1], "target"));
        }

        [Fact]
        public async Task Should_make_no_call_once_disposed()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            await observation.DisposeAsync();
            var calls = browser.Watcher.Recorder.Calls.Count;

            // Act
            await observation.SyncAsync(Viewport, Block);
            await observation.RefreshAsync();
            await observation.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.True(observation.IsDisposed),
                () => Assert.Equal(calls, browser.Watcher.Recorder.Calls.Count));
        }
    }

    public sealed class Delivering
    {
        [Fact]
        public async Task Should_drop_a_state_from_an_earlier_generation()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(state => { handled.Add(state); return Task.CompletedTask; }, Inline);
            var earlier = GetAttaches(browser)[0];
            await observation.SyncAsync(Viewport, Block);

            // Act
            await FakeDomBrowser.DeliverAsync(earlier, End);

            // Assert
            Assert.Multiple(
                () => Assert.Equal([Start, Start], handled),
                () => Assert.Equal(Start, observation.Current));
        }

        [Fact]
        public async Task Should_hand_states_over_one_at_a_time_in_the_order_measured()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            var handled = new List<ScrollState>();
            var busy = new TaskCompletionSource();
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(
                async state =>
                {
                    handled.Add(state);

                    if (state == Middle)
                    {
                        await busy.Task;
                    }
                },
                Inline);
            var attach = GetAttaches(browser)[0];

            // Act
            var middle = FakeDomBrowser.DeliverAsync(attach, Middle);
            var end = FakeDomBrowser.DeliverAsync(attach, End);
            var handledWhileBusy = handled.ToList();
            busy.SetResult();
            await Task.WhenAll(middle, end);

            // Assert
            Assert.Multiple(
                () => Assert.Equal([Start, Middle], handledWhileBusy),
                () => Assert.Equal([Start, Middle, End], handled));
        }

        [Fact]
        public async Task Should_log_a_handler_that_threw_under_its_owner_and_registration_and_hand_over_later_states()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            var runtimeId = Guid.CreateVersion7();
            await using var watcher = new DomWatcher(browser.CreateEnvironment(loggerFactory, runtimeId));
            var handled = new List<ScrollState>();
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(
                state =>
                {
                    handled.Add(state);

                    return state == Start ? throw new InvalidOperationException("The render failed.") : Task.CompletedTask;
                },
                Inline);

            // Act
            await FakeDomBrowser.DeliverAsync(GetAttaches(browser)[0], End);

            // Assert
            var entry = Assert.Single(loggerFactory.Entries);
            Assert.Multiple(
                () => Assert.Equal(LogLevel.Error, entry.Level),
                () => Assert.Contains("scroll handler", entry.Message, StringComparison.Ordinal),
                () => Assert.Contains("registration 1, generation 1", entry.Message, StringComparison.Ordinal),
                () => Assert.Contains(runtimeId.ToString(), entry.Message, StringComparison.Ordinal),
                () => Assert.Equal([Start, End], handled));
        }

        [Fact]
        public async Task Should_dispose_from_inside_its_handler_without_waiting_for_it()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            DomObservation<ScrollWatchOptions, ScrollState>? observation = null;
            var handled = new List<ScrollState>();
            observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(
                async state =>
                {
                    handled.Add(state);

                    if (state == End)
                    {
                        await observation!.DisposeAsync();
                    }
                },
                Inline);
            var attach = GetAttaches(browser)[0];

            // Act
            await FakeDomBrowser.DeliverAsync(attach, End);
            await FakeDomBrowser.DeliverAsync(attach, Middle);

            // Assert
            Assert.Multiple(
                () => Assert.True(observation.IsDisposed),
                () => Assert.Single(browser.Watcher.Recorder.GetCalls("detach")),
                () => Assert.Equal([Start, End], handled));
        }

        [Fact]
        public async Task Should_drop_a_state_for_a_registration_it_does_not_hold()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var receiver = ((DotNetObjectReference<DomReceiver>)GetAttaches(browser)[0].Arguments[1]!).Value;

            // Act
            await receiver.NotifyAsync("99", 1, new JSTransfer { Value = JsonSerializer.SerializeToElement(End, JsonSerializerOptions.Web) });

            // Assert
            Assert.Equal(Start, observation.Current);
        }

        [Fact]
        public async Task Should_release_unread_a_streamed_state_for_a_registration_it_does_not_hold()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var receiver = ((DotNetObjectReference<DomReceiver>)GetAttaches(browser)[0].Arguments[1]!).Value;
            await using var stream = new FakeJSStreamReference(JsonSerializer.SerializeToUtf8Bytes(End, JsonSerializerOptions.Web));

            // Act
            await receiver.NotifyAsync("99", 1, new JSTransfer { Stream = stream });

            // Assert
            Assert.Multiple(
                () => Assert.Null(stream.OpenedWith),
                () => Assert.True(stream.IsDisposed));
        }

        [Fact]
        public async Task Should_take_a_state_streamed_past_the_transfer_s_budget()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var attach = GetAttaches(browser)[0];
            var receiver = ((DotNetObjectReference<DomReceiver>)attach.Arguments[1]!).Value;
            await using var stream = new FakeJSStreamReference(JsonSerializer.SerializeToUtf8Bytes(End, JsonSerializerOptions.Web));

            // Act
            await receiver.NotifyAsync((string)FakeDomBrowser.GetRequestField(attach, "token")!, (int)FakeDomBrowser.GetRequestField(attach, "generation")!, new JSTransfer { Stream = stream });

            // Assert
            Assert.Equal(End, observation.Current);
        }

        [Fact]
        public async Task Should_drop_and_log_a_state_larger_than_it_accepts()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);
            var attach = GetAttaches(browser)[0];
            var receiver = ((DotNetObjectReference<DomReceiver>)attach.Arguments[1]!).Value;
            await using var stream = new FakeJSStreamReference([], length: JSTransferOptions.Default.MaximumLength + 1);

            // Act
            await receiver.NotifyAsync((string)FakeDomBrowser.GetRequestField(attach, "token")!, (int)FakeDomBrowser.GetRequestField(attach, "generation")!, new JSTransfer { Stream = stream });

            // Assert
            Assert.Multiple(
                () => Assert.Equal(Start, observation.Current),
                () => Assert.Contains(loggerFactory.Entries, entry => entry.Message.Contains("was lost on its way", StringComparison.Ordinal)));
        }
    }

    public sealed class Refreshing
    {
        [Fact]
        public async Task Should_ask_the_browser_to_measure_afresh_under_its_registration()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            var browser = CreateBrowser();
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Act
            await observation.RefreshAsync();

            // Assert
            var refresh = Assert.Single(browser.Watcher.Recorder.GetCalls("refresh"));
            Assert.Equal(new object?[] { "1", 1 }, refresh.Arguments.Skip(1));
        }

        [Fact]
        public async Task Should_establish_again_once_refreshed_while_inactive()
        {
            // Arrange
            using var loggerFactory = new FakeLoggerFactory();
            ScrollState? answer = null;
            var browser = CreateBrowser(() => answer);
            await using var watcher = CreateWatcher(browser, loggerFactory);
            await using var observation = await watcher.GetElement(Viewport).Scroll.WatchAsync(_ => Task.CompletedTask, Inline);

            // Act
            answer = Middle;
            await observation.RefreshAsync();

            // Assert
            Assert.Multiple(
                () => Assert.True(observation.IsActive),
                () => Assert.Equal(Middle, observation.Current),
                () => Assert.Empty(browser.Watcher.Recorder.GetCalls("refresh")));
        }
    }
}
