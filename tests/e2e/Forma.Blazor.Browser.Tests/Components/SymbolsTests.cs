namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The symbols' interactions, in every runtime.</summary>
public sealed class SymbolsTests
{
    public sealed class Icon(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_the_glyph_it_is_given_and_name_only_the_mark_its_label_names(string runtime)
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
            await Assertions.Expect(page.Locator("#glyph")).ToHaveAttributeAsync("aria-hidden", "true");
            await Assertions.Expect(brand).ToHaveRoleAsync(AriaRole.Img);
            await Assertions.Expect(brand).ToHaveAccessibleNameAsync("Forma on GitHub");
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
            await page.WaitForFunctionAsync(IsDrawnFromItsSheet, "#emoji");
            await Assertions.Expect(emoji).ToHaveAccessibleNameAsync("grinning face");
            var drawn = await emoji.Locator("use").GetAttributeAsync("href");

            // Act
            await page.Locator("#swap").ClickAsync();

            // Assert
            await Assertions.Expect(emoji.Locator("use")).Not.ToHaveAttributeAsync("href", drawn!);
            await page.WaitForFunctionAsync(IsDrawnFromItsSheet, "#emoji");
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

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_more_of_its_group_from_their_sheets_as_it_scrolls_to_its_end(string runtime)
        {
            // Arrange — the first of the group drawn from its sheet
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "emoji-picker");
            await using var _ = context;
            var panel = page.Locator("#picker .forma-emoji-picker-panel");
            var cells = page.Locator("#picker .forma-emoji-picker-emoji");
            await page.WaitForFunctionAsync(IsDrawnFromItsSheet, "#picker .forma-emoji-picker-emoji svg");
            var first = await cells.CountAsync();

            // Act — the panel scrolled to its end until nothing is left to load
            await page.WaitForFunctionAsync(
                "() => { const panel = document.querySelector('#picker .forma-emoji-picker-panel'); panel.scrollTop = panel.scrollHeight; return panel.querySelector('.forma-emoji-picker-sentinel') === null; }",
                null,
                new() { PollingInterval = 200, Timeout = 60000 });

            // Assert
            Assert.True(await cells.CountAsync() > first);
            await page.WaitForFunctionAsync(IsDrawnFromItsSheet, "#picker .forma-emoji-picker-grid button:last-of-type svg");
            await Assertions.Expect(panel).ToBeVisibleAsync();
        }
    }

    // Whether the sprite the element names stands in the document, which its sheet's arrival puts there.
    private const string IsDrawnFromItsSheet = "selector => { const href = document.querySelector(selector)?.querySelector('use')?.getAttribute('href'); return href != null && document.getElementById(href.slice(1)) !== null; }";
}
