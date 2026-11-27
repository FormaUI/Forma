namespace Forma.Framework.Themes.Blazor.Server.Tests;

public sealed class ThemePublicationTests
{
    [Fact]
    public void Should_publish_the_choice_and_the_store_s_revision_it_rests_on()
    {
        // Act
        var attributes = ThemePublication.GetMarkerAttributes(new RequestTheme(new ThemeChoice("harbor", ColorSchemePreference.System), 7, keepsChoice: true));

        // Assert
        Assert.Equal(
            new Dictionary<string, string> { ["data-theme"] = "harbor", ["data-scheme"] = "system", ["data-revision"] = "7" },
            attributes);
    }

    [Fact]
    public void Should_mark_a_page_given_its_choice_as_keeping_nothing()
    {
        // Act
        var attributes = ThemePublication.GetMarkerAttributes(new RequestTheme(new ThemeChoice("harbor", ColorSchemePreference.Dark), 0, keepsChoice: false));

        // Assert
        Assert.Equal(string.Empty, attributes["data-keeps-nothing"]);
    }
}
