namespace Forma.Framework.Localization.Tests;

using System.Globalization;

using Forma.Framework.Globalization;
using Forma.Framework.Localization;
using Forma.Tests.Globalization;
using Forma.Tests.Shared;

using Microsoft.Extensions.DependencyInjection;

using Xunit;

public sealed class StringLocaleFactoryExtensionsTests
{
    // Changed inside an asynchronous method of its own, so the cultures the change applies stay in that flow and the caller's are left as they were.
    private static async Task ChangeAsync(ICultureState cultureState, string culture)
        => await cultureState.ChangeAsync(CultureInfo.GetCultureInfo(culture));

    public sealed class Create
    {
        [Fact]
        public async Task Should_create_one_locale_that_follows_every_change_of_the_state()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            using var services = new ServiceCollection().AddFormaCultureServices().BuildServiceProvider();
            using var scope = services.CreateScope();
            var cultureState = scope.ServiceProvider.GetRequiredService<ICultureState>();
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Create), cultureState);
            var english = locale["Stored {Amount}", 1.5].Value;

            // Act
            await ChangeAsync(cultureState, "de-DE");
            var german = locale["Stored {Amount}", 1.5].Value;

            // Assert
            Assert.Multiple(
                () => Assert.Equal(["Stored 1.5", "Stored 1,5"], [english, german]),
                () => Assert.Equal("en", CultureInfo.CurrentCulture.Name));
        }

        [Fact]
        public void Should_bind_again_after_a_change_that_lands_between_its_reads_of_the_state()
        {
            // Arrange
            using var fixture = new CultureFixture("en");
            var cultureState = new InterleavedCultureState(CultureInfo.GetCultureInfo("en"), CultureInfo.GetCultureInfo("de-DE"));
            var locale = NullStringLocaleFactory.Instance.Create(typeof(Create), cultureState);

            // Act
            var duringChange = locale["Stored {Amount}", 1.5].Value;
            var afterChange = locale["Stored {Amount}", 1.5].Value;

            // Assert
            Assert.Equal(["Stored 1.5", "Stored 1,5"], [duringChange, afterChange]);
        }

        [Fact]
        public void Should_reject_a_missing_culture_state()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => NullStringLocaleFactory.Instance.Create(typeof(Create), null!));
        }
    }
}
