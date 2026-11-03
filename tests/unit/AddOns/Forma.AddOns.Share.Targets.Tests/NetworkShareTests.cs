namespace Forma.AddOns.Share.Targets.Tests;

using Forma.Framework.Resources;
using Forma.Framework.Share;
using Forma.Framework.Share.Targets;

using Xunit;

public sealed class NetworkShareTests
{
    private static readonly ShareItem Article = new()
    {
        Name = "Stage 12",
        Address = "https://example.com/news/stage-12",
    };

    public static TheoryData<LinkShare, string> Composers => new()
    {
        { new FacebookShare(), "https://www.facebook.com/sharer/sharer.php?u=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12" },
        { new XShare(), "https://x.com/intent/post?url=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12&text=Stage%2012" },
        { new WhatsAppShare(), "https://wa.me/?text=Stage%2012%20https%3A%2F%2Fexample.com%2Fnews%2Fstage-12" },
        { new TelegramShare(), "https://t.me/share/url?url=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12&text=Stage%2012" },
        { new BlueskyShare(), "https://bsky.app/intent/compose?text=Stage%2012%20https%3A%2F%2Fexample.com%2Fnews%2Fstage-12" },
        { new RedditShare(), "https://www.reddit.com/submit?url=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12&title=Stage%2012" },
        { new SnapchatShare(), "https://www.snapchat.com/share?link=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12" },
        { new LinkedInShare(), "https://www.linkedin.com/sharing/share-offsite/?url=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12" },
    };

    public sealed class EveryNetwork
    {
        [Theory]
        [MemberData(nameof(Composers), MemberType = typeof(NetworkShareTests))]
        public void Should_open_the_networks_composer_on_the_items_address(LinkShare target, string expected)
        {
            // Assert
            Assert.Equal(expected, target.GetAddress(Article));
            Assert.True(target.Supports(Article));
        }

        [Theory]
        [MemberData(nameof(Composers), MemberType = typeof(NetworkShareTests))]
        public void Should_refuse_a_file_and_a_contact(LinkShare target, string expected)
        {
            // Arrange
            var file = Article with { Kind = ShareItemKind.File };
            var contact = Article with { Kind = ShareItemKind.Contact };

            // Assert
            Assert.NotNull(expected);
            Assert.False(target.Supports(file));
            Assert.False(target.Supports(contact));
        }
    }

    public sealed class Pinterest
    {
        [Fact]
        public void Should_pin_a_picture_by_its_media_and_refuse_everything_else()
        {
            // Arrange
            var target = new PinterestShare();
            var picture = Article with { Kind = ShareItemKind.Image, Resource = new ResourceInfo("https://example.com/photos/finish.jpg") };

            // Assert
            Assert.Multiple(
                () => Assert.False(target.Supports(Article)),
                () => Assert.Contains("media=https%3A%2F%2Fexample.com%2Fphotos%2Ffinish.jpg", target.GetAddress(picture), StringComparison.Ordinal),
                () => Assert.Contains("url=https%3A%2F%2Fexample.com%2Fnews%2Fstage-12", target.GetAddress(picture), StringComparison.Ordinal));
        }
    }

    public sealed class NamelessItem
    {
        [Fact]
        public void Should_share_its_address_alone()
        {
            // Arrange
            var nameless = new ShareItem { Address = "https://example.com/a" };

            // Assert
            Assert.Equal("https://wa.me/?text=https%3A%2F%2Fexample.com%2Fa", new WhatsAppShare().GetAddress(nameless));
        }
    }
}
