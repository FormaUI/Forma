namespace Forma.Framework.Culture.Router.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureSwitcherTests
{
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager, CultureSwitchMode cultureSwitchMode = CultureSwitchMode.FullLoad)
        => new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddSingleton<IJSRuntime>(new RecordingJSRuntime((_, _) => null))
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureRouting(new CultureRouterOptions
            {
                Prefix = CulturePrefix.ExceptDefault,
                Switch = cultureSwitchMode,
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
            await scope.ServiceProvider.GetRequiredService<ICultureSwitch>().SwitchAsync(Hebrew);
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

        [Fact]
        public async Task Should_navigate_in_the_circuit_where_a_switch_is_made_in_place_and_leave_the_culture_state_to_follow()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/about"));
            await using var provider = CreateServices(navigationManager, CultureSwitchMode.InPlace);
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            scope.ServiceProvider.GetRequiredService<CultureSwitcher>().SwitchTo(GetRoute(scope.ServiceProvider, "he"));

            // Assert
            var navigation = Assert.Single(navigationManager.Navigations);

            Assert.Multiple(
                () => Assert.Equal("http://localhost/he/about", navigation.Uri),
                () => Assert.False(navigation.Options.ForceLoad),
                () => Assert.Equal("en", cultureState.UICulture.Name));
        }
    }
}
