namespace Forma.Framework.Globalization.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

public sealed class CultureCircuitHandlerTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he-IL");

    private static ServiceProvider CreateServices()
        => new ServiceCollection().AddFormaGlobalizationServices().BuildServiceProvider();

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
}
