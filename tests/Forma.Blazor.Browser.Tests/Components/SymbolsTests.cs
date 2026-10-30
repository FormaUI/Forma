namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The symbols' main interactions, in every runtime.</summary>
public sealed class SymbolsTests
{
    public sealed class Icon(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_the_glyph_it_is_given_and_name_the_brand_mark(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "icon");
            await using var _ = context;
            var brand = page.Locator("#brand");
            var glyphPath = page.Locator("#glyph path").First;
            var drawn = await glyphPath.GetAttributeAsync("d");
            Assert.NotNull(drawn);

            // Act
            await page.Locator("#swap").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Star");
            await Assertions.Expect(glyphPath).Not.ToHaveAttributeAsync("d", drawn);
            await Assertions.Expect(brand).ToHaveRoleAsync(AriaRole.Img);
            await Assertions.Expect(brand).ToHaveAccessibleNameAsync("GitHub");
        }
    }

    public sealed class Emoji(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_the_emoji_it_is_given_under_its_name(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "emoji");
            await using var _ = context;
            var emoji = page.Locator("#emoji");
            await Assertions.Expect(emoji).ToHaveTextAsync("\U0001F600");
            await Assertions.Expect(emoji).ToHaveAccessibleNameAsync("grinning face");

            // Act
            await page.Locator("#swap").ClickAsync();

            // Assert
            await Assertions.Expect(emoji).ToHaveTextAsync("\U0001F389");
            await Assertions.Expect(emoji).ToHaveRoleAsync(AriaRole.Img);
            await Assertions.Expect(emoji).ToHaveAccessibleNameAsync("party popper");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("party popper");
        }
    }

    public sealed class EmojiPicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_pick_an_emoji_from_the_category_chosen_and_keep_it_among_the_recents(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "emoji-picker");
            await using var _ = context;
            var picker = page.Locator("#picker");
            var animals = picker.GetByRole(AriaRole.Tab, new() { Name = "Animals & Nature", Exact = true });

            // Act
            await animals.ClickAsync();
            await picker.GetByRole(AriaRole.Button, new() { Name = "monkey face", Exact = true }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("\U0001F435");
            await Assertions.Expect(animals).ToHaveAttributeAsync("aria-selected", "true");
            await Assertions.Expect(picker.GetByRole(AriaRole.Tab, new() { Name = "Recently Used", Exact = true })).ToBeVisibleAsync();
        }
    }
}
