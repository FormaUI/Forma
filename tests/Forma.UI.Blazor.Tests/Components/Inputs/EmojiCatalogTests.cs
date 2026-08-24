namespace Forma.UI.Blazor.Tests.Components.Inputs;

using Forma.UI.Blazor.Components.Inputs;

public sealed class EmojiCatalogTests
{
    [Fact]
    public void Should_load_every_category_resource()
    {
        // Assert
        Assert.All(EmojiCatalog.Groups, group => Assert.NotEmpty(EmojiCatalog.InGroup(group.Id)));
    }

    [Fact]
    public void Should_load_the_generated_catalogue()
    {
        // Act
        var all = EmojiCatalog.All;

        // Assert
        Assert.NotEmpty(all);
        Assert.Equal("15.1", EmojiCatalog.Version);
        Assert.Contains(all, emoji => emoji.Character == "😀");
    }

    [Fact]
    public void Should_expose_every_group_in_presentation_order()
    {
        // Act
        var groups = EmojiCatalog.Groups;

        // Assert
        Assert.Equal(
            ["smileys", "people", "animals", "food", "activities", "travel", "objects", "symbols", "flags"],
            groups.Select(group => group.Id));
    }

    [Fact]
    public void Should_place_every_emoji_in_a_known_group()
    {
        // Arrange
        var ids = EmojiCatalog.Groups.Select(group => group.Id).ToHashSet(StringComparer.Ordinal);

        // Assert
        Assert.All(EmojiCatalog.All, emoji => Assert.Contains(emoji.GroupId, ids));
    }

    [Fact]
    public void Should_give_every_emoji_a_name_and_search_keywords()
    {
        // Assert
        Assert.All(EmojiCatalog.All, emoji =>
        {
            Assert.False(string.IsNullOrWhiteSpace(emoji.Name));
            Assert.NotEmpty(emoji.Keywords);
        });
    }

    [Fact]
    public void Should_return_the_members_of_a_group()
    {
        // Act
        var smileys = EmojiCatalog.InGroup("smileys");

        // Assert
        Assert.NotEmpty(smileys);
        Assert.All(smileys, emoji => Assert.Equal("smileys", emoji.GroupId));
    }

    [Fact]
    public void Should_return_nothing_for_a_group_it_does_not_hold()
    {
        // Assert
        Assert.Empty(EmojiCatalog.InGroup("nonexistent"));
    }

    [Fact]
    public void Should_match_a_search_against_keywords_rather_than_the_name_alone()
    {
        // Act
        var results = EmojiCatalog.Search("grin");

        // Assert
        Assert.Contains(results, emoji => emoji.Character == "😀");
    }

    [Fact]
    public void Should_match_a_word_rather_than_any_substring()
    {
        // Act
        var results = EmojiCatalog.Search("cat");

        // Assert
        Assert.Contains(results, emoji => emoji.Character == "🐱");
        Assert.DoesNotContain(results, emoji => emoji.Character == "🥴");
    }

    [Fact]
    public void Should_match_a_word_that_is_not_the_first()
    {
        // Act
        var results = EmojiCatalog.Search("face");

        // Assert
        Assert.Contains(results, emoji => emoji.Character == "😀");
    }

    [Fact]
    public void Should_require_every_term_to_match()
    {
        // Act
        var both = EmojiCatalog.Search("flag colombia");
        var impossible = EmojiCatalog.Search("grin colombia");

        // Assert
        Assert.Contains(both, emoji => emoji.Character == "🇨🇴");
        Assert.Empty(impossible);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_return_nothing_for_an_empty_query(string? query)
    {
        // Assert
        Assert.Empty(EmojiCatalog.Search(query));
    }

    [Fact]
    public void Should_give_every_emoji_a_lowercase_code_point_sprite_id()
    {
        // Assert
        Assert.All(EmojiCatalog.All, emoji =>
        {
            Assert.Matches("^[0-9a-f]+(-[0-9a-f]+)*$", emoji.SpriteId);
            Assert.DoesNotContain("-0", emoji.SpriteId, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void Should_carry_no_skin_tone_variants()
    {
        // Assert
        Assert.DoesNotContain(EmojiCatalog.All, emoji => emoji.Character.Contains("\U0001F3FB", StringComparison.Ordinal));
    }
}
