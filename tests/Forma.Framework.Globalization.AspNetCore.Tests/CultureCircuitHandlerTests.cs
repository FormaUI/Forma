namespace Forma.Framework.Globalization.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Globalization;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureCircuitHandlerTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he-IL");

    private static ServiceProvider CreateServices(FakeDocumentLanguageBrowser? browser = null)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>((browser ?? new FakeDocumentLanguageBrowser()).Runtime)
            .AddFormaGlobalizationServices()
            .BuildServiceProvider();

    // The circuit is not read by the handler, and a test cannot make one.
    private static Task ConnectAsync(IServiceProvider services)
        => Assert.Single(services.GetServices<CircuitHandler>()).OnConnectionUpAsync(null!, TestContext.Current.CancellationToken);

    // The context is not read by the handler, and a test cannot make one.
    private static Task RunActivityAsync(IServiceProvider services, Func<Task> activity)
        => Assert.Single(services.GetServices<CircuitHandler>()).CreateInboundActivityHandler(_ => activity())(null!);

    [Fact]
    public async Task Should_run_the_activity_after_a_switch_in_the_new_cultures()
    {
        // Arrange — the connection's culture, which every activity's flow starts from
        using var fixture = new CultureFixture("en-US");
        await using var provider = CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();
        CultureInfo? culture = null;
        CultureInfo? uiCulture = null;

        // Act
        await RunActivityAsync(scope.ServiceProvider, () => cultureState.ChangeAsync(German, Hebrew));
        await RunActivityAsync(scope.ServiceProvider, () =>
        {
            culture = CultureInfo.CurrentCulture;
            uiCulture = CultureInfo.CurrentUICulture;

            return Task.CompletedTask;
        });

        // Assert
        Assert.Multiple(
            () => Assert.Equal(German, culture),
            () => Assert.Equal(Hebrew, uiCulture));
    }

    [Fact]
    public async Task Should_leave_the_dispatching_flows_cultures_in_place()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        await using var provider = CreateServices();
        await using var scope = provider.CreateAsyncScope();
        var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();
        await RunActivityAsync(scope.ServiceProvider, () => cultureState.ChangeAsync(German, Hebrew));

        // Act
        await RunActivityAsync(scope.ServiceProvider, () => Task.CompletedTask);

        // Assert
        Assert.Multiple(
            () => Assert.Equal("en-US", CultureInfo.CurrentCulture.Name),
            () => Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name));
    }

    [Fact]
    public async Task Should_keep_the_document_on_the_state_from_the_connection_up()
    {
        // Arrange
        using var fixture = new CultureFixture("he-IL");
        var browser = new FakeDocumentLanguageBrowser("en", "ltr");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();

        // Act
        await ConnectAsync(scope.ServiceProvider);

        // Assert
        Assert.Equal(("he-IL", "rtl"), browser.Document);
    }

    [Fact]
    public async Task Should_start_the_document_language_on_a_later_connection_after_an_expected_failure()
    {
        // Arrange — the first connection drops while the module is imported
        using var fixture = new CultureFixture("he-IL");
        var browser = new FakeDocumentLanguageBrowser("en", "ltr");
        browser.FailNext("import", new JSDisconnectedException("The circuit dropped."));
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await ConnectAsync(scope.ServiceProvider);

        // Act
        await ConnectAsync(scope.ServiceProvider);

        // Assert
        Assert.Equal(("he-IL", "rtl"), browser.Document);
    }
}
