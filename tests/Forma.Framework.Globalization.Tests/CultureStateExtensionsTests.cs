namespace Forma.Framework.Globalization.Tests;

using System.Globalization;

using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class CultureStateExtensionsTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    private static string GetAmbientCultures()
        => $"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name}";

    // Changed inside an asynchronous method of its own, so the cultures the change applies stay in that flow and the caller's remain English.
    private static async Task<ICultureState> GetChangedStateAsync(IServiceScope scope)
    {
        var state = scope.ServiceProvider.GetRequiredService<ICultureState>();
        await state.ChangeAsync(German, Hebrew);

        return state;
    }

    public sealed class Operation
    {
        [Fact]
        public async Task Should_run_the_operation_in_the_states_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = new ServiceCollection().AddFormaCultureServices().BuildServiceProvider();
            using var scope = services.CreateScope();
            var state = await GetChangedStateAsync(scope);

            // Act
            var ambientCultures = state.Run(GetAmbientCultures);

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", ambientCultures),
                () => Assert.Equal("en/en", GetAmbientCultures()));
        }

        [Fact]
        public async Task Should_run_an_action_in_the_states_cultures()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = new ServiceCollection().AddFormaCultureServices().BuildServiceProvider();
            using var scope = services.CreateScope();
            var state = await GetChangedStateAsync(scope);
            var ambientCultures = "";

            // Act
            state.Run(() => { ambientCultures = GetAmbientCultures(); });

            // Assert
            Assert.Multiple(
                () => Assert.Equal("de-DE/he", ambientCultures),
                () => Assert.Equal("en/en", GetAmbientCultures()));
        }
    }
}
