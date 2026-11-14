namespace Forma.Framework.Culture.Router.Tests;

using System.Globalization;

using Forma.Framework.Culture.Browser.Interop;
using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureSwitcherTests
{
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager, CultureSwitchMode cultureSwitchMode = CultureSwitchMode.FullLoad, ICultureDestinationResolver? destinationResolver = null)
        => new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddDestinationResolver(destinationResolver)
            .AddSingleton<IJSRuntime>(new RecordingJSRuntime((_, _) => null))
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureRouting(new CultureRouterOptions
            {
                Prefix = CulturePrefix.ExceptDefault,
                Switch = cultureSwitchMode,
                DefaultCulture = "en",
                Routes =
                [
                    new() { Token = "en", UICulture = new("en") },
                    new() { Token = "he", UICulture = new("he") }
                ]
            })
            .AddFormaCultureCoordination()
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

        [Theory]
        [InlineData(CultureSwitchMode.FullLoad)]
        [InlineData(CultureSwitchMode.InPlace)]
        public async Task Should_request_the_routes_pair_through_the_culture_switch_and_navigate_nowhere_itself(CultureSwitchMode cultureSwitchMode)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/about"));
            await using var provider = CreateServices(navigationManager, cultureSwitchMode);
            await using var scope = provider.CreateAsyncScope();
            var cultureSwitch = (TestCultureSwitch)scope.ServiceProvider.GetRequiredService<ICultureSwitch>();

            // Act
            await scope.ServiceProvider.GetRequiredService<CultureSwitcher>().SwitchToAsync(GetRoute(scope.ServiceProvider, "he"));

            // Assert
            var request = Assert.Single(cultureSwitch.Requests);

            Assert.Multiple(
                () => Assert.Equal("he/he", $"{request.Culture.Name}/{request.UICulture.Name}"),
                () => Assert.Empty(navigationManager.Navigations));
        }
    }

    public sealed class Destinations
    {
        [Fact]
        public async Task Should_lead_to_the_current_pages_own_address_where_the_host_resolves_none()
        {
            // Arrange
            using var fixture = new CultureFixture();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/news/the-final?page=2#comments")));
            await using var scope = provider.CreateAsyncScope();
            var switcher = scope.ServiceProvider.GetRequiredService<CultureSwitcher>();
            var hebrew = GetRoute(scope.ServiceProvider, "he");

            // Act
            var destination = await switcher.GetDestinationAsync(hebrew, TestContext.Current.CancellationToken);
            var address = await switcher.GetDestinationUriAsync(hebrew, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(CultureDestination.Translated("news/the-final?page=2"), destination),
                () => Assert.Equal("http://localhost/he/news/the-final?page=2", address.AbsoluteUri));
        }

        [Theory]
        [InlineData("http://localhost/news/the-final", "he", false, "http://localhost/he/hadashot/hagmar")]
        [InlineData("http://localhost/he/hadashot/hagmar", "en", false, "http://localhost/news/the-final")]
        [InlineData("http://localhost/news/an-untranslated-story", "he", true, "http://localhost/he/hadashot")]
        public async Task Should_lead_to_the_destination_the_host_names(string address, string token, bool isFallback, string expected)
        {
            // Arrange
            using var fixture = new CultureFixture();
            var resolver = new NewsResolver();
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri(address)), destinationResolver: resolver);
            await using var scope = provider.CreateAsyncScope();
            var switcher = scope.ServiceProvider.GetRequiredService<CultureSwitcher>();
            var route = GetRoute(scope.ServiceProvider, token);

            // Act
            var destination = await switcher.GetDestinationAsync(route, TestContext.Current.CancellationToken);
            var destinationAddress = await switcher.GetDestinationUriAsync(route, TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Equal(isFallback, destination.IsFallback),
                () => Assert.Equal(expected, destinationAddress.AbsoluteUri));
        }

        // The one story both cultures carry, at an address of its own in each, and the news section where Hebrew lacks a story.
        private sealed class NewsResolver : ICultureDestinationResolver
        {
            public ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken)
                => ValueTask.FromResult<CultureDestination?>((relativeAddress, to.Token) switch
                {
                    ("news/the-final", "he") => CultureDestination.Translated("hadashot/hagmar"),
                    ("hadashot/hagmar", "en") => CultureDestination.Translated("news/the-final"),
                    (_, "he") => CultureDestination.Fallback("hadashot"),
                    _ => null
                });
        }
    }
}

// Registers the host's destination resolver where a test gives one.
file static class DestinationResolverRegistration
{
    public static IServiceCollection AddDestinationResolver(this IServiceCollection services, ICultureDestinationResolver? resolver)
        => resolver is null ? services : services.AddSingleton(resolver);
}
