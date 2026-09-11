namespace Forma.Framework.Input.Interop.Tests;

using Forma.Framework.Core.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class InputScopeTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    [Fact]
    public async Task Should_hold_the_scope_active_once_the_call_ran()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var handle = new FakeJSObjectReference((_, _) => null);
        await using var scope = new InputScope(handle, CreateEnvironment(loggerFactory));

        // Act
        var ran = await scope.ActivateAsync();

        // Assert
        Assert.True(ran);
        Assert.True(scope.IsActive);
        Assert.Empty(loggerFactory.Entries);
    }

    [Fact]
    public async Task Should_log_the_dropped_circuit_at_debug_and_leave_the_scope_inactive()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var handle = new FakeJSObjectReference((_, _) => throw new JSDisconnectedException("The circuit is gone."));
        await using var scope = new InputScope(handle, CreateEnvironment(loggerFactory));

        // Act
        var ran = await scope.ActivateAsync();

        // Assert
        Assert.False(ran);
        Assert.False(scope.IsActive);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Multiple(() =>
        {
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Contains("circuit dropped", entry.Message, StringComparison.Ordinal);
            Assert.Contains("activate", entry.Message, StringComparison.Ordinal);
            Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Should_log_a_dispose_the_script_threw_under_at_debug()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var handle = new FakeJSObjectReference((identifier, _) => identifier == "dispose" ? throw new JSException("dispose is not a function") : null);
        var scope = new InputScope(handle, CreateEnvironment(loggerFactory));
        await scope.ActivateAsync();

        // Act
        await scope.DisposeAsync();

        // Assert
        Assert.False(scope.IsActive);
        var entry = Assert.Single(loggerFactory.Entries);
        Assert.Multiple(() =>
        {
            Assert.Equal(LogLevel.Debug, entry.Level);
            Assert.Contains("threw", entry.Message, StringComparison.Ordinal);
            Assert.Contains("dispose", entry.Message, StringComparison.Ordinal);
            Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal);
        });
    }

    private static JSEnvironment CreateEnvironment(FakeLoggerFactory loggerFactory)
        => new(new FakeJSRuntime((_, _) => null), loggerFactory.CreateLogger("scope"), Owner);
}
