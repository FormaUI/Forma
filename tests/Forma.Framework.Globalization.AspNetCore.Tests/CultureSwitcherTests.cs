namespace Forma.Framework.Globalization.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;

public sealed class CultureSwitcherTests
{
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager)
        => new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddCultureRouting(new CultureRouterOptions
            {
                Prefix = CulturePrefix.ExceptDefault,
                DefaultCulture = "en",
                Routes =
                [
                    new() { Token = "en", Culture = new("en") },
                    new() { Token = "he", Culture = new("he") }
                ]
            })
            .BuildServiceProvider();

    private static CultureRoute GetRoute(IServiceProvider services, string token)
        => services.GetRequiredService<CultureRouter>().Routes.Single(route => route.Token == token);

    // The circuit is not read by the handler, and a test cannot make one.
    private static Task OpenCircuitAsync(IServiceProvider services)
        => Assert.Single(services.GetServices<CircuitHandler>()).OnCircuitOpenedAsync(null!, TestContext.Current.CancellationToken);

    public sealed class CurrentRoute
    {
        [Fact]
        public async Task Should_be_the_route_of_the_culture_states_ui_culture_whatever_the_ambient_culture()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/about")));
            await using var scope = provider.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ICultureState>().ChangeAsync(Hebrew);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en");

            // Act
            var route = scope.ServiceProvider.GetRequiredService<CultureSwitcher>().CurrentRoute;

            // Assert
            Assert.Equal("he", route.Token);
        }
    }

    public sealed class Switching
    {
        [Theory]
        [InlineData("http://localhost/", "http://localhost/about?tab=2", "he", "http://localhost/he/about?tab=2")]
        [InlineData("http://localhost/", "http://localhost/he/about#states", "en", "http://localhost/about")]
        [InlineData("http://localhost/", "http://localhost/he", "en", "http://localhost/")]
        [InlineData("http://localhost/app/", "http://localhost/app/about", "he", "http://localhost/app/he/about")]
        [InlineData("http://localhost/he/", "http://localhost/he/about?tab=2", "en", "http://localhost/about?tab=2")]
        [InlineData("http://localhost/app/he/", "http://localhost/app/he/about", "en", "http://localhost/app/about")]
        [InlineData("http://localhost/app/he/", "http://localhost/app/he/", "en", "http://localhost/app/")]
        [InlineData("http://localhost/HE/", "http://localhost/HE/about", "he", "http://localhost/he/about")]
        public async Task Should_give_the_current_pages_address_in_another_culture(string baseAddress, string address, string token, string expected)
        {
            // Arrange
            using var fixture = new CultureFixture();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(address)));
            await using var scope = provider.CreateAsyncScope();
            var switcher = scope.ServiceProvider.GetRequiredService<CultureSwitcher>();

            // Act
            var switched = switcher.GetUri(GetRoute(scope.ServiceProvider, token));

            // Assert
            Assert.Equal(expected, switched.AbsoluteUri);
        }

        [Theory]
        [InlineData("http://localhost/", "he", "composer?tab=2", "http://localhost/he/composer?tab=2")]
        [InlineData("http://localhost/app/he/", "en", "composer", "http://localhost/app/composer")]
        [InlineData("http://localhost/app/he/", "he", "en/composer", "http://localhost/app/he/composer")]
        [InlineData("http://localhost/app/", "he", "", "http://localhost/app/he")]
        public async Task Should_give_an_address_from_the_applications_root_in_a_culture(string baseAddress, string token, string relativeAddress, string expected)
        {
            // Arrange
            using var fixture = new CultureFixture();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(baseAddress)));
            await using var scope = provider.CreateAsyncScope();
            var switcher = scope.ServiceProvider.GetRequiredService<CultureSwitcher>();

            // Act
            var built = switcher.GetUri(GetRoute(scope.ServiceProvider, token), relativeAddress);

            // Assert
            Assert.Equal(expected, built.AbsoluteUri);
        }

        [Fact]
        public async Task Should_load_the_address_and_leave_the_culture_state_to_follow_it()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/about"));
            await using var provider = CreateServices(navigationManager);
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            scope.ServiceProvider.GetRequiredService<CultureSwitcher>().SwitchTo(GetRoute(scope.ServiceProvider, "he"));

            // Assert
            var navigation = Assert.Single(navigationManager.Navigations);

            Assert.Multiple(
                () => Assert.Equal("http://localhost/he/about", navigation.Uri),
                () => Assert.True(navigation.Options.ForceLoad),
                () => Assert.Equal("en", cultureState.UICulture.Name));
        }
    }

    public sealed class Reconciliation
    {
        [Theory]
        [InlineData("en", "http://localhost/", "http://localhost/he/about?tab=2", "he")]
        [InlineData("he", "http://localhost/", "http://localhost/about", "en")]
        [InlineData("he", "http://localhost/", "http://localhost/", "en")]
        [InlineData("en", "http://localhost/", "http://localhost/HE/about", "he")]
        [InlineData("en", "http://localhost/app/", "http://localhost/app/he/about#states", "he")]
        [InlineData("en", "http://localhost/he/", "http://localhost/he/about", "he")]
        [InlineData("en", "http://localhost/app/he/", "http://localhost/app/he/", "he")]
        [InlineData("he", "http://localhost/app/", "http://localhost/app/about", "en")]
        public async Task Should_set_the_culture_state_from_the_address_a_circuit_opens_at(string connection, string baseAddress, string address, string culture)
        {
            // Arrange — the culture the circuit's connection arrived with, from the cookie another tab may have written
            using var fixture = new CultureFixture(connection);
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(address)));
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            await OpenCircuitAsync(scope.ServiceProvider);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(culture, cultureState.Culture.Name),
                () => Assert.Equal(culture, cultureState.UICulture.Name));
        }

        [Fact]
        public async Task Should_leave_the_culture_state_at_an_address_naming_two_consecutive_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("he");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/en/he/about")));
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            await OpenCircuitAsync(scope.ServiceProvider);

            // Assert
            Assert.Equal(0, cultureState.Revision);
        }
    }
}
