namespace Forma.Framework.Themes.Blazor.Server.Tests;

using Microsoft.Extensions.DependencyInjection;

public sealed class RequestThemeResolutionTests
{
    private static string? WrittenRecord(Microsoft.AspNetCore.Http.HttpContext context)
        => context.Response.Headers.SetCookie.ToString() is { Length: > 0 } cookie ? cookie : null;

    [Fact]
    public async Task Should_start_a_request_without_a_record_or_a_store_in_the_default()
    {
        // Arrange
        using var fixture = new ThemeServerFixture();
        var context = fixture.CreateContext();

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("forma", ColorSchemePreference.System), theme.Choice),
            () => Assert.Equal(0, theme.Revision),
            () => Assert.True(theme.KeepsChoice),
            () => Assert.Same(theme, context.Features.Get<RequestTheme>()),
            () => Assert.Null(WrittenRecord(context)));
    }

    [Fact]
    public async Task Should_start_a_request_in_its_record_writing_nothing()
    {
        // Arrange
        using var fixture = new ThemeServerFixture();
        var context = fixture.CreateContext("theme=harbor&scheme=dark&revision=3");

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), theme.Choice),
            () => Assert.Equal(3, theme.Revision),
            () => Assert.Null(WrittenRecord(context)));
    }

    [Fact]
    public async Task Should_start_a_request_in_the_store_s_choice_where_its_revision_is_newer_and_write_it_into_the_record()
    {
        // Arrange — the reader changed it on another device
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("harbor", ColorSchemePreference.Light), 5) };
        using var fixture = new ThemeServerFixture(store);
        var context = fixture.CreateContext("theme=forma&scheme=dark&revision=4");

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Light), theme.Choice),
            () => Assert.Equal(5, theme.Revision),
            () => Assert.StartsWith("forma-theme=theme%3Dharbor%26scheme%3Dlight%26revision%3D5", WrittenRecord(context), StringComparison.Ordinal));
    }

    [Fact]
    public async Task Should_start_a_request_whose_record_agrees_with_the_store_in_the_record()
    {
        // Arrange — a switch the record holds whose write the store hasn't completed yet
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("harbor", ColorSchemePreference.Light), 4) };
        using var fixture = new ThemeServerFixture(store);
        var context = fixture.CreateContext("theme=forma&scheme=dark&revision=4");

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("forma", ColorSchemePreference.Dark), theme.Choice),
            () => Assert.Null(WrittenRecord(context)));
    }

    [Fact]
    public async Task Should_start_a_request_with_no_record_in_the_store_s_choice_as_a_sign_in_on_a_new_browser_does()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("harbor", ColorSchemePreference.Dark), 1) };
        using var fixture = new ThemeServerFixture(store);
        var context = fixture.CreateContext();

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), theme.Choice),
            () => Assert.NotNull(WrittenRecord(context)));
    }

    [Fact]
    public async Task Should_start_a_request_given_its_choice_in_it_reading_no_store_and_writing_no_record()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("forma", ColorSchemePreference.Light), 9) };
        using var fixture = new ThemeServerFixture(store, _ => new ThemeChoice("harbor", ColorSchemePreference.Dark));
        var context = fixture.CreateContext("theme=forma&scheme=light&revision=2");

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Multiple(
            () => Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), theme.Choice),
            () => Assert.False(theme.KeepsChoice),
            () => Assert.Equal(0, store.Reads),
            () => Assert.Null(WrittenRecord(context)));
    }

    [Theory]
    [InlineData("theme=removed&scheme=dark&revision=1", "forma", ColorSchemePreference.Dark)]
    [InlineData("theme=harbor&scheme=dusk&revision=1", "forma", ColorSchemePreference.System)]
    [InlineData("theme=harbor&scheme=dark&revision=-1", "forma", ColorSchemePreference.System)]
    [InlineData("harbor", "forma", ColorSchemePreference.System)]
    public async Task Should_start_a_request_whose_record_names_no_registered_theme_or_isn_t_one_in_the_default_theme(string record, string theme, ColorSchemePreference scheme)
    {
        // Arrange
        using var fixture = new ThemeServerFixture();
        var context = fixture.CreateContext(record);

        // Act
        var resolved = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Equal(new ThemeChoice(theme, scheme), resolved.Choice);
    }

    [Fact]
    public async Task Should_start_a_request_whose_store_fails_to_read_in_its_record()
    {
        // Arrange
        using var fixture = new ThemeServerFixture(new FakeThemePreferenceStore { FailsToRead = true });
        var context = fixture.CreateContext("theme=harbor&scheme=light&revision=2");

        // Act
        var theme = await RequestThemeResolution.ResolveAsync(context);

        // Assert
        Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Light), theme.Choice);
    }

    [Fact]
    public async Task Should_read_and_write_the_record_in_a_host_prefixed_secure_cookie_over_https()
    {
        // Arrange
        var store = new FakeThemePreferenceStore { Preference = new(new ThemeChoice("harbor", ColorSchemePreference.Dark), 2) };
        using var fixture = new ThemeServerFixture(store);
        var context = fixture.CreateContext("theme=forma&scheme=light&revision=1", isHttps: true);

        // Act
        await RequestThemeResolution.ResolveAsync(context);

        // Assert
        var cookie = WrittenRecord(context);
        Assert.Multiple(
            () => Assert.StartsWith("__Host-forma-theme=", cookie, StringComparison.Ordinal),
            () => Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("path=/", cookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Should_start_a_request_s_theme_state_in_the_request_s_choice()
    {
        // Arrange
        using var fixture = new ThemeServerFixture();
        var context = fixture.CreateContext("theme=harbor&scheme=dark&revision=1");
        await RequestThemeResolution.ResolveAsync(context);
        context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Http.IHttpContextAccessor>().HttpContext = context;

        // Act
        var state = context.RequestServices.GetRequiredService<IThemeState>();

        // Assert
        Assert.Equal(new ThemeChoice("harbor", ColorSchemePreference.Dark), state.Choice);
    }
}
