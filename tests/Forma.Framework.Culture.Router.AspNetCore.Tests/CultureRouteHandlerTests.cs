namespace Forma.Framework.Culture.Router.AspNetCore.Tests;

using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureRouteHandlerTests
{
    private static ServiceProvider CreateServices(FakeNavigationManager navigationManager)
        => new ServiceCollection()
            .AddSingleton<NavigationManager>(navigationManager)
            .AddSingleton<IJSRuntime>(new RecordingJSRuntime((_, _) => null))
            .AddFormaCultureRouting(new CultureRouterOptions
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

    public sealed class Handling
    {
        [Theory]
        [InlineData("en", "http://localhost/", "http://localhost/he/about", "he")]
        [InlineData("he", "http://localhost/he/", "http://localhost/about", "en")]
        [InlineData("he", "http://localhost/app/he/", "http://localhost/app/about?tab=2", "en")]
        [InlineData("en", "http://localhost/app/", "http://localhost/APP/he/about", "he")]
        [InlineData("he", "http://localhost/app/", "http://localhost/app", "en")]
        [InlineData("he", "http://localhost/app/", "http://localhost/app?tab=2", "en")]
        [InlineData("he", "http://localhost/", "http://localhost/he/%2e%2e/about", "en")]
        [InlineData("en", "http://localhost/", "http://localhost/h%65/%41bout", "he")]
        public async Task Should_set_the_culture_state_from_the_address_inside_the_applications_root(string connection, string baseAddress, string address, string culture)
        {
            // Arrange
            using var fixture = new CultureFixture(connection);
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(baseAddress)));
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            await scope.ServiceProvider.GetRequiredService<ICultureRouteHandler>().HandleAsync(new Uri(address));

            // Assert
            Assert.Multiple(
                () => Assert.Equal(culture, cultureState.Culture.Name),
                () => Assert.Equal(culture, cultureState.UICulture.Name));
        }

        [Theory]
        [InlineData("http://localhost/app/", "http://localhost/he/about")]
        [InlineData("http://localhost/app/", "http://localhost/apple/he/about")]
        [InlineData("http://localhost/", "http://example.com/he/about")]
        [InlineData("http://localhost/", "https://localhost/he/about")]
        [InlineData("http://localhost/", "http://localhost:5001/he/about")]
        public async Task Should_leave_the_culture_state_at_an_address_of_another_application(string baseAddress, string address)
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            await using var provider = CreateServices(new FakeNavigationManager(new Uri(baseAddress), new Uri(baseAddress)));
            await using var scope = provider.CreateAsyncScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();

            // Act
            await scope.ServiceProvider.GetRequiredService<ICultureRouteHandler>().HandleAsync(new Uri(address));

            // Assert
            Assert.Equal(0, cultureState.Revision);
        }

        [Fact]
        public async Task Should_change_the_culture_state_without_navigating()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var navigationManager = new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/about"));
            await using var provider = CreateServices(navigationManager);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await scope.ServiceProvider.GetRequiredService<ICultureRouteHandler>().HandleAsync(new Uri("http://localhost/he/about"));

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he", scope.ServiceProvider.GetRequiredService<ICultureState>().UICulture.Name),
                () => Assert.Empty(navigationManager.Navigations));
        }

        [Fact]
        public async Task Should_refuse_a_relative_address()
        {
            // Arrange
            await using var provider = CreateServices(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/")));
            await using var scope = provider.CreateAsyncScope();
            var cultureRouteHandler = scope.ServiceProvider.GetRequiredService<ICultureRouteHandler>();

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => cultureRouteHandler.HandleAsync(new Uri("he/about", UriKind.Relative)));
        }
    }
}
