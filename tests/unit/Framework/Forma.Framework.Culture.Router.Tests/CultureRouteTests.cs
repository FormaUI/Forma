namespace Forma.Framework.Culture.Router.Tests;


public sealed class CultureRouteTests
{
    [Fact]
    public void Should_default_hreflang_to_the_token()
    {
        // Arrange
        var route = new CultureRoute { Token = "en", UICulture = new("en") };

        // Act & Assert
        Assert.Equal("en", route.HrefLang);
    }

    [Fact]
    public void Should_let_an_explicit_hreflang_override_the_token()
    {
        // Arrange
        var route = new CultureRoute { Token = "latam", UICulture = new("es-419"), HrefLang = "es" };

        // Act & Assert
        Assert.Equal("es", route.HrefLang);
    }
}
