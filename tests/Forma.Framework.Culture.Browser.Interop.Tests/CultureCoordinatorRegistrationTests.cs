namespace Forma.Framework.Culture.Browser.Interop.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class CultureCoordinatorRegistrationTests
{
    private static ServiceProvider CreateServices(FakeCultureCoordinatorBrowser browser)
        => new ServiceCollection()
            .AddSingleton<IJSRuntime>(browser.Runtime)
            .AddFormaCultureCoordination()
            .BuildServiceProvider();

    private static CultureCoordinatorRegistration GetRegistration(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<CultureCoordinatorRegistration>();

    private static ICultureState GetCultureState(IServiceScope scope)
        => scope.ServiceProvider.GetRequiredService<ICultureState>();

    public sealed class Registering
    {
        [Fact]
        public async Task Should_adopt_the_pair_the_page_accepted()
        {
            // Arrange — a scope that starts in English, on a page the server rendered in Hebrew text with German formats
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser("de-DE", "he");
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            await GetRegistration(scope).RegisterAsync();

            // Assert
            var state = GetCultureState(scope);
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(browser.DocumentKey, state.Version.DocumentKey),
                () => Assert.Equal(0, state.Version.Number));
        }

        [Fact]
        public async Task Should_register_again_under_its_identity_as_each_connection_comes_up()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();

            // Act
            registration.MarkUnavailable();
            await registration.RegisterAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal(2, browser.CountRegistrations()),
                () => Assert.Single(browser.Identities));
        }

        [Fact]
        public async Task Should_adopt_the_pair_the_page_accepted_meanwhile_as_it_registers_again()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();
            registration.MarkUnavailable();
            browser.CloseCircuit();
            await browser.LandAsync("he", "he");

            // Act
            await registration.RegisterAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Equal("he", GetCultureState(scope).UICulture.Name),
                () => Assert.Equal(1, GetCultureState(scope).Version.Number));
        }

        [Fact]
        public async Task Should_fail_where_the_page_can_not_coordinate()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            browser.FailNext("register", new JSException("The page carries no valid culture marker."));
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();

            // Act
            var exception = await Record.ExceptionAsync(GetRegistration(scope).RegisterAsync);

            // Assert
            Assert.Multiple(
                () => Assert.IsType<JSException>(exception),
                () => Assert.Equal(0, GetCultureState(scope).Revision));
        }
    }

    public sealed class Following
    {
        [Fact]
        public async Task Should_apply_each_pair_the_page_accepts()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            await GetRegistration(scope).RegisterAsync();

            // Act
            await browser.LandAsync("de-DE", "he");

            // Assert
            var state = GetCultureState(scope);
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", $"{state.Culture.Name}/{state.UICulture.Name}"),
                () => Assert.Equal(1, state.Version.Number));
        }

        [Fact]
        public async Task Should_apply_nothing_once_disposed()
        {
            // Arrange
            using var fixture = new CultureFixture("en-US");
            var browser = new FakeCultureCoordinatorBrowser();
            await using var provider = CreateServices(browser);
            await using var scope = provider.CreateAsyncScope();
            var registration = GetRegistration(scope);
            await registration.RegisterAsync();
            var identity = Assert.Single(browser.Identities);
            var reference = browser.GetRegistration(identity);

            // Act
            await registration.DisposeAsync();

            // Assert
            Assert.Multiple(
                () => Assert.Empty(browser.Identities),
                () => Assert.Equal(1, reference.Releases),
                () => Assert.Equal(CultureInfo.GetCultureInfo("en-US"), GetCultureState(scope).UICulture));
        }
    }
}
