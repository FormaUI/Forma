namespace Forma.Framework.Events.Tests;

using Xunit;

public sealed class StateNotifierTests
{
    [Fact]
    public async Task Should_invoke_subscribers_in_registration_order()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new List<string>();
        notifier.Subscribe(() => { calls.Add("first"); return Task.CompletedTask; });
        notifier.Subscribe(() => { calls.Add("second"); return Task.CompletedTask; });

        // Act
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first", "second"], calls);
    }

    [Fact]
    public async Task Should_stop_invoking_an_unsubscribed_callback()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = 0;
        Task Count() { calls++; return Task.CompletedTask; }
        notifier.Subscribe(Count);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Act
        notifier.Unsubscribe(Count);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Should_keep_a_callback_subscribed_again_in_its_place_once()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new List<string>();
        Task FirstAsync() { calls.Add("first"); return Task.CompletedTask; }
        notifier.Subscribe(FirstAsync);
        notifier.Subscribe(() => { calls.Add("second"); return Task.CompletedTask; });

        // Act
        notifier.Subscribe(FirstAsync);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);
        notifier.Unsubscribe(FirstAsync);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first", "second", "second"], calls);
    }

    [Fact]
    public async Task Should_keep_the_order_of_the_rest_when_one_unsubscribes()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new List<int>();
        var callbacks = Enumerable.Range(0, 5).Select(index => (Func<Task>)(() => { calls.Add(index); return Task.CompletedTask; })).ToArray();
        foreach (var callback in callbacks)
        {
            notifier.Subscribe(callback);
        }

        // Act
        notifier.Unsubscribe(callbacks[2]);
        notifier.Unsubscribe(callbacks[0]);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([1, 3, 4], calls);
    }

    [Fact]
    public async Task Should_let_a_callback_unsubscribe_while_the_notification_runs()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new List<string>();
        Task SecondAsync() { calls.Add("second"); return Task.CompletedTask; }
        notifier.Subscribe(() => { calls.Add("first"); notifier.Unsubscribe(SecondAsync); return Task.CompletedTask; });
        notifier.Subscribe(SecondAsync);

        // Act
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["first", "second", "first"], calls);
    }

    [Fact]
    public async Task Should_invoke_the_rest_when_one_fails_and_fault_with_every_failure()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new List<string>();
        notifier.Subscribe(() => throw new InvalidOperationException("first"));
        notifier.Subscribe(() => { calls.Add("second"); return Task.CompletedTask; });
        notifier.Subscribe(() => Task.FromException(new InvalidOperationException("third")));

        // Act
        var exception = await Assert.ThrowsAsync<AggregateException>(() => notifier.NotifyAsync(TestContext.Current.CancellationToken));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(["second"], calls),
            () => Assert.Equal(["first", "third"], exception.InnerExceptions.Select(failure => failure.Message)));
    }

    [Fact]
    public async Task Should_keep_every_subscription_made_from_threads_of_their_own()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = new int[1000];
        var callbacks = Enumerable.Range(0, calls.Length).Select(index => (Func<Task>)(() => { calls[index]++; return Task.CompletedTask; })).ToArray();

        // Act
        await Task.WhenAll(callbacks.Select(callback => Task.Run(() => notifier.Subscribe(callback), TestContext.Current.CancellationToken)));
        await Task.WhenAll(callbacks.Where((_, index) => index % 2 == 0).Select(callback => Task.Run(() => notifier.Unsubscribe(callback), TestContext.Current.CancellationToken)));
        await notifier.NotifyAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(Enumerable.Range(0, calls.Length).Select(index => index % 2), calls);
    }

    [Fact]
    public async Task Should_stop_at_a_canceled_token()
    {
        // Arrange
        var notifier = new StateNotifier();
        var calls = 0;
        notifier.Subscribe(() => { calls++; return Task.CompletedTask; });
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => notifier.NotifyAsync(cancellation.Token));
        Assert.Equal(0, calls);
    }
}
