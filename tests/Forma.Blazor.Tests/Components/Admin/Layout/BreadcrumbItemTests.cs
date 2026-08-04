namespace Forma.Blazor.Tests.Components.Admin.Layout;

using Forma.Blazor.Components.Admin.Layout;

using Xunit;

public sealed class BreadcrumbItemTests
{
    [Fact]
    public void Should_create_item_with_label()
    {
        var item = new BreadcrumbItem("Home");

        Assert.Equal("Home", item.Label);
        Assert.Null(item.Url);
    }

    [Fact]
    public void Should_create_item_with_label_and_url()
    {
        var item = new BreadcrumbItem("Home", "/");

        Assert.Equal("Home", item.Label);
        Assert.Equal("/", item.Url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_reject_invalid_label(string? label)
    {
        Assert.ThrowsAny<ArgumentException>(() => new BreadcrumbItem(label!));
    }
}
