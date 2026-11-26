namespace Forma.Blazor.Browser.Tests.Components;

using static Forma.Blazor.Browser.Tests.ComponentChecks;

/// <summary>The components at the library's root, the field and the visibility, in every runtime.</summary>
public sealed class RootTests
{
    // What keeps the label of the field around the input id names from showing whole: a label or its text wider than it shows,
    // ending in an ellipsis, or text standing outside the field's box.
    private const string ReadLabelProblems = """
        id => {
            const box = document.getElementById(id).closest('.forma-field-box');
            const label = box.querySelector(':scope > .forma-field-label');
            const text = label.querySelector('.forma-field-label-text');
            const range = document.createRange();
            range.selectNodeContents(text);
            const shown = range.getBoundingClientRect();
            const bounds = box.getBoundingClientRect();
            const problems = [label, text].flatMap(element => [
                ...(element.scrollWidth > element.clientWidth ? [`${element.className} is ${element.scrollWidth - element.clientWidth}px wider than it shows`] : []),
                ...(getComputedStyle(element).textOverflow === 'ellipsis' ? [`${element.className} ends in an ellipsis`] : []),
            ]);
            if (shown.top < bounds.top - 0.5 || shown.bottom > bounds.bottom + 0.5 || shown.left < bounds.left - 0.5 || shown.right > bounds.right + 0.5) {
                problems.push(`the text stands at ${shown.top},${shown.left} to ${shown.bottom},${shown.right}, outside the box at ${bounds.top},${bounds.left} to ${bounds.bottom},${bounds.right}`);
            }
            return problems;
        }
        """;

    // Whether the label's text of the field around the input id names lies exactly over the copy holding its room, as a floated
    // label does once it has settled.
    private const string IsLabelOverItsRoom = """
        id => {
            const box = document.getElementById(id).closest('.forma-field-box');
            const read = element => { const range = document.createRange(); range.selectNodeContents(element); return range.getBoundingClientRect(); };
            const text = read(box.querySelector(':scope > .forma-field-label .forma-field-label-text'));
            const room = read(box.querySelector(':scope > .forma-field-label-room'));
            return ['top', 'left', 'width', 'height'].every(side => Math.abs(text[side] - room[side]) < 0.5);
        }
        """;

    private const string ReadBoxHeight = "id => document.getElementById(id).closest('.forma-field-box').getBoundingClientRect().height";

    // What keeps the password box id names from standing at the field's step with its toggle at the value's end: its box's height,
    // its toggle's target smaller than a target takes under the page's pointer, a finger's where the pointer is coarse, or a line of
    // its label over that target.
    private const string ReadEndButtonProblems = """
        id => {
            const box = document.getElementById(id).closest('.forma-field-box');
            const rem = parseFloat(getComputedStyle(document.documentElement).fontSize);
            const step = parseFloat(getComputedStyle(box).getPropertyValue('--target-size-xl')) * rem;
            const least = matchMedia('(pointer: coarse)').matches ? 44 : 24;
            const height = box.getBoundingClientRect().height;
            const target = box.querySelector('.forma-password-box-toggle button').getBoundingClientRect();
            const range = document.createRange();
            range.selectNodeContents(box.querySelector(':scope > .forma-field-label .forma-field-label-text'));
            const isOverTarget = line => line.left < target.right - 0.5 && target.left < line.right - 0.5 && line.top < target.bottom - 0.5 && target.top < line.bottom - 0.5;
            return [
                ...(Math.abs(height - step) > 0.5 ? [`the box stands ${height}px where the field's step is ${step}px`] : []),
                ...(target.width < least - 0.5 || target.height < least - 0.5 ? [`the toggle's target is ${target.width} by ${target.height}, less than ${least}`] : []),
                ...([...range.getClientRects()].some(isOverTarget) ? ["a line of the label lies over the toggle's target"] : []),
            ];
        }
        """;

    public sealed class Field(BrowserHost host)
    {
        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_show_a_long_label_whole_at_a_phone_s_width_and_keep_its_height_as_the_label_floats(string runtime)
        {
            // Arrange — a phone's width, a field in each direction resting
            var (context, page) = await OpenAsync(host, runtime, "field-label");
            await using var _ = context;
            await page.SetViewportSizeAsync(360, page.ViewportSize!.Height);
            var restingProblems = new List<string>();
            var restingHeights = new Dictionary<string, double>();

            foreach (var id in (string[])["ltr", "rtl"])
            {
                restingProblems.AddRange((await page.EvaluateAsync<string[]>(ReadLabelProblems, id)).Select(problem => $"{id} resting: {problem}"));
                restingHeights[id] = await page.EvaluateAsync<double>(ReadBoxHeight, id);
            }

            // Act — each input focused in turn, its label floated over its room once it settles
            var floatedProblems = new List<string>();
            var floatedHeights = new Dictionary<string, double>();

            foreach (var id in (string[])["ltr", "rtl"])
            {
                await page.Locator($"#{id}").FocusAsync();
                await page.WaitForFunctionAsync(IsLabelOverItsRoom, id);
                floatedProblems.AddRange((await page.EvaluateAsync<string[]>(ReadLabelProblems, id)).Select(problem => $"{id} floated: {problem}"));
                floatedHeights[id] = await page.EvaluateAsync<double>(ReadBoxHeight, id);
            }

            // Assert
            Assert.Multiple(
                () => Assert.Empty(restingProblems),
                () => Assert.Empty(floatedProblems),
                () => Assert.Equal(restingHeights["ltr"], floatedHeights["ltr"], 0.5),
                () => Assert.Equal(restingHeights["rtl"], floatedHeights["rtl"], 0.5));
        }

        [Theory]
        [MemberData(nameof(Runtimes), MemberType = typeof(ComponentChecks))]
        public async Task Should_stand_at_its_step_with_a_button_at_its_value_s_end_its_label_clear_of_the_button_under_any_pointer(string runtime)
        {
            // Arrange — a password box in each direction, labeled in one line, its toggle at the value's end
            var (context, page) = await OpenAsync(host, runtime, "field-label");
            await using var _ = context;
            var problems = new List<string>();

            // Act — each read resting, then focused once its label has floated over its room
            foreach (var id in (string[])["ltr-password", "rtl-password"])
            {
                problems.AddRange((await page.EvaluateAsync<string[]>(ReadEndButtonProblems, id)).Select(problem => $"{id} resting: {problem}"));
                await page.Locator($"#{id}").FocusAsync();
                await page.WaitForFunctionAsync(IsLabelOverItsRoom, id);
                problems.AddRange((await page.EvaluateAsync<string[]>(ReadEndButtonProblems, id)).Select(problem => $"{id} floated: {problem}"));
            }

            // Assert
            Assert.Empty(problems);
        }

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
