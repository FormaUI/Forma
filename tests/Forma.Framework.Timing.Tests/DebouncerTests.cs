namespace Forma.Framework.Timing.Tests;

using Microsoft.Extensions.Time.Testing;

using Xunit;

public sealed class DebouncerTests
{
    private static readonly TimeSpan Delay = TimeSpan.FromMilliseconds(100);

    private static (Debouncer Debouncer, FakeTimeProvider Clock) Create()
    {
        var clock = new FakeTimeProvider();

        return (new Debouncer { Clock = clock }, clock);
    }

    [Fact]
    public async Task Should_run_the_action_once_the_delay_has_passed()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var runs = 0;

        // Act
        var request = debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        var beforeTheDelay = runs;
        clock.Advance(Delay);
        await request;

        // Assert
        Assert.Multiple(
            () => Assert.Equal(0, beforeTheDelay),
            () => Assert.Equal(1, runs));
    }

    [Fact]
    public async Task Should_run_only_the_latest_request_when_another_arrives_in_time()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var ran = new List<string>();

        // Act
        var first = debouncer.DebounceAsync(_ => { ran.Add("first"); return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        clock.Advance(Delay / 2);
        var second = debouncer.DebounceAsync(_ => { ran.Add("second"); return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        await first;
        clock.Advance(Delay);
        await second;

        // Assert
        Assert.Equal(["second"], ran);
    }

    [Fact]
    public async Task Should_run_nothing_after_cancel()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var runs = 0;

        // Act
        var request = debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        debouncer.Cancel();
        clock.Advance(Delay);
        await request;

        // Assert
        Assert.Equal(0, runs);
    }

    [Fact]
    public async Task Should_run_a_request_with_no_delay_at_once()
    {
        // Arrange
        var (debouncer, _) = Create();
        var runs = 0;

        // Act
        await debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, TimeSpan.Zero, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, runs);
    }

    [Fact]
    public async Task Should_accept_a_new_request_after_one_has_run()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var runs = 0;
        var first = debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        clock.Advance(Delay);
        await first;

        // Act
        var second = debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, Delay, TestContext.Current.CancellationToken);
        clock.Advance(Delay);
        await second;

        // Assert
        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task Should_cancel_a_running_action_when_a_new_request_arrives()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var firstStarted = new TaskCompletionSource();
        var firstToken = CancellationToken.None;

        // Act
        var first = debouncer.DebounceAsync(async token =>
        {
            firstToken = token;
            firstStarted.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }, Delay, TestContext.Current.CancellationToken);
        clock.Advance(Delay);
        await firstStarted.Task;
        var second = debouncer.DebounceAsync(_ => Task.CompletedTask, Delay, TestContext.Current.CancellationToken);
        await first;
        clock.Advance(Delay);
        await second;

        // Assert
        Assert.True(firstToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Should_cancel_a_running_action_when_cancelled()
    {
        // Arrange
        var (debouncer, clock) = Create();
        var started = new TaskCompletionSource();
        var actionToken = CancellationToken.None;

        // Act
        var request = debouncer.DebounceAsync(async token =>
        {
            actionToken = token;
            started.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, token);
        }, Delay, TestContext.Current.CancellationToken);
        clock.Advance(Delay);
        await started.Task;
        debouncer.Cancel();
        await request;

        // Assert
        Assert.True(actionToken.IsCancellationRequested);
    }

    [Fact]
    public async Task Should_abandon_a_waiting_request_when_the_caller_cancels()
    {
        // Arrange
        var (debouncer, clock) = Create();
        using var caller = new CancellationTokenSource();
        var runs = 0;

        // Act
        var request = debouncer.DebounceAsync(_ => { runs++; return Task.CompletedTask; }, Delay, caller.Token);
        await caller.CancelAsync();
        clock.Advance(Delay);
        await request;

        // Assert
        Assert.Equal(0, runs);
    }

    [Fact]
    public async Task Should_reject_a_null_action_or_a_negative_delay()
    {
        // Arrange
        var (debouncer, _) = Create();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await debouncer.DebounceAsync(null!, Delay, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () => await debouncer.DebounceAsync(_ => Task.CompletedTask, TimeSpan.FromMilliseconds(-1), TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Should_measure_on_the_system_clock_unless_given_another()
        => Assert.Same(TimeProvider.System, new Debouncer().Clock);
}
