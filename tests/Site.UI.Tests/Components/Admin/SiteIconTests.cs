using Site.UI.Components.Admin;
using Xunit;

namespace Site.UI.Tests.Components.Admin;

public sealed class SiteIconTests
{
    [Fact]
    public void FontAwesome_should_produce_solid_css_class()
    {
        var icon = SiteIcon.FontAwesome("gear");

        Assert.Equal("fa-solid fa-gear", icon.CssClass);
    }

    [Fact]
    public void FontAwesomeRegular_should_produce_regular_css_class()
    {
        var icon = SiteIcon.FontAwesomeRegular("bell");

        Assert.Equal("fa-regular fa-bell", icon.CssClass);
    }

    [Fact]
    public void From_should_preserve_css_class()
    {
        var icon = SiteIcon.From("custom-icon");

        Assert.Equal("custom-icon", icon.CssClass);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FontAwesome_should_reject_invalid_name(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => SiteIcon.FontAwesome(name!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FontAwesomeRegular_should_reject_invalid_name(string? name)
    {
        Assert.ThrowsAny<ArgumentException>(() => SiteIcon.FontAwesomeRegular(name!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_should_reject_invalid_css_class(string? cssClass)
    {
        Assert.ThrowsAny<ArgumentException>(() => SiteIcon.From(cssClass!));
    }
}
