namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

using Xunit;

public sealed class MessengerShareTests
{
    private static readonly Uri Redirect = new("https://example.com/shared");

    [Fact]
    public void Should_open_the_send_dialog_under_the_hosts_app_on_the_items_address()
    {
        // Arrange
        var target = new MessengerShare("123456", Redirect);
        var article = new ShareItem { Name = "Stage 12", Address = "https://example.com/news/stage-12" };

        // Act
        var address = target.GetAddress(article);

        // Assert
        Assert.Equal(
            "https://www.facebook.com/dialog/send?app_id=123456&link=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12&redirect_uri=https%3A%2F%2Fexample.com%2Fshared",
            address);
    }

    [Fact]
    public void Should_refuse_a_file()
    {
        // Arrange
        var target = new MessengerShare("123456", Redirect);
        var file = new ShareItem { Kind = ShareItemKind.File, Name = "kit.zip", Address = "https://example.com/files/kit.zip" };

        // Assert
        Assert.False(target.Supports(file));
    }

    [Fact]
    public void Should_refuse_a_blank_app_id()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MessengerShare(" ", Redirect));
    }
}
