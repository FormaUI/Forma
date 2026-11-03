namespace Forma.Framework.JavaScript.Interop.Tests;

using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

using Xunit;

public sealed class JSEnvironmentTests
{
    [Fact]
    public async Task Should_forward_calls_to_the_runtime_it_wraps()
    {
        // Arrange
        var runtime = new FakeJSRuntime((identifier, args) => identifier == "measure" ? (int)args![0]! * 2 : null);
        var environment = new JSEnvironment(runtime, NullLogger.Instance, Guid.CreateVersion7());

        // Act
        var plain = await environment.InvokeAsync<int>("measure", [21]);
        var cancellable = await environment.InvokeAsync<int>("measure", CancellationToken.None, [4]);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(42, plain);
            Assert.Equal(8, cancellable);
        });
    }

    [Fact]
    public void Should_carry_the_identity_and_log_it_was_given()
    {
        // Arrange
        var runtimeId = Guid.CreateVersion7();
        var logger = NullLogger.Instance;

        // Act
        var environment = new JSEnvironment(new FakeJSRuntime((_, _) => null), logger, runtimeId);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(runtimeId, environment.RuntimeId);
            Assert.Same(logger, environment.Logger);
        });
    }

    [Fact]
    public void Should_refuse_a_missing_runtime_or_log()
    {
        // Arrange
        var runtime = new FakeJSRuntime((_, _) => null);

        // Act
        var withoutRuntime = Assert.Throws<ArgumentNullException>(() => new JSEnvironment(null!, NullLogger.Instance, Guid.CreateVersion7()));
        var withoutLogger = Assert.Throws<ArgumentNullException>(() => new JSEnvironment(runtime, null!, Guid.CreateVersion7()));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("runtime", withoutRuntime.ParamName);
            Assert.Equal("logger", withoutLogger.ParamName);
        });
    }
}
