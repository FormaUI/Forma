namespace Forma.Framework.Culture.Router.Tests;

using Forma.Framework.Culture.Browser.Interop;

using Microsoft.Extensions.DependencyInjection;

public sealed class CultureDestinationSourceTests
{
    private static ServiceProvider CreateServices(ICultureDestinationResolver? resolver = null)
    {
        var services = new ServiceCollection().AddFormaCultureRouting(new CultureRouterOptions
        {
            Prefix = CulturePrefix.ExceptDefault,
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", UICulture = new("en") },
                new() { Token = "he", UICulture = new("he") }
            ]
        });

        if (resolver is not null)
        {
            services.AddSingleton(resolver);
        }

        return services.BuildServiceProvider();
    }

    public sealed class Resolving
    {
        [Fact]
        public async Task Should_name_nothing_where_the_host_resolves_no_destination()
        {
            // Arrange
            await using var provider = CreateServices();
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("news/the-final", "he", TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.False(source.ResolvesDestinations),
                () => Assert.Null(destination));
        }

        [Fact]
        public async Task Should_hand_the_resolver_both_routes_and_the_address_without_its_culture_segment()
        {
            // Arrange
            var resolver = new RecordingResolver(_ => CultureDestination.Translated("hadashot/hagmar?page=2"));
            await using var provider = CreateServices(resolver);
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("news/the-final?page=2", "he", TestContext.Current.CancellationToken);

            // Assert
            var (from, to, relativeAddress) = Assert.Single(resolver.Requests);
            Assert.Multiple(
                () => Assert.True(source.ResolvesDestinations),
                () => Assert.Equal(("en", "he", "news/the-final?page=2"), (from.Token, to.Token, relativeAddress)),
                () => Assert.Equal("/he/hadashot/hagmar?page=2", destination));
        }

        [Fact]
        public async Task Should_read_the_route_the_address_names_and_leave_out_its_segment()
        {
            // Arrange
            var resolver = new RecordingResolver(_ => CultureDestination.Translated("/news/the-final"));
            await using var provider = CreateServices(resolver);
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("he/hadashot/hagmar", "en", TestContext.Current.CancellationToken);

            // Assert
            var (from, to, relativeAddress) = Assert.Single(resolver.Requests);
            Assert.Multiple(
                () => Assert.Equal(("he", "en", "hadashot/hagmar"), (from.Token, to.Token, relativeAddress)),
                () => Assert.Equal("/news/the-final", destination));
        }

        [Fact]
        public async Task Should_give_the_pages_own_address_where_the_resolver_names_none()
        {
            // Arrange
            await using var provider = CreateServices(new RecordingResolver(_ => null));
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("news/the-final?page=2", "he", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("/he/news/the-final?page=2", destination);
        }

        [Fact]
        public async Task Should_give_the_fallback_the_resolver_names_in_the_routes_culture()
        {
            // Arrange
            await using var provider = CreateServices(new RecordingResolver(_ => CultureDestination.Fallback("hadashot")));
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("news/an-untranslated-story", "he", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("/he/hadashot", destination);
        }

        [Fact]
        public async Task Should_name_nothing_for_a_token_no_route_carries()
        {
            // Arrange
            var resolver = new RecordingResolver(_ => CultureDestination.Translated("ailleurs"));
            await using var provider = CreateServices(resolver);
            var source = provider.GetRequiredService<ICultureDestinationSource>();

            // Act
            var destination = await source.ResolveAsync("news/the-final", "fr", TestContext.Current.CancellationToken);

            // Assert
            Assert.Multiple(
                () => Assert.Null(destination),
                () => Assert.Empty(resolver.Requests));
        }
    }

    // A resolver that answers each request through `answer` and records what it was asked.
    private sealed class RecordingResolver(Func<string, CultureDestination?> answer) : ICultureDestinationResolver
    {
        public List<(CultureRoute From, CultureRoute To, string RelativeAddress)> Requests { get; } = [];

        public ValueTask<CultureDestination?> ResolveAsync(CultureRoute from, CultureRoute to, string relativeAddress, CancellationToken cancellationToken)
        {
            Requests.Add((from, to, relativeAddress));

            return ValueTask.FromResult(answer(relativeAddress));
        }
    }
}
