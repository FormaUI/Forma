namespace Forma.Framework.Globalization.AspNetCore.Tests;

using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

public sealed class CultureBaseHrefTests
{
    [Theory]
    [InlineData(CulturePrefix.ExceptDefault, "he", "", "/he/")]
    [InlineData(CulturePrefix.ExceptDefault, "he", "/app", "/app/he/")]
    [InlineData(CulturePrefix.ExceptDefault, "en", "", "/")]
    [InlineData(CulturePrefix.ExceptDefault, "en", "/app", "/app/")]
    [InlineData(CulturePrefix.Always, "en", "", "/en/")]
    [InlineData(CulturePrefix.Always, "en", "/my app", "/my%20app/en/")]
    public async Task Should_write_the_base_in_the_requests_culture_under_its_path_base(CulturePrefix prefix, string culture, string pathBase, string href)
    {
        // Arrange — the request's culture, which the middleware set and the request's culture state starts from
        using var fixture = new CultureFixture(culture);
        await using var provider = new ServiceCollection()
            .AddSingleton<NavigationManager>(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/")))
            .AddCultureRouting(new CultureRouterOptions
            {
                Prefix = prefix,
                DefaultCulture = "en",
                Routes =
                [
                    new() { Token = "en", Culture = new("en") },
                    new() { Token = "he", Culture = new("he") }
                ]
            })
            .BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        await using var renderer = new HtmlRenderer(scope.ServiceProvider, NullLoggerFactory.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.PathBase = pathBase;

        // Act
        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync<CascadingValue<HttpContext>>(ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(CascadingValue<>.Value)] = httpContext,
                [nameof(CascadingValue<>.ChildContent)] = (RenderFragment)(builder =>
                {
                    builder.OpenComponent<CultureBaseHref>(0);
                    builder.CloseComponent();
                })
            }));

            return output.ToHtmlString();
        });

        // Assert
        Assert.Equal($"<base href=\"{href}\" />", html);
    }
}
