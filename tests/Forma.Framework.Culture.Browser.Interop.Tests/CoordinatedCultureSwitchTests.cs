namespace Forma.Framework.Culture.Browser.Interop.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CoordinatedCultureSwitchTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser, ICultureAddressing? addressing = null)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaCultureCoordination()
            .AddScoped(provider => new CoordinatedCultureSwitch(provider.GetRequiredService<CultureCoordinatorRegistration>(), addressing))
            .BuildServiceProvider();

    private static CultureCoordinatorRegistration GetRegistration(AsyncServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>();

    private static CoordinatedCultureSwitch GetSwitch(AsyncServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<CoordinatedCultureSwitch>();

    private static string ReadPair(AsyncServiceScope scope)
    {
        var state = scope.ServiceProvider.GetRequiredService<ICultureState>();

        return $"{state.Culture.Name}/{state.UICulture.Name} #{state.Version.Number}";
    }

    [Fact]
    public async Task Should_complete_once_the_requesting_state_has_applied_the_accepted_pair()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();

        // Act
        await GetSwitch(scope).SwitchAsync(German, Hebrew);

        // Assert
        Assert.Equal("de-DE/he #1", ReadPair(scope));
    }

    [Fact]
    public async Task Should_take_the_culture_as_the_UI_culture_when_none_is_given()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();

        // Act
        await GetSwitch(scope).SwitchAsync(Hebrew);

        // Assert
        Assert.Equal("he/he #1", ReadPair(scope));
    }

    [Fact]
    public async Task Should_reach_every_registration_on_the_page()
    {
        // Arrange — two runtimes on one page
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await using var otherScope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();
        await GetRegistration(otherScope).RegisterAsync();

        // Act
        await GetSwitch(scope).SwitchAsync(Hebrew);

        // Assert
        Assert.Equal(["he/he #1", "he/he #1"], [ReadPair(scope), ReadPair(otherScope)]);
    }

    [Fact]
    public async Task Should_fail_at_once_without_a_registration()
    {
        // Arrange — a static render, whose scope never registers
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(Hebrew));

        // Assert
        Assert.Multiple(
            () => Assert.IsType<InvalidOperationException>(exception),
            () => Assert.Equal("en-US/en-US #0", ReadPair(scope)),
            () => Assert.Equal(0, browser.Accepted.Number));
    }

    [Fact]
    public async Task Should_fail_at_once_while_the_connection_is_down()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();
        GetRegistration(scope).MarkUnavailable();

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(Hebrew));

        // Assert
        Assert.Multiple(
            () => Assert.IsType<InvalidOperationException>(exception),
            () => Assert.Equal(0, browser.Accepted.Number));
    }

    [Fact]
    public async Task Should_wait_for_a_registration_under_way()
    {
        // Arrange — a registration whose module is still loading
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        browser.Runtime.Recorder.Hold("import");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        var registering = GetRegistration(scope).RegisterAsync();

        // Act
        var switching = GetSwitch(scope).SwitchAsync(Hebrew);
        var isCompletedBeforeRegistration = switching.IsCompleted;
        Assert.Single(browser.Runtime.Recorder.GetCalls("import")).Complete(browser.Module);
        await registering;
        await switching;

        // Assert
        Assert.Multiple(
            () => Assert.False(isCompletedBeforeRegistration),
            () => Assert.Equal("he/he #1", ReadPair(scope)));
    }

    [Fact]
    public async Task Should_fail_with_the_registration_it_waits_for()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        browser.Runtime.Recorder.Hold("import");
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        var registering = GetRegistration(scope).RegisterAsync();
        var switching = GetSwitch(scope).SwitchAsync(Hebrew);

        // Act
        Assert.Single(browser.Runtime.Recorder.GetCalls("import")).Fail(new JSDisconnectedException("The circuit dropped."));
        var exception = await Record.ExceptionAsync(() => switching);

        // Assert
        Assert.Multiple(
            () => Assert.IsType<JSDisconnectedException>(exception),
            () => Assert.True(registering.IsFaulted),
            () => Assert.Equal(0, browser.Accepted.Number));
    }

    [Fact]
    public async Task Should_fail_where_the_page_refuses_the_switch()
    {
        // Arrange — a cookie the coordinator couldn't read back
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();
        browser.FailNext("request", new JSException("The culture cookie couldn't be read back."));

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(Hebrew));

        // Assert
        Assert.Multiple(
            () => Assert.IsType<JSException>(exception),
            () => Assert.Equal("en-US/en-US #0", ReadPair(scope)));
    }

    [Fact]
    public async Task Should_refuse_a_pair_no_culture_route_carries_before_requesting_anything()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser, new HebrewAddressing());
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(German, Hebrew));

        // Assert
        Assert.Multiple(
            () => Assert.Contains("'de-DE' with the UI culture 'he'", Assert.IsType<InvalidOperationException>(exception).Message, StringComparison.Ordinal),
            () => Assert.Empty(browser.GetRegistration(browser.Identities.Single()).Recorder.GetCalls("request")),
            () => Assert.Equal("en-US/en-US #0", ReadPair(scope)));
    }

    [Fact]
    public async Task Should_request_a_pair_a_culture_route_carries()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser, new HebrewAddressing());
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();

        // Act
        await GetSwitch(scope).SwitchAsync(Hebrew);

        // Assert
        Assert.Equal("he/he #1", ReadPair(scope));
    }

    [Fact]
    public async Task Should_complete_as_canceled_where_its_navigation_did_not_land()
    {
        // Arrange
        using var fixture = new CultureFixture("en-US");
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser, new HebrewAddressing());
        await using var scope = provider.CreateAsyncScope();
        await GetRegistration(scope).RegisterAsync();
        browser.AnswerNextRequest("canceled");

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(Hebrew));

        // Assert
        Assert.Multiple(
            () => Assert.IsAssignableFrom<OperationCanceledException>(exception),
            () => Assert.Equal("en-US/en-US #0", ReadPair(scope)));
    }

    [Fact]
    public async Task Should_reject_a_missing_culture()
    {
        // Arrange
        var browser = new FakeCultureCoordinatorBrowser();
        await using var provider = CreateServices(browser);
        await using var scope = provider.CreateAsyncScope();

        // Act
        var exception = await Record.ExceptionAsync(() => GetSwitch(scope).SwitchAsync(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    // Culture routing that carries the Hebrew pair alone.
    private sealed class HebrewAddressing : ICultureAddressing
    {
        public bool CanAddress(CultureInfo culture, CultureInfo uiCulture)
            => culture.Name == "he" && uiCulture.Name == "he";

        public string GetDocumentBase(string root, CultureInfo culture, CultureInfo uiCulture)
            => root;

        public CultureRouteMap GetMap(string root)
            => throw new NotSupportedException();
    }
}
