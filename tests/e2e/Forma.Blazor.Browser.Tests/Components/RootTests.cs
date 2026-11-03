namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The components at the library's root, the field and the visibility, in every runtime.</summary>
public sealed class RootTests
{
    public sealed class Field(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_name_its_input_by_its_label(string runtime)
        {
            // Arrange
            var (context, page) = await OpenAsync(host, runtime, "field");
            await using var _ = context;

            // Act
            var input = page.GetByRole(AriaRole.Textbox, new() { Name = "Name", Exact = true });

            // Assert
            await Assertions.Expect(input).ToHaveAttributeAsync("id", "name");
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_the_form_s_message_for_its_input_until_a_submit_finds_it_valid(string runtime)
        {
            // Arrange — a submit with the input empty
            var (context, page) = await OpenAsync(host, runtime, "field");
            await using var _ = context;
            var message = page.Locator("#check .forma-field-message");
            await page.Locator("#submit").ClickAsync();
            await Assertions.Expect(message).ToHaveTextAsync("The Name field is required.");

            // Act — the name typed and submitted with Enter
            await page.Locator("#name").FillAsync("Ada");
            await page.Locator("#name").PressAsync("Enter");

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Ada");
            await Assertions.Expect(message).ToHaveCountAsync(0);
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_submit_on_the_first_press_as_its_message_leaves(string runtime)
        {
            // Arrange — a submit with the input empty, then the name typed and the input still focused
            var (context, page) = await OpenAsync(host, runtime, "field");
            await using var _ = context;
            var submit = page.Locator("#submit");
            await submit.ClickAsync();
            await Assertions.Expect(page.Locator("#check .forma-field-message")).ToHaveTextAsync("The Name field is required.");
            await page.Locator("#name").FillAsync("Ada");
            var before = await submit.BoundingBoxAsync();

            // Act — the press takes the focus from the input, whose message leaves as it validates
            await submit.ClickAsync();

            // Assert
            await Assertions.Expect(page.Locator("#result")).ToHaveTextAsync("Ada");
            Assert.Equal(before?.Y, (await submit.BoundingBoxAsync())?.Y);
        }
    }

    public sealed class Visibility(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_collapse_its_content_out_of_reach_and_show_it_again_as_it_was(string runtime)
        {
            // Arrange — text typed into the content, which then collapses
            var (context, page) = await OpenAsync(host, runtime, "visibility");
            await using var _ = context;
            var inside = page.Locator("#inside");
            await inside.FillAsync("kept");
            await page.Locator("#toggle").ClickAsync();
            await Assertions.Expect(inside).ToBeHiddenAsync();
            await Assertions.Expect(page.Locator("#check")).ToHaveAttributeAsync("inert", string.Empty);

            // Act
            await page.Locator("#toggle").ClickAsync();

            // Assert
            await Assertions.Expect(inside).ToBeVisibleAsync();
            await Assertions.Expect(inside).ToHaveValueAsync("kept");
        }
    }
}
