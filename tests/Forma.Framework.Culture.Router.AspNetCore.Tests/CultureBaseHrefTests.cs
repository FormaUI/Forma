namespace Forma.Framework.Culture.Router.AspNetCore.Tests;

using Forma.Tests.Culture;
using Forma.Tests.Interop;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

public sealed class CultureBaseHrefTests
{
    [Theory]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.ExceptDefault, "he", "", "/he/")]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.ExceptDefault, "he", "/app", "/app/he/")]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.ExceptDefault, "en", "", "/")]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.ExceptDefault, "en", "/app", "/app/")]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.Always, "en", "", "/en/")]
    [InlineData(CultureSwitchMode.FullLoad, CulturePrefix.Always, "en", "/my app", "/my%20app/en/")]
    [InlineData(CultureSwitchMode.InPlace, CulturePrefix.ExceptDefault, "he", "", "/")]
    [InlineData(CultureSwitchMode.InPlace, CulturePrefix.ExceptDefault, "he", "/app", "/app/")]
    [InlineData(CultureSwitchMode.InPlace, CulturePrefix.Always, "en", "/my app", "/my%20app/")]
    public async Task Should_write_the_base_in_the_requests_culture_or_at_the_root_under_its_path_base(CultureSwitchMode cultureSwitchMode, CulturePrefix prefix, string culture, string pathBase, string href)
    {
        // Arrange — the request's culture, which the middleware set and the request's culture state starts from
        using var fixture = new CultureFixture(culture);
        await using var provider = new ServiceCollection()
            .AddSingleton<NavigationManager>(new FakeNavigationManager(new Uri("http://localhost/"), new Uri("http://localhost/")))
            .AddSingleton<IJSRuntime>(new RecordingJSRuntime((_, _) => null))
            .AddScoped<ICultureSwitch, TestCultureSwitch>()
            .AddFormaCultureRouting(new CultureRouterOptions
            {
                Prefix = prefix,
                Switch = cultureSwitchMode,
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
