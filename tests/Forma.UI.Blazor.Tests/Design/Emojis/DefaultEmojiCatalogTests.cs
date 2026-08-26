namespace Forma.UI.Blazor.Tests.Design.Emojis;

using Forma.UI.Blazor.Design.Emojis;
using Forma.UI.Design.Emojis;

public sealed class DefaultEmojiCatalogTests
{
    private static readonly string[] GroupIds =
        ["smileys", "people", "animals", "food", "activities", "travel", "objects", "symbols", "flags"];

    [Fact]
    public void Should_load_every_category()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Assert
        Assert.All(GroupIds, groupId =>
        {
            Assert.True(catalog.TryFindByGroupId(groupId, out var emojis));
            Assert.NotEmpty(emojis);
        });
    }

    [Fact]
    public void Should_refuse_a_group_it_does_not_hold()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var found = catalog.TryFindByGroupId("nonexistent", out var emojis);

        // Assert
        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Empty(emojis));
    }

    [Fact]
    public void Should_place_every_emoji_in_the_group_it_was_asked_for()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        catalog.TryFindByGroupId("smileys", out var smileys);

        // Assert
        Assert.NotEmpty(smileys);
        Assert.All(smileys, info => Assert.Equal("smileys", info.GroupId));
    }

    [Fact]
    public void Should_give_every_emoji_a_name_and_search_keywords()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Assert
        Assert.All(GroupIds, groupId =>
        {
            catalog.TryFindByGroupId(groupId, out var emojis);

            Assert.All(emojis, info =>
            {
                Assert.False(string.IsNullOrWhiteSpace(info.Name));
                Assert.NotEmpty(info.Keywords);
            });
        });
    }

    [Fact]
    public void Should_find_a_known_emoji_by_its_character()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var found = catalog.TryGetByEmoji("😀", out var info);

        // Assert
        Assert.True(found);
        Assert.NotNull(info);
        Assert.Equal("😀", info.Emoji.Character);
    }

    [Fact]
    public void Should_refuse_an_emoji_it_does_not_hold()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var found = catalog.TryGetByEmoji("🛸👾🫠🫠🫠🫠🫠", out var info);

        // Assert
        Assert.Multiple(
            () => Assert.False(found),
            () => Assert.Null(info));
    }

    [Fact]
    public void Should_match_a_search_against_keywords_rather_than_the_name_alone()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var results = catalog.Find("grin");

        // Assert
        Assert.Contains(results, info => info.Emoji.Character == "😀");
    }

    [Fact]
    public void Should_match_a_word_rather_than_any_substring()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var results = catalog.Find("cat");

        // Assert
        Assert.Contains(results, info => info.Emoji.Character == "🐱");
        Assert.DoesNotContain(results, info => info.Emoji.Character == "🥴");
    }

    [Fact]
    public void Should_match_a_word_that_is_not_the_first()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var results = catalog.Find("face");

        // Assert
        Assert.Contains(results, info => info.Emoji.Character == "😀");
    }

    [Fact]
    public void Should_require_every_term_to_match()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act
        var both = catalog.Find("flag colombia");
        var impossible = catalog.Find("grin colombia");

        // Assert
        Assert.Contains(both, info => info.Emoji.Character == "🇨🇴");
        Assert.Empty(impossible);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_return_nothing_for_an_empty_query(string? query)
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Act & Assert
        Assert.Empty(catalog.Find(query!));
    }

    [Fact]
    public void Should_carry_no_skin_tone_variants()
    {
        // Arrange
        var catalog = new DefaultEmojiCatalog();

        // Assert
        Assert.All(GroupIds, groupId =>
        {
            catalog.TryFindByGroupId(groupId, out var emojis);

            Assert.DoesNotContain(emojis, info => info.Emoji.Character.Contains("\U0001F3FB", StringComparison.Ordinal));
        });
    }
}
