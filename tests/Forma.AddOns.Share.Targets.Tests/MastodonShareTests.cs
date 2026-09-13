namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Share;
using Forma.Framework.Share.Targets;
using Forma.Tests.Share;

using Xunit;

public sealed class MastodonShareTests
{
    private static readonly ShareItem Article = new()
    {
        Name = "Stage 12",
        Address = "https://example.com/news/stage-12",
    };

    [Fact]
    public void Should_open_the_composer_on_the_users_server()
    {
        // Arrange
        var servers = new FakeShareServerSource { Servers = { ["mastodon"] = new Uri("https://mastodon.social/") } };
        var target = new MastodonShare(servers);

        // Act
        var address = target.GetAddress(Article);

        // Assert
        Assert.Equal("https://mastodon.social/share?text=Stage%2012%20https%3A%2F%2Fexample.com%2Fnews%2Fstage-12", address);
    }

    [Fact]
    public void Should_support_a_link_before_the_user_names_a_server_and_answer_no_address()
    {
        // Arrange
        var target = new MastodonShare(new FakeShareServerSource());

        // Assert
        Assert.Multiple(
            () => Assert.True(target.RequiresAccount),
            () => Assert.True(target.Supports(Article)),
            () => Assert.Null(target.GetAddress(Article)));
    }

    [Fact]
    public void Should_refuse_a_file()
    {
        // Arrange
        var servers = new FakeShareServerSource { Servers = { ["mastodon"] = new Uri("https://mastodon.social/") } };
        var target = new MastodonShare(servers);
        var file = new ShareItem { Kind = ShareItemKind.File, Name = "kit.zip", Address = "https://example.com/files/kit.zip" };

        // Assert
        Assert.False(target.Supports(file));
    }
}
