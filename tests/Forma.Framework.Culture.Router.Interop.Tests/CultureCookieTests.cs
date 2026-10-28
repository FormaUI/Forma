namespace Forma.Framework.Culture.Router.Interop.Tests;

using Forma.Framework.JavaScript.Interop;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

using Xunit;

public sealed class CultureCookieTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();

    private static JSEnvironment CreateEnvironment(IJSRuntime js, ILoggerFactory loggerFactory)
        => new(js, loggerFactory.CreateLogger<CultureCookieTests>(), Owner);

    [Fact]
    public async Task Should_write_the_cookie_through_its_module()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var module = new RecordingJSObjectReference((_, _) => null);
        var js = new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null);
        await using var cultureCookie = new CultureCookie(CreateEnvironment(js, loggerFactory));

        // Act
        var isWritten = await cultureCookie.WriteAsync("forma-culture-route%2Fapp", "he", "/app", TimeSpan.FromDays(365), isSecure: true);

        // Assert
        Assert.Multiple(
            () => Assert.True(isWritten),
            () => Assert.Equal(["./_content/forma/culture-router/scripts/CultureCookie.js"], Assert.Single(js.Recorder.GetCalls("import")).Arguments),
            () => Assert.Equal(["forma-culture-route%2Fapp", "he", "/app", 31_536_000L, true], Assert.Single(module.Recorder.GetCalls("write")).Arguments),
            () => Assert.Empty(loggerFactory.Entries));
    }

    [Fact]
    public async Task Should_report_a_cookie_the_dropped_circuit_left_unwritten_and_log_it_at_debug()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var module = new FakeJSObjectReference((_, _) => throw new JSDisconnectedException("The circuit is gone."));
        await using var cultureCookie = new CultureCookie(CreateEnvironment(FakeJSRuntime.Importing(module), loggerFactory));

        // Act
        var isWritten = await cultureCookie.WriteAsync("forma-culture-route", "he", "/", TimeSpan.FromDays(365), isSecure: false);

        // Assert
        var entry = Assert.Single(loggerFactory.Entries);

        Assert.Multiple(
            () => Assert.False(isWritten),
            () => Assert.Equal(LogLevel.Debug, entry.Level),
            () => Assert.Contains(Owner.ToString(), entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_release_its_module()
    {
        // Arrange
        using var loggerFactory = new FakeLoggerFactory();
        await using var module = new RecordingJSObjectReference((_, _) => null);
        var js = new RecordingJSRuntime((identifier, _) => identifier == "import" ? module : null);
        var cultureCookie = new CultureCookie(CreateEnvironment(js, loggerFactory));
        await cultureCookie.WriteAsync("forma-culture-route", "he", "/", TimeSpan.FromDays(365), isSecure: false);

        // Act
        await cultureCookie.DisposeAsync();

        // Assert
        Assert.Equal(1, module.Releases);
    }
}
