namespace Forma.Framework.Culture.Blazor.WebAssembly.Tests;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Framework.Culture.Router;
using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.JSInterop;

public sealed class ServiceCollectionExtensionsTests
{
    private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser, CultureArrangement arrangement = CultureArrangement.Coordinated, CultureSwitchPolicy? policy = null, bool isRouted = false)
    {
        var services = new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddSingleton<NavigationManager>(new FakeNavigationManager(new Uri("https://example.com/app/"), new Uri("https://example.com/app/")));

        if (isRouted)
        {
            services.AddFormaCultureRouting(new CultureRouterOptions { DefaultCulture = "en", Routes = [new() { Token = "en", UICulture = new("en") }] });
        }

        return services.AddFormaCultureWebAssembly(arrangement, policy).BuildServiceProvider();
    }

    // The runtime starts its hosted services from its root provider, apart from the scope its components resolve from.
    private static async Task StartAsync(ServiceProvider provider)
    {
        foreach (var service in provider.GetServices<IHostedService>())
        {
            await service.StartAsync(TestContext.Current.CancellationToken);
        }
    }

    private static object GetStart(FakeCultureCoordinatorBrowser browser)
        => Assert.Single(browser.Module.Recorder.GetCalls("register")).Arguments[3]!;

    private static IReadOnlyDictionary<string, string> GetStartAttributes(FakeCultureCoordinatorBrowser browser, string name)
        => (IReadOnlyDictionary<string, string>)InteropPayload.GetValue(GetStart(browser), name)!;

    private static string GetDefaults()
        => $"{CultureInfo.DefaultThreadCurrentCulture?.Name}/{CultureInfo.DefaultThreadCurrentUICulture?.Name}";

    public sealed class Coordinated : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        [Fact]
        public async Task Should_register_the_runtime_with_the_page_s_coordinator_as_it_starts_and_adopt_the_page_s_pair()
        {
            // Arrange — a runtime that starts in English, on a page the server rendered in Hebrew text with German formats
            var browser = new FakeCultureCoordinatorBrowser("de-DE", "he");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await StartAsync(provider);

            // Assert
            var state = scope.ServiceProvider.GetRequiredService<ICultureState>();
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal("de-DE/he", GetDefaults()));
        }

        [Fact]
        public async Task Should_carry_the_runtime_s_pair_and_the_state_policy_for_a_page_no_server_published()
        {
            // Arrange
            using var hebrew = new CultureFixture("he");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await StartAsync(provider);

            // Assert
            var marker = GetStartAttributes(browser, "marker");
            Assert.Multiple(
                () => Assert.Equal("he/he", $"{marker["data-culture"]}/{marker["data-ui-culture"]}"),
                () => Assert.Equal("state", Assert.Single(GetStartAttributes(browser, "policy")).Value),
                () => Assert.Equal("rtl", InteropPayload.GetValue(GetStart(browser), "direction")));
        }

        [Fact]
        public async Task Should_carry_the_policy_the_client_gives()
        {
            // Arrange
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, policy: new CultureSwitchPolicy { Persistence = CultureSwitchPersistence.Cookie, CookieName = "culture" });
            await using var scope = provider.CreateAsyncScope();

            // Act
            await StartAsync(provider);

            // Assert
            var policy = GetStartAttributes(browser, "policy");
            Assert.Equal("cookie culture", $"{policy["content"]} {policy["data-name"]}");
        }

        [Fact]
        public async Task Should_carry_the_address_policy_under_the_application_s_root_where_culture_addresses_are_registered()
        {
            // Arrange
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, isRouted: true);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await StartAsync(provider);

            // Assert
            var policy = GetStartAttributes(browser, "policy");
            Assert.Equal("address /app/", $"{policy["content"]} {policy["data-root"]}");
        }

        [Fact]
        public async Task Should_go_on_in_the_pair_it_started_in_where_the_registration_fails()
        {
            // Arrange
            var browser = new FakeCultureCoordinatorBrowser("he");
            browser.FailNext("register", new JSException("The page carries no valid culture marker."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            var exception = await Record.ExceptionAsync(() => StartAsync(provider));

            // Assert
            Assert.Multiple(
                () => Assert.Null(exception),
                () => Assert.Equal("en-US", scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture.Name));
        }

        [Fact]
        public async Task Should_switch_through_the_page_s_coordinator_and_set_the_process_s_defaults_to_the_accepted_pair()
        {
            // Arrange
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            await StartAsync(provider);

            // Act
            await scope.ServiceProvider.GetRequiredService<ICultureSwitch>().SwitchAsync(CultureInfo.GetCultureInfo("de-DE"), CultureInfo.GetCultureInfo("he"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(("de-DE", "he", 1), browser.Accepted),
                () => Assert.Equal("de-DE/he", GetDefaults()));
        }
    }

    public sealed class Independent : IDisposable
    {
        private readonly ProcessCultures _cultures = new();

        public void Dispose()
            => _cultures.Dispose();

        [Fact]
        public async Task Should_switch_the_runtime_alone_and_set_the_process_s_defaults_to_its_pair()
        {
            // Arrange
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser, CultureArrangement.Independent);
            await using var scope = provider.CreateAsyncScope();
            await StartAsync(provider);

            // Act
            await scope.ServiceProvider.GetRequiredService<ICultureSwitch>().SwitchAsync(CultureInfo.GetCultureInfo("he"));

            // Assert
            var state = scope.ServiceProvider.GetRequiredService<ICultureState>();
            Assert.Multiple(
                () => Assert.Equal("he/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal("he/he", $"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name}"),
                () => Assert.Equal("he/he", GetDefaults()),
                () => Assert.Empty(browser.Runtime.Recorder.GetCalls("import")));
        }

        [Fact]
        public void Should_reject_a_switch_policy()
        {
            // Act
            var exception = Record.Exception(() => new ServiceCollection().AddFormaCultureWebAssembly(CultureArrangement.Independent, CultureSwitchPolicy.Default));

            // Assert
            Assert.IsType<InvalidOperationException>(exception);
        }
    }

    public sealed class Arrangement
    {
        [Fact]
        public void Should_reject_an_arrangement_it_does_not_name()
        {
            // Act
            var exception = Record.Exception(() => new ServiceCollection().AddFormaCultureWebAssembly((CultureArrangement)7));

            // Assert
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        }
    }

    /// <summary>Holds the test's flow in English and restores the process's default cultures, which an application sets.</summary>
    private sealed class ProcessCultures : IDisposable
    {
        private readonly CultureInfo? _defaultCulture = CultureInfo.DefaultThreadCurrentCulture;
        private readonly CultureInfo? _defaultUICulture = CultureInfo.DefaultThreadCurrentUICulture;
        private readonly CultureFixture _fixture = new("en-US");

        public void Dispose()
        {
            CultureInfo.DefaultThreadCurrentCulture = _defaultCulture;
            CultureInfo.DefaultThreadCurrentUICulture = _defaultUICulture;
            _fixture.Dispose();
        }
    }
}
