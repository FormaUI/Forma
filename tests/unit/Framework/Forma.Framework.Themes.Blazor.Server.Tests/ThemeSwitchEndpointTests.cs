namespace Forma.Framework.Themes.Blazor.Server.Tests;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

public sealed class ThemeSwitchEndpointTests
{
    private static FormCollection CreateForm(string theme, string scheme, string? returnAddress = "/settings")
        => new(new Dictionary<string, StringValues>
        {
            ["theme"] = theme,
            ["scheme"] = scheme,
            ["returnUrl"] = returnAddress,
        });

    private static async Task<HttpContext> SwitchAsync(ThemeServerFixture fixture, FormCollection form, string? record = null)
    {
        var context = fixture.CreateContext(record);
        var result = await ThemeSwitchEndpoint.SwitchAsync(context, form);
        await result.ExecuteAsync(context);

        return context;
    }

    [Fact]
    public async Task Should_keep_the_choice_in_the_store_one_revision_past_the_latest_then_in_the_record_and_send_the_reader_back()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("forma", ColorSchemePreference.Light), 3) };
        using var fixture = new ThemeServerFixture(store);

        // Act
        var context = await SwitchAsync(fixture, CreateForm("harbor", "dark"), "theme=forma&scheme=light&revision=2");

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemePreference(new ThemeChoice("harbor", ColorSchemePreference.Dark), 4), Assert.Single(store.Writes)),
            () => Assert.StartsWith("forma-theme=theme%3Dharbor%26scheme%3Ddark%26revision%3D4", context.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal),
            () => Assert.Equal(StatusCodes.Status302Found, context.Response.StatusCode),
            () => Assert.Equal("/settings", context.Response.Headers.Location.ToString()));
    }

    [Fact]
    public async Task Should_keep_the_choice_in_the_record_beside_the_earlier_revision_where_the_store_fails()
    {
        // Arrange
        using var fixture = new ThemeServerFixture(new FakeThemePreferenceStore { FailsToWrite = true });

        // Act
        var context = await SwitchAsync(fixture, CreateForm("harbor", "light"), "theme=forma&scheme=light&revision=6");

        // Assert
        Assert.StartsWith("forma-theme=theme%3Dharbor%26scheme%3Dlight%26revision%3D6", context.Response.Headers.SetCookie.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("removed", "dark")]
    [InlineData("harbor", "dusk")]
    [InlineData("", "dark")]
    public async Task Should_refuse_a_switch_naming_no_registered_theme_or_no_scheme_preference(string theme, string scheme)
    {
        // Arrange
        var store = new FakeThemePreferenceStore();
        using var fixture = new ThemeServerFixture(store);

        // Act
        var context = await SwitchAsync(fixture, CreateForm(theme, scheme));

        // Assert
        Assert.Multiple(
            () => Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode),
            () => Assert.Empty(store.Writes),
            () => Assert.Equal(string.Empty, context.Response.Headers.SetCookie.ToString()));
    }

    [Theory]
    [InlineData("https://elsewhere.test/")]
    [InlineData("//elsewhere.test/")]
    [InlineData("/\\elsewhere.test/")]
    [InlineData(null)]
    public async Task Should_send_the_reader_to_the_application_s_root_from_a_return_address_that_isn_t_its_own(string? returnAddress)
    {
        // Arrange
        using var fixture = new ThemeServerFixture();

        // Act
        var context = await SwitchAsync(fixture, CreateForm("harbor", "dark", returnAddress));

        // Assert
        Assert.Equal("/", context.Response.Headers.Location.ToString());
    }
}
