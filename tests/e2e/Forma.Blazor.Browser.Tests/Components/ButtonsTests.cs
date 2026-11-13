namespace Forma.Blazor.Browser.Tests.Components;

/// <summary>The buttons' main interactions, in every runtime.</summary>
public sealed class ButtonsTests
{
    public sealed class Button(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_run_its_handler_on_each_press(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "button");
            await using var _ = context;

            // Act
            await page.Locator("#check").ClickAsync();
            await page.Locator("#check").ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("2");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_each_variant_s_label_in_its_own_color_at_rest_under_the_pointer_and_pressed(string runtime)
        {
            // Arrange — a theme giving the primary, secondary and danger labels each a color of its own
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "button");
            await using var _ = context;
            (string Id, string Color)[] variants = [("primary", "rgb(250, 250, 250)"), ("secondary", "rgb(17, 34, 51)"), ("danger", "rgb(255, 238, 0)")];

            foreach (var (id, color) in variants)
            {
                var button = page.Locator($"#{id}");

                // Act and assert
                await Assertions.Expect(button).ToHaveCSSAsync("color", color);
                await button.HoverAsync();
                await Assertions.Expect(button).ToHaveCSSAsync("color", color);
                await page.Mouse.DownAsync();
                await Assertions.Expect(button).ToHaveCSSAsync("color", color);
                await page.Mouse.UpAsync();
            }
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_draw_a_link_in_its_variant_s_look_and_a_disabled_one_without_its_address(string runtime)
        {
            // Arrange — a primary link beside the primary button, under the theme coloring its label
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "button");
            await using var _ = context;
            var link = page.Locator("#primary-link");
            var disabled = page.Locator("#disabled-link");
            var background = await page.Locator("#primary").EvaluateAsync<string>("element => getComputedStyle(element).backgroundColor");

            // Act
            await link.HoverAsync();

            // Assert
            await Assertions.Expect(link).ToHaveRoleAsync(AriaRole.Link);
            await Assertions.Expect(link).ToHaveAttributeAsync("href", "#top");
            await Assertions.Expect(link).ToHaveCSSAsync("color", "rgb(250, 250, 250)");
            await Assertions.Expect(link).ToHaveCSSAsync("text-decoration-line", "none");
            await page.Mouse.MoveAsync(0, 0);
            await Assertions.Expect(link).ToHaveCSSAsync("background-color", background);
            await Assertions.Expect(disabled).ToHaveRoleAsync(AriaRole.Link);
            await Assertions.Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");
            Assert.False(await disabled.EvaluateAsync<bool>("element => element.hasAttribute('href')"));
        }
    }

    public sealed class CloseButton(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_close_the_notice_through_its_handler(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "close-button");
            await using var _ = context;

            // Act
            await page.GetByRole(AriaRole.Button, new() { Name = "Dismiss Notice" }).ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#notice")).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Dismissed");
        }
    }

    public sealed class EyeButton(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_reveal_and_hide_the_code_through_it(string runtime)
        {
            // Arrange — the code revealed by a first press
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "eye-button");
            await using var _ = context;
            var eyeButton = page.Locator("#check");
            var result = page.Locator("#result");
            await eyeButton.ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("4242");
            await Assertions.Expect(eyeButton).ToHaveAccessibleNameAsync("Hide Code");

            // Act
            await eyeButton.ClickAsync();

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("••••");
            await Assertions.Expect(eyeButton).ToHaveAccessibleNameAsync("Show Code");
        }
    }

    public sealed class ToggleButton(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_toggle_its_pressed_state_on_each_press(string runtime)
        {
            // Arrange — the toggle turned on by a first press
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "toggle-button");
            await using var _ = context;
            var toggleButton = page.Locator("#check");
            await toggleButton.ClickAsync();
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-pressed", "true");
            await Assertions.Expect(toggleButton).ToHaveAccessibleNameAsync("Bold");

            // Act
            await toggleButton.ClickAsync();

            // Assert
            await Assertions.Expect(toggleButton).ToHaveAttributeAsync("aria-pressed", "false");
        }
    }
}
