namespace Forma.Framework.Globalization.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
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
}
