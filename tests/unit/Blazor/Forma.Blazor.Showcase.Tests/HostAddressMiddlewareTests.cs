namespace Forma.Blazor.Showcase.Tests;

using Microsoft.AspNetCore.Http;

public sealed class HostAddressMiddlewareTests
{
    [Theory]
    [InlineData("localhost:5080")]
    [InlineData("host.docker.internal:5080")]
    [InlineData("[::1]:5080")]
    [InlineData("localhost")]
    public async Task Should_pass_on_a_request_whose_host_forms_an_address(string host)
    {
        // Arrange
        var reachedNext = false;
        var middleware = new HostAddressMiddleware(_ => { reachedNext = true; return Task.CompletedTask; });
        var context = CreateContext(host);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.True(reachedNext),
            () => Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode));
    }

    [Theory]
    [InlineData("")]
    [InlineData("localhost:99999")]
    [InlineData("$(id)")]
    public async Task Should_refuse_a_request_whose_host_forms_no_address(string host)
    {
        // Arrange
        var reachedNext = false;
        var middleware = new HostAddressMiddleware(_ => { reachedNext = true; return Task.CompletedTask; });
        var context = CreateContext(host);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.False(reachedNext),
            () => Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode));
    }

    private static DefaultHttpContext CreateContext(string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString(host);
        context.Request.Path = "/text-box";

        return context;
    }
}
