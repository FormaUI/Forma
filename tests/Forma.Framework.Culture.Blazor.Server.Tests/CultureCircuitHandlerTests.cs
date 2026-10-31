namespace Forma.Framework.Culture.Blazor.Server.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureCircuitHandlerTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he-IL");

    private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser? browser = null)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>((browser ?? new FakeCultureCoordinatorBrowser()).Runtime)
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureServer()
            .BuildServiceProvider();

    // The circuit is not read by the handler, and a test cannot make one.
    private static Task ConnectAsync(IServiceProvider services)
        => Assert.Single(services.GetServices<CircuitHandler>()).OnConnectionUpAsync(null!, TestContext.Current.CancellationToken);

    // The circuit is not read by the handler, and a test cannot make one.
    private static Task DisconnectAsync(IServiceProvider services)
        => Assert.Single(services.GetServices<CircuitHandler>()).OnConnectionDownAsync(null!, TestContext.Current.CancellationToken);

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
        var cultureSwitch = scope.ServiceProvider.GetRequiredService<ICultureSwitch>();
        CultureInfo? culture = null;
        CultureInfo? uiCulture = null;

        // Act
        await RunActivityAsync(scope.ServiceProvider, () => cultureSwitch.SwitchAsync(German, Hebrew));
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
        var cultureSwitch = scope.ServiceProvider.GetRequiredService<ICultureSwitch>();
        await RunActivityAsync(scope.ServiceProvider, () => cultureSwitch.SwitchAsync(German, Hebrew));

        // Act
        await RunActivityAsync(scope.ServiceProvider, () => Task.CompletedTask);

        // Assert
        Assert.Multiple(
            () => Assert.Equal("en-US", CultureInfo.CurrentCulture.Name),
            () => Assert.Equal("en-US", CultureInfo.CurrentUICulture.Name));
    }

    [Fact]
    public async Task Should_adopt_the_pair_the_page_accepted_as_the_connection_comes_up()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser("he-IL");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();

        // Act
        await ConnectAsync(scope.ServiceProvider);

        // Assert
        Assert.Equal(Hebrew, scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture);
    }

    [Fact]
    public async Task Should_complete_the_first_connection_once_its_registration_completes()
    {
        // Arrange — the browser holding the coordinator's module, which the registration imports
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser("he-IL");
        browser.Runtime.Recorder.Hold("import");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();

        // Act
        var connecting = ConnectAsync(scope.ServiceProvider);
        var isCompletedBeforeTheBrowserAnswers = connecting.IsCompleted;
        Assert.Single(browser.Runtime.Recorder.GetCalls("import")).Complete(browser.Module);
        await connecting;

        // Assert
        Assert.False(isCompletedBeforeTheBrowserAnswers);
        Assert.Equal(Hebrew, scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture);
    }

    [Fact]
    public async Task Should_complete_a_later_connection_before_the_browser_answers_its_registration()
    {
        // Arrange — the first connection registered and dropped, then the browser holding its answer to the next registration,
        // as it holds every answer while the reconnection that asks is under way
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser("he-IL");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await ConnectAsync(scope.ServiceProvider);
        await DisconnectAsync(scope.ServiceProvider);
        browser.Module.Recorder.Hold("register");

        // Act
        var connecting = ConnectAsync(scope.ServiceProvider);
        var isCompletedBeforeTheBrowserAnswers = connecting.IsCompletedSuccessfully;
        var registrations = browser.Module.Recorder.GetCalls("register");
        registrations[^1].Fail(new JSDisconnectedException("The test answers the registration to let the scope go."));

        // Assert
        Assert.True(isCompletedBeforeTheBrowserAnswers);
        Assert.Equal(2, registrations.Count);
    }

    [Fact]
    public async Task Should_register_again_on_a_later_connection_after_an_expected_failure()
    {
        // Arrange — the first connection drops while the module is imported
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser("he-IL");
        browser.FailNext("import", new JSDisconnectedException("The circuit dropped."));
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await ConnectAsync(scope.ServiceProvider);

        // Act
        await ConnectAsync(scope.ServiceProvider);

        // Assert
        Assert.Multiple(
            () => Assert.Single(browser.Identities),
            () => Assert.Equal(Hebrew, scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture));
    }

    [Fact]
    public async Task Should_fail_a_switch_at_once_while_the_connection_is_down()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaCultureServer()
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await ConnectAsync(scope.ServiceProvider);

        // Act
        await DisconnectAsync(scope.ServiceProvider);
        var exception = await Record.ExceptionAsync(() => scope.ServiceProvider.GetRequiredService<ICultureSwitch>().SwitchAsync(German));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }
}
