namespace Forma.Framework.Culture.Blazor.Server.Tests;

using System.Globalization;

using Forma.Tests.Culture;
using Forma.Tests.Shared;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

public sealed class RequestCultureExtensionsTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo Hebrew = CultureInfo.GetCultureInfo("he");

    // A request whose pipeline chose German formats and Hebrew text, as request localization chooses them, ahead of the step.
    private static async Task<string> RunAsync(IServiceProvider services, HttpContext context)
    {
        var app = new ApplicationBuilder(services);
        app.Use((request, next) =>
        {
            CultureInfo.CurrentCulture = German;
            CultureInfo.CurrentUICulture = Hebrew;
            request.Features.Set<IRequestCultureFeature>(new RequestCultureFeature(new RequestCulture(German, Hebrew), new CookieRequestCultureProvider()));

            return next(request);
        });
        app.UseFormaRequestCulture();

        var reached = string.Empty;
        app.Run(_ =>
        {
            reached = $"{CultureInfo.CurrentCulture.Name} {CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern}/{CultureInfo.CurrentUICulture.Name} {CultureInfo.CurrentUICulture.DateTimeFormat.ShortTimePattern}";

            return Task.CompletedTask;
        });

        await app.Build()(context);

        return reached;
    }

    [Fact]
    public async Task Should_run_the_request_in_the_provider_s_cultures_for_the_names_its_pipeline_chose()
    {
        // Arrange
        using var fixture = new CultureFixture();
        await using var services = new ServiceCollection().AddSingleton<ICultureProvider>(new FakeCultureProvider()).BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };

        // Act
        var reached = await RunAsync(services, context);

        // Assert
        var feature = context.Features.Get<IRequestCultureFeature>()!;
        Assert.Multiple(
            () => Assert.Equal($"de-DE {FakeCultureProvider.ShortTimePattern}/he {FakeCultureProvider.ShortTimePattern}", reached),
            () => Assert.Equal(FakeCultureProvider.ShortTimePattern, feature.RequestCulture.Culture.DateTimeFormat.ShortTimePattern),
            () => Assert.Equal(FakeCultureProvider.ShortTimePattern, feature.RequestCulture.UICulture.DateTimeFormat.ShortTimePattern),
            () => Assert.IsType<CookieRequestCultureProvider>(feature.Provider));
    }

    [Fact]
    public async Task Should_leave_a_request_in_its_own_cultures_where_no_provider_is_registered()
    {
        // Arrange
        using var fixture = new CultureFixture();
        await using var services = new ServiceCollection().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };

        // Act
        var reached = await RunAsync(services, context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal($"de-DE {German.DateTimeFormat.ShortTimePattern}/he {Hebrew.DateTimeFormat.ShortTimePattern}", reached),
            () => Assert.Same(German, context.Features.Get<IRequestCultureFeature>()!.RequestCulture.Culture));
    }
}
