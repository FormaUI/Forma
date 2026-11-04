namespace Forma.Framework.Culture.Router.AspNetCore.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

public sealed class CultureRoutingMiddlewareTests
{
    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "/file-viewer", "/file-viewer", "en")]
    [InlineData(CulturePrefix.ExceptDefault, "/he/file-viewer", "/file-viewer", "he")]
    [InlineData(CulturePrefix.ExceptDefault, "/es/file-viewer", "/file-viewer", "es-419")]
    [InlineData(CulturePrefix.ExceptDefault, "/he", "/", "he")]
    [InlineData(CulturePrefix.ExceptDefault, "/", "/", "en")]
    [InlineData(CulturePrefix.Always, "/en/file-viewer", "/file-viewer", "en")]
    [InlineData(CulturePrefix.Always, "/he/file-viewer", "/file-viewer", "he")]
    [InlineData(CulturePrefix.Always, "/es/file-viewer", "/file-viewer", "es-419")]
    public async Task Should_serve_a_canonical_url(CulturePrefix prefix, string requested, string forwarded, string culture)
    {
        // Arrange
        using var fixture = new CultureFixture();
        string? reached = null;
        var middleware = new CultureRoutingMiddleware(context => { reached = context.Request.Path.Value; return Task.CompletedTask; }, CreateRouter(prefix));
        var context = CreateContext(requested);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(forwarded, reached);
            Assert.Equal(culture, CultureInfo.CurrentUICulture.Name);
            Assert.Equal(string.Empty, context.Response.Headers.Location.ToString());
        });
    }

    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "/en/file-viewer", "/file-viewer")]
    [InlineData(CulturePrefix.ExceptDefault, "/EN/file-viewer", "/file-viewer")]
    [InlineData(CulturePrefix.ExceptDefault, "/en", "/")]
    [InlineData(CulturePrefix.ExceptDefault, "/es-419/file-viewer", "/es/file-viewer")]
    [InlineData(CulturePrefix.ExceptDefault, "/HE/file-viewer", "/he/file-viewer")]
    [InlineData(CulturePrefix.Always, "/file-viewer", "/en/file-viewer")]
    [InlineData(CulturePrefix.Always, "/", "/en")]
    [InlineData(CulturePrefix.Always, "/es-419/file-viewer", "/es/file-viewer")]
    [InlineData(CulturePrefix.Always, "/HE/file-viewer", "/he/file-viewer")]
    [InlineData(CulturePrefix.Always, "/xx/file-viewer", "/en/xx/file-viewer")]
    public async Task Should_redirect_a_url_that_is_not_canonical(CulturePrefix prefix, string requested, string location)
    {
        // Arrange
        using var fixture = new CultureFixture();
        var reachedNext = false;
        var middleware = new CultureRoutingMiddleware(_ => { reachedNext = true; return Task.CompletedTask; }, CreateRouter(prefix));
        var context = CreateContext(requested);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(reachedNext);
            Assert.Equal(location, context.Response.Headers.Location.ToString());
            Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode);
        });
    }

    [Theory]
    [InlineData("/xx/file-viewer")]
    [InlineData("/en-GB/file-viewer")]
    [InlineData("/es-anything/file-viewer")]
    public async Task Should_forward_an_unknown_first_segment_unchanged(string requested)
    {
        // Arrange
        using var fixture = new CultureFixture();
        string? reached = null;
        var middleware = new CultureRoutingMiddleware(context => { reached = context.Request.Path.Value; return Task.CompletedTask; }, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext(requested);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(requested, reached);
            Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
        });
    }

    // A segment equal to a registered token is always a culture, so two in a row name no address.
    [Theory]
    [InlineData("/he/es")]
    [InlineData("/he/en/file-viewer")]
    [InlineData("/es/es-419")]
    [InlineData("/he/en/app.css")]
    public async Task Should_reject_a_url_naming_consecutive_cultures(string requested)
    {
        // Arrange
        using var fixture = new CultureFixture();
        var reachedNext = false;
        var middleware = new CultureRoutingMiddleware(_ => { reachedNext = true; return Task.CompletedTask; }, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext(requested);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.False(reachedNext);
            Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
            Assert.Equal(string.Empty, context.Response.Headers.Location.ToString());
        });
    }

    [Fact]
    public async Task Should_carry_the_query_string_through_a_redirect()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/en/file-viewer");
        context.Request.QueryString = new QueryString("?tab=2&q=a%20b");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("/file-viewer?tab=2&q=a%20b", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Should_carry_the_path_base_through_a_redirect()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/en/file-viewer");
        context.Request.PathBase = new PathString("/app");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("/app/file-viewer", context.Response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Should_escape_a_reserved_character_in_a_redirect()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/en/a b");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("/a%20b", context.Response.Headers.Location.ToString());
    }

    [Theory]
    [InlineData("/he/app.css", "/app.css")]
    [InlineData("/he/_framework/blazor.web.js", "/_framework/blazor.web.js")]
    [InlineData("/he/_blazor/negotiate", "/_blazor/negotiate")]
    [InlineData("/he/_blazor/disconnect", "/_blazor/disconnect")]
    [InlineData("/_blazor", "/_blazor")]
    [InlineData("/favicon.ico", "/favicon.ico")]
    public async Task Should_neither_redirect_nor_cookie_a_request_that_is_not_a_document(string requested, string forwarded)
    {
        // Arrange
        using var fixture = new CultureFixture();
        string? reached = null;
        var middleware = new CultureRoutingMiddleware(context => { reached = context.Request.Path.Value; return Task.CompletedTask; }, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext(requested);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(forwarded, reached);
            Assert.Equal(string.Empty, context.Response.Headers.Location.ToString());
            Assert.Equal(string.Empty, context.Response.Headers.SetCookie.ToString());
        });
    }

    [Theory]
    [InlineData("/he/_blazor/negotiate", "/_blazor/negotiate")]
    [InlineData("/he/file-viewer", "/file-viewer")]
    public async Task Should_clear_the_endpoint_chosen_before_it_removes_the_culture_segment(string requested, string forwarded)
    {
        // Arrange — an endpoint and its route values chosen for the address with its culture segment
        using var fixture = new CultureFixture();
        var chosen = new Endpoint(null, null, "chosen");
        Endpoint? reachedEndpoint = chosen;
        var reachedValues = -1;
        var middleware = new CultureRoutingMiddleware(context => { reachedEndpoint = context.GetEndpoint(); reachedValues = context.Request.RouteValues.Count; return Task.CompletedTask; }, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext(requested);
        context.SetEndpoint(chosen);
        context.Request.RouteValues["culture"] = "he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(forwarded, context.Request.Path.Value);
            Assert.Null(reachedEndpoint);
            Assert.Equal(0, reachedValues);
        });
    }

    [Fact]
    public async Task Should_keep_the_endpoint_of_a_request_whose_path_it_leaves()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var chosen = new Endpoint(null, null, "chosen");
        Endpoint? reachedEndpoint = null;
        var middleware = new CultureRoutingMiddleware(context => { reachedEndpoint = context.GetEndpoint(); return Task.CompletedTask; }, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/_blazor/negotiate");
        context.SetEndpoint(chosen);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Same(chosen, reachedEndpoint);
    }

    [Fact]
    public async Task Should_write_the_culture_cookie_for_a_document()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/he/file-viewer");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cookie = context.Response.Headers.SetCookie.ToString();

        Assert.Multiple(() =>
        {
            Assert.Contains("forma-culture-route=he;", cookie, StringComparison.Ordinal);
            Assert.Contains("path=/;", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("httponly", cookie, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("max-age=", cookie, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task Should_write_the_culture_cookie_at_the_path_base()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/he/file-viewer");
        context.Request.PathBase = "/app";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var cookie = context.Response.Headers.SetCookie.ToString();

        Assert.Multiple(() =>
        {
            Assert.StartsWith("forma-culture-route%2Fapp=he;", cookie, StringComparison.Ordinal);
            Assert.Contains("path=/app;", cookie, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Theory]
    [InlineData("forma-culture-route=en; forma-culture-route%2Fapp=he")]
    [InlineData("forma-culture-route%2Fapp=he; forma-culture-route=en")]
    [InlineData("forma-culture-route%2Fapp%2Fadmin=en; forma-culture-route%2Fapp=he")]
    public async Task Should_read_only_its_own_culture_cookie_beside_another_applications_on_the_host(string cookies)
    {
        // Arrange — the cookies of an application at / and of one under /app/admin, beside this one's under /app
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/_blazor/negotiate");
        context.Request.PathBase = "/app";
        context.Request.Headers.Cookie = cookies;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("he", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task Should_leave_another_applications_culture_cookie_out_of_the_choice()
    {
        // Arrange — only the cookie of an application at /, naming Hebrew
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/_blazor/negotiate");
        context.Request.PathBase = "/app";
        context.Request.Headers.Cookie = "forma-culture-route=he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task Should_write_the_canonical_token_rather_than_the_alias_that_was_requested()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/es/file-viewer");

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Contains("forma-culture-route=es;", context.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Should_leave_the_culture_cookie_alone_when_it_already_matches()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/he/file-viewer");
        context.Request.Headers.Cookie = "forma-culture-route=he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(string.Empty, context.Response.Headers.SetCookie.ToString());
    }

    [Fact]
    public async Task Should_ignore_the_culture_cookie_when_choosing_the_culture()
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/file-viewer");
        context.Request.Headers.Cookie = "forma-culture-route=he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
            Assert.Equal(string.Empty, context.Response.Headers.Location.ToString());
        });
    }

    [Theory]
    [InlineData("/_blazor")]
    [InlineData("/_blazor/negotiate")]
    public async Task Should_take_the_culture_from_the_cookie_for_a_framework_endpoint(string requested)
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext(requested);
        context.Request.Headers.Cookie = "forma-culture-route=he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("he", CultureInfo.CurrentUICulture.Name);
    }

    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "/he/_blazor/negotiate", "he")]
    [InlineData(CulturePrefix.ExceptDefault, "/HE/_blazor/disconnect", "he")]
    [InlineData(CulturePrefix.Always, "/en/_blazor/negotiate", "en")]
    public async Task Should_take_the_culture_a_framework_endpoints_address_names_over_the_cookie(CulturePrefix prefix, string requested, string culture)
    {
        // Arrange — a cookie another tab wrote, naming the other culture
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(prefix));
        var context = CreateContext(requested);
        context.Request.Headers.Cookie = culture == "he" ? "forma-culture-route=en" : "forma-culture-route=he";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.Equal(culture, CultureInfo.CurrentUICulture.Name);
            Assert.Equal(string.Empty, context.Response.Headers.SetCookie.ToString());
            Assert.Equal(string.Empty, context.Response.Headers.Location.ToString());
        });
    }

    [Theory]
    [InlineData("forma-culture-route=xx")]
    [InlineData("forma-culture-route=es-419")]
    [InlineData("forma-culture-route=")]
    public async Task Should_fall_back_to_the_default_culture_when_the_cookie_names_no_route(string cookie)
    {
        // Arrange
        using var fixture = new CultureFixture();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/_blazor");
        context.Request.Headers.Cookie = cookie;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal("en", CultureInfo.CurrentUICulture.Name);
    }

    [Fact]
    public async Task Should_run_the_request_in_the_provider_s_cultures_for_its_route()
    {
        // Arrange
        using var fixture = new CultureFixture();
        await using var services = new ServiceCollection().AddSingleton<ICultureProvider>(new FakeCultureProvider()).BuildServiceProvider();
        var middleware = new CultureRoutingMiddleware(_ => Task.CompletedTask, CreateRouter(CulturePrefix.ExceptDefault));
        var context = CreateContext("/he/file-viewer");
        context.RequestServices = services;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        var requestCulture = context.Features.Get<IRequestCultureFeature>()!.RequestCulture;
        Assert.Multiple(
            () => Assert.Equal("he/he", $"{CultureInfo.CurrentCulture.Name}/{CultureInfo.CurrentUICulture.Name}"),
            () => Assert.Equal(FakeCultureProvider.ShortTimePattern, CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern),
            () => Assert.Equal(FakeCultureProvider.ShortTimePattern, CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern),
            () => Assert.Same(CultureInfo.CurrentCulture, requestCulture.Culture),
            () => Assert.Same(CultureInfo.CurrentUICulture, requestCulture.UICulture));
    }

    private static CultureRouter CreateRouter(CulturePrefix prefix)
        => new(new CultureRouterOptions
        {
            Prefix = prefix,
            DefaultCulture = "en",
            Routes =
            [
                new() { Token = "en", UICulture = new("en") },
                new() { Token = "he", UICulture = new("he") },
                new() { Token = "es", UICulture = new("es-419"), HrefLang = "es", Aliases = ["es-419"] }
            ]
        });

    private static DefaultHttpContext CreateContext(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;

        return context;
    }
}
