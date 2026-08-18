namespace Forma.UI.Blazor.Tests.Globalization;

using Forma.UI.Blazor.Globalization;

public sealed class CultureRouteTests
{
    [Fact]
    public void Should_default_hreflang_to_the_token()
    {
        // Arrange
        var route = new CultureRoute { Token = "en", Culture = new("en") };

        // Act & Assert
        Assert.Equal("en", route.HrefLang);
    }

    [Fact]
    public void Should_let_an_explicit_hreflang_override_the_token()
    {
        // Arrange
        var route = new CultureRoute { Token = "latam", Culture = new("es-419"), HrefLang = "es" };

        // Act & Assert
        Assert.Equal("es", route.HrefLang);
    }
}
