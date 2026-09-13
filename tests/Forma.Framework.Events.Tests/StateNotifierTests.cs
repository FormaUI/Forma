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
