namespace Forma.Blazor.Tests.Globalization;

using Forma.Blazor.Globalization;
using Forma.Blazor.Tests.Support.Shared;

public sealed class CultureRouterTests
{
    [Fact]
    public void Should_reject_a_default_culture_that_is_not_a_registered_token()
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = "es",
            Routes = [new() { Token = "en", Culture = new("en") }]
        };

        // Act
        var thrown = Record.Exception(() => new CultureRouter(options));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    // A default that merely resolves is not enough: the bare URL must name exactly one culture.
    [Fact]
    public void Should_reject_a_default_culture_that_only_matches_an_alias()
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = "es-419",
            Routes = [new() { Token = "es", Culture = new("es-419"), HrefLang = "es", Aliases = ["es-419"] }]
        };

        // Act
        var thrown = Record.Exception(() => new CultureRouter(options));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Should_reject_an_empty_culture_set()
    {
        // Arrange
        var options = new CultureRouterOptions { DefaultCulture = "en", Routes = [] };

        // Act
        var thrown = Record.Exception(() => new CultureRouter(options));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData("he", "es")]
    [InlineData("es", "es")]
    public void Should_reject_a_token_claimed_by_more_than_one_culture(string first, string second)
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", Culture = new("en"), Aliases = [first] },
                new() { Token = "he", Culture = new("he"), Aliases = [second] }
            ]
        };

        // Act
        var thrown = Record.Exception(() => new CultureRouter(options));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    // Google supports neither a UN M.49 region such as 419 nor a three-letter language.
    [Theory]
    [InlineData("es-419")]
    [InlineData("spa")]
    [InlineData("es-latam")]
    public void Should_reject_a_token_that_is_not_a_usable_hreflang(string token)
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", Culture = new("en") },
                new() { Token = token, Culture = new("es-419") }
            ]
        };

        // Act
        var thrown = Record.Exception(() => new CultureRouter(options));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Theory]
    [InlineData("en")]
    [InlineData("en-GB")]
    [InlineData("zh-Hant")]
    [InlineData("zh-Hant-TW")]
    public void Should_accept_a_language_with_an_optional_script_and_region(string token)
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = token,
            Routes = [new() { Token = token, Culture = new("en") }]
        };

        // Act
        var router = new CultureRouter(options);

        // Assert
        Assert.Equal(token, router.DefaultRoute.Token);
    }

    // An explicit hreflang is what lets a token Google would reject still be used as a URL segment.
    [Fact]
    public void Should_accept_an_unusable_token_carrying_an_explicit_hreflang()
    {
        // Arrange
        var options = new CultureRouterOptions
        {
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", Culture = new("en") },
                new() { Token = "latam", Culture = new("es-419"), HrefLang = "es" }
            ]
        };

        // Act
        var router = new CultureRouter(options);

        // Assert
        Assert.Equal("es", router.Routes[1].HrefLang);
    }

    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "en", "/file-viewer", "/file-viewer")]
    [InlineData(CulturePrefix.ExceptDefault, "he", "/file-viewer", "/he/file-viewer")]
    [InlineData(CulturePrefix.ExceptDefault, "en", "/", "/")]
    [InlineData(CulturePrefix.ExceptDefault, "he", "/", "/he")]
    [InlineData(CulturePrefix.Always, "en", "/file-viewer", "/en/file-viewer")]
    [InlineData(CulturePrefix.Always, "en", "/", "/en")]
    public void Should_build_the_path_addressing_a_culture(CulturePrefix prefix, string token, string path, string expected)
    {
        // Arrange
        var router = Router(prefix);
        var route = router.Routes.Single(culture => culture.Token == token);

        // Act
        var result = router.GetPath(route, path);

        // Assert
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("he", "/es/file-viewer", "/he/file-viewer")]
    [InlineData("en", "/he/file-viewer", "/file-viewer")]
    public void Should_strip_an_existing_culture_segment_before_prefixing(string token, string path, string expected)
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);
        var route = router.Routes.Single(culture => culture.Token == token);

        // Act
        var result = router.GetPath(route, path);

        // Assert
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData("/he/file-viewer", "he", "/file-viewer")]
    [InlineData("/es-419/file-viewer", "es", "/file-viewer")]
    [InlineData("/HE/file-viewer", "he", "/file-viewer")]
    [InlineData("/he", "he", "/")]
    public void Should_match_a_known_culture_and_strip_it_from_the_path(string requested, string token, string remainder)
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);

        // Act
        var match = router.Match(requested);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(token, match.Route.Token);
            Assert.Equal(remainder, match.Path.Value);
        });
    }

    [Theory]
    [InlineData("/file-viewer")]
    [InlineData("/xx/file-viewer")]
    [InlineData("/")]
    public void Should_leave_a_path_that_names_no_culture_intact(string requested)
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);

        // Act
        var match = router.Match(requested);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(router.DefaultRoute, match.Route);
            Assert.Equal(requested, match.Path.Value);
        });
    }

    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "/file-viewer", true)]
    [InlineData(CulturePrefix.ExceptDefault, "/en/file-viewer", false)]
    [InlineData(CulturePrefix.ExceptDefault, "/he/file-viewer", true)]
    [InlineData(CulturePrefix.ExceptDefault, "/HE/file-viewer", false)]
    [InlineData(CulturePrefix.ExceptDefault, "/es-419/file-viewer", false)]
    [InlineData(CulturePrefix.Always, "/en/file-viewer", true)]
    [InlineData(CulturePrefix.Always, "/file-viewer", false)]
    [InlineData(CulturePrefix.Always, "/xx/file-viewer", false)]
    public void Should_report_whether_the_requested_address_is_canonical(CulturePrefix prefix, string requested, bool expected)
    {
        // Arrange
        var router = Router(prefix);

        // Act
        var match = router.Match(requested);

        // Assert
        Assert.Equal(expected, match.IsCanonical);
    }

    [Fact]
    public void Should_provide_the_canonical_path_for_a_non_canonical_address()
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);

        // Act
        var match = router.Match("/es-419/file-viewer");

        // Assert
        Assert.Equal("/es/file-viewer", match.CanonicalPath.Value);
    }

    // A segment equal to a registered token is always a culture, so two in a row name no address.
    [Theory]
    [InlineData("/he/es")]
    [InlineData("/he/en/file-viewer")]
    [InlineData("/es/es-419")]
    [InlineData("/HE/EN")]
    public void Should_reject_a_path_naming_consecutive_cultures(string requested)
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);

        // Act
        var thrown = Record.Exception(() => router.Match(requested));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Should_reject_building_a_path_naming_consecutive_cultures()
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);
        var he = router.Routes.Single(route => route.Token == "he");

        // Act
        var thrown = Record.Exception(() => router.GetPath(he, "/en/es"));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    [Fact]
    public void Should_reject_building_a_url_naming_consecutive_cultures()
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);
        var he = router.Routes.Single(route => route.Token == "he");

        // Act
        var thrown = Record.Exception(() => router.GetUrl(he, "en/es?tab=2"));

        // Assert
        Assert.IsType<ArgumentException>(thrown);
    }

    // A culture may still appear further along, where it is an ordinary segment rather than a prefix.
    [Theory]
    [InlineData("/he")]
    [InlineData("/he/file-viewer")]
    [InlineData("/he/english")]
    [InlineData("/file-viewer/en")]
    public void Should_accept_a_path_without_consecutive_cultures(string requested)
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);

        // Act & Assert
        Assert.False(router.HasConsecutiveCultures(requested));
    }

    [Fact]
    public void Should_build_a_relative_url_including_the_query_string()
    {
        // Arrange
        var router = Router(CulturePrefix.ExceptDefault);
        var he = router.Routes.Single(route => route.Token == "he");

        // Act
        var url = router.GetUrl(he, "es/file-viewer?tab=2");

        // Assert
        Assert.Equal("/he/file-viewer?tab=2", url);
    }

    [Fact]
    public void Should_fall_back_to_the_default_route_when_the_ambient_culture_matches_none()
    {
        // Arrange
        using var culture = new CultureFixture("fr");
        var router = Router(CulturePrefix.ExceptDefault);

        // Act & Assert
        Assert.Equal(router.DefaultRoute, router.CurrentRoute);
    }

    private static CultureRouter Router(CulturePrefix prefix)
        => new(new CultureRouterOptions
        {
            Prefix = prefix,
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", Culture = new("en") },
                new() { Token = "he", Culture = new("he") },
                new() { Token = "es", Culture = new("es-419"), HrefLang = "es", Aliases = ["es-419"] }
            ]
        });
}
