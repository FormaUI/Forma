namespace Forma.UI.Blazor.Tests.Components.Interactive;

using Forma.UI.Blazor.Components.Interactive;

using Xunit;

public sealed class BreadcrumbItemTests
{
    [Fact]
    public void Should_create_item_with_label()
    {
        var item = new BreadcrumbItem<string>("Home");

        Assert.Equal("Home", item.Label);
        Assert.Null(item.Url);
    }

    [Fact]
    public void Should_create_item_with_label_and_url()
    {
        var item = new BreadcrumbItem<string>("Home", "/");

        Assert.Equal("Home", item.Label);
        Assert.Equal("/", item.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_invalid_label(string? label)
    {
        Assert.ThrowsAny<ArgumentException>(() => new BreadcrumbItem<string>(label!));
    }

    [Fact]
    public void Should_default_value_to_null()
    {
        var item = new BreadcrumbItem<string>("Home");

        Assert.Null(item.Value);
    }

    [Fact]
    public void Should_carry_a_value()
    {
        var item = new BreadcrumbItem<string>("Home") { Value = "home-folder" };

        Assert.Equal("home-folder", item.Value);
    }

    [Fact]
    public void Should_default_is_current_to_false()
    {
        var item = new BreadcrumbItem<string>("Home");

        Assert.False(item.IsCurrent);
    }

    [Fact]
    public void Should_default_children_to_null()
    {
        var item = new BreadcrumbItem<string>("Home");

        Assert.Null(item.Children);
    }

    [Fact]
    public void Should_carry_children()
    {
        BreadcrumbItem<string>[] children = [new("Races"), new("Covers")];

        var item = new BreadcrumbItem<string>("Home") { Children = children };

        Assert.NotNull(item.Children);
        Assert.Equal(["Races", "Covers"], item.Children.Select(c => c.Label));
    }
}
