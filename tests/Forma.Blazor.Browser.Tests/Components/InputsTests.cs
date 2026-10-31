namespace Forma.Blazor.Browser.Tests.Components;

using System.Text.RegularExpressions;

/// <summary>The inputs' interactions, in every runtime.</summary>
public sealed partial class InputsTests
{
    public sealed class CheckBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_check_by_click_and_uncheck_by_space(string runtime)
        {
            // Arrange — the box checked by a click
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "check-box");
            await using var _ = context;
            var result = page.Locator("#result");
            var checkBox = page.Locator("#check");
            await checkBox.ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("True");
            await Assertions.Expect(checkBox).ToBeCheckedAsync();

            // Act
            await checkBox.PressAsync("Space");

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("False");
            await Assertions.Expect(checkBox).Not.ToBeCheckedAsync();
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_the_mixed_state_its_host_sets_and_clear_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "check-box");
            await using var _ = context;
            var box = page.Locator("#check");
            var mix = page.Locator("#mix");

            // Act
            await mix.ClickAsync();

            // Assert
            await Assertions.Expect(box).ToHaveJSPropertyAsync("indeterminate", true);
            await mix.ClickAsync();
            await Assertions.Expect(box).ToHaveJSPropertyAsync("indeterminate", false);
        }
    }

    public sealed class ToggleSwitch(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_turn_on_by_click_and_off_by_space(string runtime)
        {
            // Arrange — the switch turned on by a click on its track, which is what takes the pointer
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "toggle-switch");
            await using var _ = context;
            var result = page.Locator("#result");
            var toggleSwitch = page.Locator("#check");
            await page.Locator("label.forma-toggle-switch").ClickAsync();
            await Assertions.Expect(result).ToHaveTextAsync("True");
            await Assertions.Expect(toggleSwitch).ToBeCheckedAsync();

            // Act
            await toggleSwitch.PressAsync("Space");

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("False");
            await Assertions.Expect(toggleSwitch).Not.ToBeCheckedAsync();
        }
    }

    public sealed class TextBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_text_as_it_is_typed(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "text-box");
            await using var _ = context;
            var textBox = page.Locator("#check");

            // Act
            await textBox.PressSequentiallyAsync("Ada Lovelace");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Ada Lovelace");
            await Assertions.Expect(textBox).ToHaveValueAsync("Ada Lovelace");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_its_label_above_a_value_the_browser_fills_until_the_browser_clears_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "text-box");
            await using var _ = context;

            // Act and assert
            await ExpectAutofillAsync(page, page.Locator("#check"));
        }
    }

    public sealed class TextArea(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_lines_as_they_are_typed_and_count_their_characters(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "text-area");
            await using var _ = context;
            var textArea = page.Locator("#check");

            // Act
            await textArea.PressSequentiallyAsync("First line");
            await textArea.PressAsync("Enter");
            await textArea.PressSequentiallyAsync("Second line");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("First line\nSecond line");
            await Assertions.Expect(textArea).ToHaveValueAsync("First line\nSecond line");
            await Assertions.Expect(page.Locator("#check-count")).ToHaveTextAsync("22 of 100 characters");
        }
    }

    public sealed class PasswordBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_the_password_as_it_is_typed_and_reveal_it_by_its_toggle(string runtime)
        {
            // Arrange — the password bound as it is typed, before focus leaves the field
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "password-box");
            await using var _ = context;
            var passwordBox = page.Locator("#check");
            var toggle = page.Locator(".forma-password-box-toggle button");
            await passwordBox.PressSequentiallyAsync("Tr0ub4dor&3");
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Tr0ub4dor&3");
            await Assertions.Expect(passwordBox).ToHaveAttributeAsync("type", "password");

            // Act
            await toggle.ClickAsync();

            // Assert
            await Assertions.Expect(passwordBox).ToHaveAttributeAsync("type", "text");
            await Assertions.Expect(passwordBox).ToHaveValueAsync("Tr0ub4dor&3");
            await Assertions.Expect(toggle).ToHaveAttributeAsync("aria-pressed", "true");
        }

        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_hold_its_label_above_a_value_the_browser_fills_until_the_browser_clears_it(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "password-box");
            await using var _ = context;

            // Act and assert
            await ExpectAutofillAsync(page, page.Locator("#check"));
        }
    }

    public sealed class SearchBox(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_report_the_search_as_it_is_typed_and_once_cleared(string runtime)
        {
            // Arrange — the search reported as it is typed
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "search-box");
            await using var _ = context;
            var result = page.Locator("#result");
            var searchBox = page.Locator("#check");
            await searchBox.PressSequentiallyAsync("forma");
            await Assertions.Expect(result).ToHaveTextAsync("forma");

            // Act
            await page.Locator(".forma-search-box-clear button").ClickAsync();

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync(string.Empty);
            await Assertions.Expect(searchBox).ToHaveValueAsync(string.Empty);
        }
    }

    public sealed class NumberInput(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_step_a_typed_number_with_the_vertical_arrows(string runtime)
        {
            // Arrange — a number typed and committed as focus leaves the field
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "number-input");
            await using var _ = context;
            var result = page.Locator("#result");
            var numberInput = page.Locator("#check");
            await ComponentChecks.WaitForInputRouteAsync(page, "#check", "number-input.increase");
            await numberInput.FillAsync("41");
            await numberInput.PressAsync("Tab");
            await Assertions.Expect(result).ToHaveTextAsync("41");

            // Act
            await numberInput.PressAsync("ArrowUp");
            await numberInput.PressAsync("ArrowUp");
            await numberInput.PressAsync("ArrowDown");

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("42");
            await Assertions.Expect(numberInput).ToHaveValueAsync("42");
            await Assertions.Expect(numberInput).ToHaveAttributeAsync("aria-valuenow", "42");
        }
    }

    public sealed class CurrencyInput(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_a_typed_amount_and_show_it_to_the_currency_s_minor_units(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "currency-input");
            await using var _ = context;
            var currencyInput = page.Locator("#check");

            // Act
            await currencyInput.FillAsync("12.5");
            await currencyInput.PressAsync("Tab");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("12.5");
            await Assertions.Expect(currencyInput).ToHaveValueAsync("12.50");
            await Assertions.Expect(currencyInput).ToHaveAttributeAsync("aria-valuenow", "12.5");
        }
    }

    public sealed class Slider(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_move_by_its_step_with_the_arrow_keys(string runtime)
        {
            // Arrange
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "slider");
            await using var _ = context;
            var slider = page.Locator("#check");

            // Act
            await slider.PressAsync("ArrowRight");
            await slider.PressAsync("ArrowRight");
            await slider.PressAsync("ArrowLeft");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("60");
            await Assertions.Expect(slider).ToHaveValueAsync("60");
            await Assertions.Expect(slider).ToHaveAttributeAsync("aria-valuetext", "60%");
            await Assertions.Expect(page.Locator(".forma-slider-value")).ToHaveTextAsync("60%");
        }
    }

    public sealed class ColorPicker(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(ComponentChecks.Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_bind_a_color_typed_as_hex_and_one_chosen_through_its_swatch(string runtime)
        {
            // Arrange — a hex color typed and committed as focus leaves the field, which the swatch's color input takes in lowercase
            var (context, page) = await ComponentChecks.OpenAsync(host, runtime, "color-picker");
            await using var _ = context;
            var result = page.Locator("#result");
            var hexInput = page.Locator("#check");
            var colorInput = page.Locator(".forma-color-picker-native");
            await hexInput.FillAsync("#FF8800");
            await hexInput.PressAsync("Tab");
            await Assertions.Expect(result).ToHaveTextAsync("#FF8800");
            await Assertions.Expect(colorInput).ToHaveValueAsync("#ff8800");

            // Act — the color the browser's picker reports once one is chosen
            await colorInput.FillAsync("#336699");

            // Assert
            await Assertions.Expect(result).ToHaveTextAsync("#336699");
            await Assertions.Expect(hexInput).ToHaveValueAsync("#336699");
        }
    }

    // A headless browser fills no field, so the check starts the animations the stylesheet runs as one is filled and cleared.
    private static async Task ExpectAutofillAsync(IPage page, ILocator input)
    {
        // The stylesheet names its animations as the field's script reads them.
        await Assertions.Expect(input).ToHaveCSSAsync("animation-name", "forma-autofill-cancel");
        Assert.True(await page.EvaluateAsync<bool>("() => [...document.styleSheets].some(sheet => [...sheet.cssRules].some(rule => rule instanceof CSSKeyframesRule && rule.name === 'forma-autofill-start'))"));

        // The script attaches in its own time, so the fill starts again until it answers.
        await page.WaitForFunctionAsync("input => { input.dispatchEvent(new AnimationEvent('animationstart', { animationName: 'forma-autofill-start' })); return input.classList.contains('has-value'); }", await input.ElementHandleAsync());
        await Assertions.Expect(input).ToHaveClassAsync(HasValuePattern());

        await input.EvaluateAsync("input => input.dispatchEvent(new AnimationEvent('animationstart', { animationName: 'forma-autofill-cancel' }))");
        await Assertions.Expect(input).Not.ToHaveClassAsync(HasValuePattern());
    }

    [GeneratedRegex(@"\bhas-value\b")]
    private static partial Regex HasValuePattern();
}
