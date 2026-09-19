namespace Forma.Framework.JavaScript.Interop.Tests;

using Forma.Framework.JavaScript.Interop;

using Microsoft.JSInterop;

using Xunit;

public sealed class UnavailableJSRuntimeTests
{
    [Fact]
    public void Should_be_one_instance()
    {
        // Act
        IJSRuntime first = UnavailableJSRuntime.Instance;
        IJSRuntime second = UnavailableJSRuntime.Instance;

        // Assert
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Should_refuse_every_call_naming_the_function()
    {
        // Arrange
        var runtime = UnavailableJSRuntime.Instance;

        // Act
        var plain = await Assert.ThrowsAsync<JSRuntimeUnavailableException>(async () => await runtime.InvokeAsync<int>("measure", []));
        var cancellable = await Assert.ThrowsAsync<JSRuntimeUnavailableException>(async () => await runtime.InvokeAsync<int>("measure", CancellationToken.None, []));

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Contains("'measure'", plain.Message, StringComparison.Ordinal);
            Assert.Contains("'measure'", cancellable.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public async Task Should_fail_as_an_invalid_operation_for_a_host_catching_broadly()
    {
        // Arrange
        var runtime = UnavailableJSRuntime.Instance;

        // Act
        var exception = await Assert.ThrowsAsync<JSRuntimeUnavailableException>(async () => await runtime.InvokeAsync<int>("measure", []));

        // Assert
        Assert.IsAssignableFrom<InvalidOperationException>(exception);
    }
}
